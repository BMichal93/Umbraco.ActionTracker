namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// The concise, 30-day readout used by the SearchPulse overview.
/// </summary>
public sealed record SearchPulseOverview(
    bool IsEnabled,
    SearchPulseOverviewTotals Totals,
    IReadOnlyList<SearchPulsePageSummary> TopPages);

/// <summary>
/// Plain-language totals for the selected reporting window.
/// </summary>
public sealed record SearchPulseOverviewTotals(
    int PageViews,
    int Exits,
    int Reached25Percent,
    int Reached50Percent,
    int Reached75Percent);

/// <summary>
/// A page's view count in the reporting window.
/// </summary>
public sealed record SearchPulsePageSummary(string Path, int PageViews);
