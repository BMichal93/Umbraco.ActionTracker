namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Transfers a bounded set of durable inbox rows into reporting storage.
/// </summary>
public interface ISearchPulseEventQueueProcessor
{
    int ProcessBatch();
}