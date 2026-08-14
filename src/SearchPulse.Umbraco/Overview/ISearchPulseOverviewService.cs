namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// Produces and clears the small backoffice reporting summary without exposing visitor-level data.
/// </summary>
public interface ISearchPulseOverviewService
{
    SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort);

    void Clear(int rangeDays);
}