using SearchPulse.Umbraco.Goals;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Overview;

public sealed class SearchPulseOverviewService(
    IScopeProvider scopeProvider,
    ISearchPulseSettingsService settingsService,
    ISearchPulseGoalService? goalService = null) : ISearchPulseOverviewService
{
    private const int MaximumTopPages = 5;
    private const int MaximumPopularInteractions = 5;
    private static readonly string[] InteractionTypes = [
        nameof(SearchPulseEventType.ExternalLinkClick), nameof(SearchPulseEventType.DownloadClick), nameof(SearchPulseEventType.CustomAction),
        nameof(SearchPulseEventType.FormSubmit), nameof(SearchPulseEventType.FormSuccess), nameof(SearchPulseEventType.VideoPlay), nameof(SearchPulseEventType.SiteSearch)];

    public SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var since = GetReportingStartUtc(generatedAtUtc, rangeDays);
        object sinceParameter = since.HasValue ? since.Value : DBNull.Value;
        using var scope = scopeProvider.CreateScope();
        var eventCounts = scope.Database.Fetch<SearchPulseEventCount>($"SELECT eventType AS EventType, COUNT(*) AS Total FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) GROUP BY eventType", sinceParameter);
        var pageCounts = scope.Database.Fetch<SearchPulsePageCount>($"SELECT path AS Path, COUNT(*) AS PageViews FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND eventType = @1 GROUP BY path", sinceParameter, nameof(SearchPulseEventType.PageView));
        var interactionCounts = scope.Database.Fetch<SearchPulseInteractionCount>($"SELECT eventType AS EventType, target AS Target, COUNT(*) AS Interactions FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND target IS NOT NULL AND target <> '' AND eventType IN (@1, @2, @3, @4, @5, @6, @7) GROUP BY eventType, target", [sinceParameter, .. InteractionTypes]);
        var acquisitionCounts = scope.Database.Fetch<SearchPulseAcquisitionCount>($"SELECT COALESCE(utmSource, '(direct)') AS Source, COALESCE(utmMedium, '(none)') AS Medium, COALESCE(utmCampaign, '(none)') AS Campaign, COALESCE(referrerDomain, '') AS ReferrerDomain, COUNT(*) AS Interactions FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND (utmSource IS NOT NULL OR referrerDomain IS NOT NULL) GROUP BY utmSource, utmMedium, utmCampaign, referrerDomain", sinceParameter);
        var contentCounts = scope.Database.Fetch<SearchPulseContentCount>($"SELECT contentKey AS ContentKey, SUM(CASE WHEN eventType = @1 THEN 1 ELSE 0 END) AS PageViews, SUM(CASE WHEN eventType <> @1 THEN 1 ELSE 0 END) AS Interactions FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND contentKey IS NOT NULL GROUP BY contentKey", sinceParameter, nameof(SearchPulseEventType.PageView));
        var goalCounts = scope.Database.Fetch<SearchPulseGoalCount>($"SELECT eventType AS EventType, target AS Target, COUNT(*) AS Completions FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) GROUP BY eventType, target", sinceParameter);

        if (rangeDays == 0)
        {
            eventCounts.AddRange(scope.Database.Fetch<SearchPulseEventCount>($"SELECT eventType AS EventType, SUM(eventCount) AS Total FROM {SearchPulseDailyAggregateDto.TableName} GROUP BY eventType"));
            pageCounts.AddRange(scope.Database.Fetch<SearchPulsePageCount>($"SELECT path AS Path, SUM(eventCount) AS PageViews FROM {SearchPulseDailyAggregateDto.TableName} WHERE eventType = @0 GROUP BY path", nameof(SearchPulseEventType.PageView)));
            interactionCounts.AddRange(scope.Database.Fetch<SearchPulseInteractionCount>($"SELECT eventType AS EventType, target AS Target, SUM(eventCount) AS Interactions FROM {SearchPulseDailyAggregateDto.TableName} WHERE target <> '' AND eventType IN (@0, @1, @2, @3, @4, @5, @6) GROUP BY eventType, target", InteractionTypes));
            acquisitionCounts.AddRange(scope.Database.Fetch<SearchPulseAcquisitionCount>($"SELECT COALESCE(utmSource, '(direct)') AS Source, COALESCE(utmMedium, '(none)') AS Medium, COALESCE(utmCampaign, '(none)') AS Campaign, COALESCE(referrerDomain, '') AS ReferrerDomain, SUM(eventCount) AS Interactions FROM {SearchPulseDailyAggregateDto.TableName} WHERE utmSource IS NOT NULL OR referrerDomain IS NOT NULL GROUP BY utmSource, utmMedium, utmCampaign, referrerDomain"));
            contentCounts.AddRange(scope.Database.Fetch<SearchPulseContentCount>($"SELECT contentKey AS ContentKey, SUM(CASE WHEN eventType = @0 THEN eventCount ELSE 0 END) AS PageViews, SUM(CASE WHEN eventType <> @0 THEN eventCount ELSE 0 END) AS Interactions FROM {SearchPulseDailyAggregateDto.TableName} WHERE contentKey IS NOT NULL GROUP BY contentKey", nameof(SearchPulseEventType.PageView)));
            goalCounts.AddRange(scope.Database.Fetch<SearchPulseGoalCount>($"SELECT eventType AS EventType, target AS Target, SUM(eventCount) AS Completions FROM {SearchPulseDailyAggregateDto.TableName} GROUP BY eventType, target"));
        }
        scope.Complete();

        var totals = eventCounts.GroupBy(x => x.EventType, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Sum(y => y.Total), StringComparer.Ordinal);
        var goals = goalService?.GetGoals(false) ?? [];
        return new SearchPulseOverview(settingsService.IsEnabled(), rangeDays, GetRangeLabel(rangeDays),
            new SearchPulseOverviewTotals(GetTotal(SearchPulseEventType.PageView), GetTotal(SearchPulseEventType.PageExit), GetTotal(SearchPulseEventType.Scroll25), GetTotal(SearchPulseEventType.Scroll50), GetTotal(SearchPulseEventType.Scroll75)),
            BuildTopPages(pageCounts, sort), BuildPopularInteractions(interactionCounts, sort), generatedAtUtc,
            BuildGoals(goals, goalCounts), BuildAcquisition(acquisitionCounts, sort), BuildContent(contentCounts, sort));

        long GetTotal(SearchPulseEventType type) => totals.GetValueOrDefault(type.ToString());
    }

    public static bool IsSupportedRange(int rangeDays) => rangeDays is 0 or 1 or 7 or 30 or 90;
    public static DateTime? GetReportingStartUtc(DateTime generatedAtUtc, int rangeDays) => rangeDays == 0 ? null : generatedAtUtc.AddDays(-rangeDays);
    internal static string GetRangeLabel(int rangeDays) => rangeDays switch { 0 => "All time", 1 => "Last 24 hours", _ => $"Last {rangeDays} days" };

    internal static IReadOnlyList<SearchPulsePageSummary> BuildTopPages(IEnumerable<SearchPulsePageCount> pageCounts, SearchPulseOverviewSort sort = SearchPulseOverviewSort.Count) =>
        (sort == SearchPulseOverviewSort.Name ? pageCounts.GroupBy(x => x.Path, StringComparer.Ordinal).Select(x => new SearchPulsePageCount { Path = x.Key, PageViews = x.Sum(y => y.PageViews) }).OrderBy(x => x.Path, StringComparer.Ordinal) : pageCounts.GroupBy(x => x.Path, StringComparer.Ordinal).Select(x => new SearchPulsePageCount { Path = x.Key, PageViews = x.Sum(y => y.PageViews) }).OrderByDescending(x => x.PageViews).ThenBy(x => x.Path, StringComparer.Ordinal)).Take(MaximumTopPages).Select(x => new SearchPulsePageSummary(x.Path, x.PageViews)).ToArray();

    internal static IReadOnlyList<SearchPulseInteractionSummary> BuildPopularInteractions(IEnumerable<SearchPulseInteractionCount> counts, SearchPulseOverviewSort sort = SearchPulseOverviewSort.Count) =>
        (sort == SearchPulseOverviewSort.Name ? counts.Where(x => !string.IsNullOrEmpty(x.Target) && IsSupportedInteractionType(x.EventType)).GroupBy(x => (x.EventType, Target: x.Target!), StringTupleComparer.Ordinal).Select(x => new SearchPulseInteractionCount { EventType = x.Key.EventType, Target = x.Key.Target, Interactions = x.Sum(y => y.Interactions) }).OrderBy(x => x.EventType, StringComparer.Ordinal).ThenBy(x => x.Target, StringComparer.Ordinal) : counts.Where(x => !string.IsNullOrEmpty(x.Target) && IsSupportedInteractionType(x.EventType)).GroupBy(x => (x.EventType, Target: x.Target!), StringTupleComparer.Ordinal).Select(x => new SearchPulseInteractionCount { EventType = x.Key.EventType, Target = x.Key.Target, Interactions = x.Sum(y => y.Interactions) }).OrderByDescending(x => x.Interactions).ThenBy(x => x.EventType, StringComparer.Ordinal).ThenBy(x => x.Target, StringComparer.Ordinal)).Take(MaximumPopularInteractions).Select(x => new SearchPulseInteractionSummary(x.EventType, x.Target, x.Interactions)).ToArray();

    private static bool IsSupportedInteractionType(string eventType) => InteractionTypes.Contains(eventType, StringComparer.Ordinal);
    private static SearchPulseGoalSummary[] BuildGoals(IReadOnlyList<SearchPulseGoalDto> goals, IEnumerable<SearchPulseGoalCount> counts) => goals.Select(goal => new SearchPulseGoalSummary(goal.Id, goal.Name, goal.EventType, goal.Target, goal.IsEnabled, counts.Where(x => string.Equals(x.EventType, goal.EventType, StringComparison.Ordinal) && string.Equals(x.Target ?? string.Empty, goal.Target, StringComparison.Ordinal)).Sum(x => x.Completions))).ToArray();
    private static SearchPulseAcquisitionSummary[] BuildAcquisition(IEnumerable<SearchPulseAcquisitionCount> counts, SearchPulseOverviewSort sort) => counts.GroupBy(x => (x.Source, x.Medium, x.Campaign, x.ReferrerDomain)).Select(x => new SearchPulseAcquisitionSummary(x.Key.Source, x.Key.Medium, x.Key.Campaign, x.Key.ReferrerDomain, x.Sum(y => y.Interactions))).OrderByDescending(x => sort == SearchPulseOverviewSort.Count ? x.Interactions : 0).ThenBy(x => x.Source, StringComparer.Ordinal).Take(10).ToArray();
    private static SearchPulseContentSummary[] BuildContent(IEnumerable<SearchPulseContentCount> counts, SearchPulseOverviewSort sort) => counts.GroupBy(x => x.ContentKey!, StringComparer.Ordinal).Select(x => new SearchPulseContentSummary(x.Key, x.Sum(y => y.PageViews), x.Sum(y => y.Interactions))).OrderByDescending(x => sort == SearchPulseOverviewSort.Count ? x.PageViews + x.Interactions : 0).ThenBy(x => x.ContentKey, StringComparer.Ordinal).Take(10).ToArray();

    private sealed class StringTupleComparer : IEqualityComparer<(string EventType, string Target)> { public static readonly StringTupleComparer Ordinal = new(); public bool Equals((string EventType, string Target) x, (string EventType, string Target) y) => StringComparer.Ordinal.Equals(x.EventType, y.EventType) && StringComparer.Ordinal.Equals(x.Target, y.Target); public int GetHashCode((string EventType, string Target) value) => HashCode.Combine(StringComparer.Ordinal.GetHashCode(value.EventType), StringComparer.Ordinal.GetHashCode(value.Target)); }
    internal sealed class SearchPulseEventCount { public string EventType { get; init; } = string.Empty; public long Total { get; init; } }
    internal sealed class SearchPulsePageCount { public string Path { get; init; } = string.Empty; public long PageViews { get; init; } }
    internal sealed class SearchPulseInteractionCount { public string EventType { get; init; } = string.Empty; public string? Target { get; init; } public long Interactions { get; init; } }
    internal sealed class SearchPulseGoalCount { public string EventType { get; init; } = string.Empty; public string? Target { get; init; } public long Completions { get; init; } }
    internal sealed class SearchPulseAcquisitionCount { public string Source { get; init; } = string.Empty; public string Medium { get; init; } = string.Empty; public string Campaign { get; init; } = string.Empty; public string ReferrerDomain { get; init; } = string.Empty; public long Interactions { get; init; } }
    internal sealed class SearchPulseContentCount { public string? ContentKey { get; init; } public long PageViews { get; init; } public long Interactions { get; init; } }
}
