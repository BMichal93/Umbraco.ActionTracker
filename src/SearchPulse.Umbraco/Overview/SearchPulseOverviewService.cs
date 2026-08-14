using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// Aggregates anonymous event rows into a small, understandable content summary.
/// </summary>
public sealed class SearchPulseOverviewService(
    IScopeProvider scopeProvider,
    ISearchPulseSettingsService settingsService) : ISearchPulseOverviewService
{
    private const int MaximumTopPages = 5;
    private const int MaximumPopularInteractions = 5;

    public SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var since = GetReportingStartUtc(generatedAtUtc, rangeDays);
        object sinceParameter = since.HasValue ? since.Value : DBNull.Value;

        using var scope = scopeProvider.CreateScope();
        var eventCounts = scope.Database.Fetch<SearchPulseEventCount>(
            $"SELECT eventType AS EventType, COUNT(*) AS Total FROM {SearchPulseEventDto.TableName} " +
            "WHERE (@0 IS NULL OR occurredUtc >= @0) GROUP BY eventType",
            sinceParameter);
        var pageCounts = scope.Database.Fetch<SearchPulsePageCount>(
            $"SELECT path AS Path, COUNT(*) AS PageViews FROM {SearchPulseEventDto.TableName} " +
            "WHERE (@0 IS NULL OR occurredUtc >= @0) AND eventType = @1 GROUP BY path",
            sinceParameter,
            SearchPulseEventType.PageView.ToString());
        var interactionCounts = scope.Database.Fetch<SearchPulseInteractionCount>(
            $"SELECT eventType AS EventType, target AS Target, COUNT(*) AS Interactions FROM {SearchPulseEventDto.TableName} " +
            "WHERE (@0 IS NULL OR occurredUtc >= @0) AND target IS NOT NULL AND target <> '' " +
            "AND eventType IN (@1, @2, @3) GROUP BY eventType, target",
            sinceParameter,
            SearchPulseEventType.ExternalLinkClick.ToString(),
            SearchPulseEventType.DownloadClick.ToString(),
            SearchPulseEventType.CustomAction.ToString());

        if (rangeDays == 0)
        {
            eventCounts.AddRange(scope.Database.Fetch<SearchPulseEventCount>(
                $"SELECT eventType AS EventType, SUM(eventCount) AS Total FROM {SearchPulseDailyAggregateDto.TableName} GROUP BY eventType"));
            pageCounts.AddRange(scope.Database.Fetch<SearchPulsePageCount>(
                $"SELECT path AS Path, SUM(eventCount) AS PageViews FROM {SearchPulseDailyAggregateDto.TableName} " +
                "WHERE eventType = @0 GROUP BY path",
                SearchPulseEventType.PageView.ToString()));
            interactionCounts.AddRange(scope.Database.Fetch<SearchPulseInteractionCount>(
                $"SELECT eventType AS EventType, target AS Target, SUM(eventCount) AS Interactions " +
                $"FROM {SearchPulseDailyAggregateDto.TableName} WHERE target <> '' " +
                "AND eventType IN (@0, @1, @2) GROUP BY eventType, target",
                SearchPulseEventType.ExternalLinkClick.ToString(),
                SearchPulseEventType.DownloadClick.ToString(),
                SearchPulseEventType.CustomAction.ToString()));
        }

        scope.Complete();

        var totals = eventCounts
            .GroupBy(item => item.EventType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Total), StringComparer.Ordinal);
        return new SearchPulseOverview(
            settingsService.IsEnabled(),
            rangeDays,
            GetRangeLabel(rangeDays),
            new SearchPulseOverviewTotals(
                GetTotal(SearchPulseEventType.PageView),
                GetTotal(SearchPulseEventType.PageExit),
                GetTotal(SearchPulseEventType.Scroll25),
                GetTotal(SearchPulseEventType.Scroll50),
                GetTotal(SearchPulseEventType.Scroll75)),
            BuildTopPages(pageCounts, sort),
            BuildPopularInteractions(interactionCounts, sort),
            generatedAtUtc);

        long GetTotal(SearchPulseEventType eventType) => totals.GetValueOrDefault(eventType.ToString());
    }

    public static bool IsSupportedRange(int rangeDays) => rangeDays is 0 or 1 or 7 or 30 or 90;

    public static DateTime? GetReportingStartUtc(DateTime generatedAtUtc, int rangeDays) =>
        rangeDays == 0 ? null : generatedAtUtc.AddDays(-rangeDays);

    internal static string GetRangeLabel(int rangeDays) => rangeDays switch
    {
        0 => "All time",
        1 => "Last 24 hours",
        _ => $"Last {rangeDays} days",
    };

    internal static IReadOnlyList<SearchPulsePageSummary> BuildTopPages(
        IEnumerable<SearchPulsePageCount> pageCounts,
        SearchPulseOverviewSort sort = SearchPulseOverviewSort.Count)
    {
        var combinedPages = pageCounts
            .GroupBy(item => item.Path, StringComparer.Ordinal)
            .Select(group => new SearchPulsePageCount
            {
                Path = group.Key,
                PageViews = group.Sum(item => item.PageViews),
            });
        var orderedPages = sort == SearchPulseOverviewSort.Name
            ? combinedPages.OrderBy(item => item.Path, StringComparer.Ordinal)
            : combinedPages.OrderByDescending(item => item.PageViews).ThenBy(item => item.Path, StringComparer.Ordinal);

        return orderedPages
            .Take(MaximumTopPages)
            .Select(item => new SearchPulsePageSummary(item.Path, item.PageViews))
            .ToArray();
    }

    internal static IReadOnlyList<SearchPulseInteractionSummary> BuildPopularInteractions(
        IEnumerable<SearchPulseInteractionCount> interactionCounts,
        SearchPulseOverviewSort sort = SearchPulseOverviewSort.Count)
    {
        var supportedInteractions = interactionCounts
            .Where(item => !string.IsNullOrEmpty(item.Target) && IsSupportedInteractionType(item.EventType))
            .GroupBy(item => (EventType: item.EventType, Target: item.Target!), StringTupleComparer.Ordinal)
            .Select(group => new SearchPulseInteractionCount
            {
                EventType = group.Key.EventType,
                Target = group.Key.Target,
                Interactions = group.Sum(item => item.Interactions),
            });

        var orderedInteractions = sort == SearchPulseOverviewSort.Name
            ? supportedInteractions
                .OrderBy(item => item.EventType, StringComparer.Ordinal)
                .ThenBy(item => item.Target, StringComparer.Ordinal)
            : supportedInteractions
                .OrderByDescending(item => item.Interactions)
                .ThenBy(item => item.EventType, StringComparer.Ordinal)
                .ThenBy(item => item.Target, StringComparer.Ordinal);

        return orderedInteractions
            .Take(MaximumPopularInteractions)
            .Select(item => new SearchPulseInteractionSummary(item.EventType, item.Target, item.Interactions))
            .ToArray();
    }

    private static bool IsSupportedInteractionType(string eventType) =>
        eventType is nameof(SearchPulseEventType.ExternalLinkClick) or nameof(SearchPulseEventType.DownloadClick) or nameof(SearchPulseEventType.CustomAction);

    private sealed class StringTupleComparer : IEqualityComparer<(string EventType, string Target)>
    {
        public static readonly StringTupleComparer Ordinal = new();

        public bool Equals((string EventType, string Target) x, (string EventType, string Target) y) =>
            StringComparer.Ordinal.Equals(x.EventType, y.EventType)
            && StringComparer.Ordinal.Equals(x.Target, y.Target);

        public int GetHashCode((string EventType, string Target) value) =>
            HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(value.EventType),
                StringComparer.Ordinal.GetHashCode(value.Target));
    }

    internal sealed class SearchPulseEventCount
    {
        public string EventType { get; init; } = string.Empty;

        public long Total { get; init; }
    }

    internal sealed class SearchPulsePageCount
    {
        public string Path { get; init; } = string.Empty;

        public long PageViews { get; init; }
    }

    internal sealed class SearchPulseInteractionCount
    {
        public string EventType { get; init; } = string.Empty;

        public string? Target { get; init; }

        public long Interactions { get; init; }
    }
}