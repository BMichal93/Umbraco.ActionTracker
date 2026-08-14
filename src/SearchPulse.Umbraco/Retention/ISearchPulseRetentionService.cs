namespace SearchPulse.Umbraco.Retention;

/// <summary>
/// Archives completed daily totals before removing detailed rows that have reached the configured retention limit.
/// </summary>
public interface ISearchPulseRetentionService
{
    void PurgeExpiredEvents();
}
