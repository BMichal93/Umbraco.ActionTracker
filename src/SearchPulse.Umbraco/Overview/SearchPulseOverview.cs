namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// The concise, 30-day readout used by the SearchPulse overview.
/// </summary>
public sealed record SearchPulseOverview(
    bool IsEnabled,
    SearchPulseOverviewTotals Totals,
    IReadOnlyList<SearchPulsePageSummary> TopPages,
    IReadOnlyList<SearchPulseInteractionSummary> PopularInteractions);

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

/// <summary>
/// A frequently used anonymous interaction in the reporting window.
/// </summary>
public sealed record SearchPulseInteractionSummary(
    string EventType,
    string? Target,
    int Interactions);
