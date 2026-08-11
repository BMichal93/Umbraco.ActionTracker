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
    private const int ReportingDays = 30;
    private const int MaximumTopPages = 5;

    public SearchPulseOverview GetLastThirtyDays()
    {
        var since = DateTime.UtcNow.AddDays(-ReportingDays);
        using var scope = scopeProvider.CreateScope();

        var eventCounts = scope.Database.Fetch<SearchPulseEventCount>(
            $"SELECT eventType AS EventType, COUNT(*) AS Total FROM {SearchPulseEventDto.TableName} WHERE occurredUtc >= @0 GROUP BY eventType",
            since);
        var pageCounts = scope.Database.Fetch<SearchPulsePageCount>(
            $"SELECT path AS Path, COUNT(*) AS PageViews FROM {SearchPulseEventDto.TableName} WHERE occurredUtc >= @0 AND eventType = @1 GROUP BY path ORDER BY COUNT(*) DESC",
            since,
            SearchPulseEventType.PageView.ToString());
        scope.Complete();

        var totals = eventCounts.ToDictionary(item => item.EventType, item => item.Total, StringComparer.Ordinal);
        return new SearchPulseOverview(
            settingsService.IsEnabled(),
            new SearchPulseOverviewTotals(
                GetTotal(SearchPulseEventType.PageView),
                GetTotal(SearchPulseEventType.PageExit),
                GetTotal(SearchPulseEventType.Scroll25),
                GetTotal(SearchPulseEventType.Scroll50),
                GetTotal(SearchPulseEventType.Scroll75)),
            pageCounts.Take(MaximumTopPages)
                .Select(item => new SearchPulsePageSummary(item.Path, item.PageViews))
                .ToArray());

        int GetTotal(SearchPulseEventType eventType) => totals.GetValueOrDefault(eventType.ToString());
    }

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
}
