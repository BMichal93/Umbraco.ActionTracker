namespace SearchPulse.Umbraco.Settings;

/// <summary>
/// Performs destructive data operations consistently across the durable queue and reporting storage.
/// </summary>
public interface ISearchPulseDataManagementService
{
    void Clear(int rangeDays);
}