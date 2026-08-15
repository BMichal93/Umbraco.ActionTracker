namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Durably accepts validated anonymous content signals.
/// </summary>
public interface ISearchPulseEventStore
{
    Task<SearchPulseEventRecordResult> RecordAsync(
        SearchPulseEvent searchPulseEvent,
        CancellationToken cancellationToken = default);

    SearchPulseQueueStatus GetQueueStatus();
}