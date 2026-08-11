namespace SearchPulse.Umbraco.Retention;

/// <summary>
/// Removes detailed event rows that have reached the configured retention limit.
/// </summary>
public interface ISearchPulseRetentionService
{
    void PurgeExpiredEvents();
}
