namespace SearchPulse.Umbraco.Overview;

/// <summary>
/// Produces the minimal backoffice summary without exposing event-level visitor data.
/// </summary>
public interface ISearchPulseOverviewService
{
    SearchPulseOverview GetLastThirtyDays();
}
