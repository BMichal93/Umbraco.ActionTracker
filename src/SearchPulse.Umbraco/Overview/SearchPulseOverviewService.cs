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
        var pageOrderBy = sort == SearchPulseOverviewSort.Name
            ? "path ASC"
            : "COUNT(*) DESC, path ASC";
        var interactionOrderBy = sort == SearchPulseOverviewSort.Name
            ? "eventType ASC, target ASC"
            : "COUNT(*) DESC, eventType ASC, target ASC";

        using var scope = scopeProvider.CreateScope();

        var eventCounts = scope.Database.Fetch<SearchPulseEventCount>(
            $"SELECT eventType AS EventType, COUNT(*) AS Total FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) GROUP BY eventType",
            sinceParameter);
        var pageCounts = scope.Database.Fetch<SearchPulsePageCount>(
            $"SELECT path AS Path, COUNT(*) AS PageViews FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND eventType = @1 GROUP BY path ORDER BY {pageOrderBy}",
            sinceParameter,
            SearchPulseEventType.PageView.ToString());
        var interactionCounts = scope.Database.Fetch<SearchPulseInteractionCount>(
            $"SELECT eventType AS EventType, target AS Target, COUNT(*) AS Interactions FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0) AND target IS NOT NULL AND target <> '' AND eventType IN (@1, @2, @3) GROUP BY eventType, target ORDER BY {interactionOrderBy}",
            sinceParameter,
            SearchPulseEventType.ExternalLinkClick.ToString(),
            SearchPulseEventType.DownloadClick.ToString(),
            SearchPulseEventType.CustomAction.ToString());
        scope.Complete();

        var totals = eventCounts.ToDictionary(item => item.EventType, item => item.Total, StringComparer.Ordinal);
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
            pageCounts.Take(MaximumTopPages)
                .Select(item => new SearchPulsePageSummary(item.Path, item.PageViews))
                .ToArray(),
            BuildPopularInteractions(interactionCounts, sort),
            generatedAtUtc);

        int GetTotal(SearchPulseEventType eventType) => totals.GetValueOrDefault(eventType.ToString());
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

    internal static IReadOnlyList<SearchPulseInteractionSummary> BuildPopularInteractions(
        IEnumerable<SearchPulseInteractionCount> interactionCounts,
        SearchPulseOverviewSort sort = SearchPulseOverviewSort.Count)
    {
        var supportedInteractions = interactionCounts
            .Where(item => item.Target is not null && IsSupportedInteractionType(item.EventType));

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

    private sealed class SearchPulseEventCount
    {
        public string EventType { get; init; } = string.Empty;

        public int Total { get; init; }
    }

    private sealed class SearchPulsePageCount
    {
        public string Path { get; init; } = string.Empty;

        public int PageViews { get; init; }
    }

    internal sealed class SearchPulseInteractionCount
    {
        public string EventType { get; init; } = string.Empty;

        public string? Target { get; init; }

        public int Interactions { get; init; }
    }
}