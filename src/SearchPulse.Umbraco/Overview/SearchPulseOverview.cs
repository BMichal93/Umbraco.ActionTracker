namespace SearchPulse.Umbraco.Overview;

public sealed record SearchPulseOverview(
    bool IsEnabled,
    int RangeDays,
    string RangeLabel,
    SearchPulseOverviewTotals Totals,
    IReadOnlyList<SearchPulsePageSummary> TopPages,
    IReadOnlyList<SearchPulseInteractionSummary> PopularInteractions,
    DateTime GeneratedAtUtc,
    IReadOnlyList<SearchPulseGoalSummary>? Goals = null,
    IReadOnlyList<SearchPulseAcquisitionSummary>? Acquisition = null,
    IReadOnlyList<SearchPulseContentSummary>? ContentPerformance = null);

public sealed record SearchPulseOverviewTotals(long PageViews, long Exits, long Reached25Percent, long Reached50Percent, long Reached75Percent);
public sealed record SearchPulsePageSummary(string Path, long PageViews);
public sealed record SearchPulseInteractionSummary(string EventType, string? Target, long Interactions);
public sealed record SearchPulseGoalSummary(long Id, string Name, string EventType, string Target, bool IsEnabled, long Completions);
public sealed record SearchPulseAcquisitionSummary(string Source, string Medium, string Campaign, string ReferrerDomain, long Interactions);
public sealed record SearchPulseContentSummary(string ContentKey, long PageViews, long Interactions);

public enum SearchPulseOverviewSort { Count, Name }
