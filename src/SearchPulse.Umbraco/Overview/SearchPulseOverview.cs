namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// The concise reporting readout used by the SearchPulse overview.
/// </summary>
public sealed record SearchPulseOverview(
    bool IsEnabled,
    int RangeDays,
    string RangeLabel,
    SearchPulseOverviewTotals Totals,
    IReadOnlyList<SearchPulsePageSummary> TopPages,
    IReadOnlyList<SearchPulseInteractionSummary> PopularInteractions,
    DateTime GeneratedAtUtc);

/// <summary>
/// Plain-language totals for the selected reporting window.
/// </summary>
public sealed record SearchPulseOverviewTotals(
    long PageViews,
    long Exits,
    long Reached25Percent,
    long Reached50Percent,
    long Reached75Percent);

/// <summary>
/// A page's view count in the reporting window.
/// </summary>
public sealed record SearchPulsePageSummary(string Path, long PageViews);

/// <summary>
/// A frequently used anonymous interaction in the reporting window.
/// </summary>
public sealed record SearchPulseInteractionSummary(
    string EventType,
    string? Target,
    long Interactions);

public enum SearchPulseOverviewSort
{
    Count,
    Name,
}