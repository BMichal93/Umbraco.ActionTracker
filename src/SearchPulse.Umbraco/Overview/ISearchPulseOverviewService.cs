namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// Produces the small backoffice reporting summary without exposing visitor-level data.
/// </summary>
public interface ISearchPulseOverviewService
{
    SearchPulseOverview GetOverview(int rangeDays, SearchPulseOverviewSort sort);
}