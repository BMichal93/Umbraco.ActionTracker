namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Keeps process-local worker state for diagnostics and health reporting. Queue rows remain the
/// durable source of truth, so this state is never used to decide whether an event was recorded.
/// </summary>
public interface ISearchPulseOperationalState
{
    void MarkWorkerStarted();

    void MarkBatchSucceeded(int processedCount);

    void MarkBatchFailed();

    SearchPulseWorkerStatus GetStatus();
}

public sealed class SearchPulseOperationalState : ISearchPulseOperationalState
{
    private readonly object _sync = new();
    private bool _workerStarted;
    private DateTime? _lastSuccessfulBatchUtc;
    private DateTime? _lastFailureUtc;
    private int _failedBatchCount;
    private int _lastProcessedCount;

    public void MarkWorkerStarted()
    {
        lock (_sync)
        {
            _workerStarted = true;
        }
    }

    public void MarkBatchSucceeded(int processedCount)
    {
        lock (_sync)
        {
            _lastSuccessfulBatchUtc = DateTime.UtcNow;
            _lastProcessedCount = processedCount;
        }
    }

    public void MarkBatchFailed()
    {
        lock (_sync)
        {
            _lastFailureUtc = DateTime.UtcNow;
            _failedBatchCount++;
        }
    }

    public SearchPulseWorkerStatus GetStatus()
    {
        lock (_sync)
        {
            return new SearchPulseWorkerStatus(
                _workerStarted,
                _lastSuccessfulBatchUtc,
                _lastFailureUtc,
                _failedBatchCount,
                _lastProcessedCount);
        }
    }
}

public sealed record SearchPulseQueueStatus(int PendingEvents, DateTime? OldestPendingEventUtc);

public sealed record SearchPulseWorkerStatus(
    bool WorkerStarted,
    DateTime? LastSuccessfulBatchUtc,
    DateTime? LastFailureUtc,
    int FailedBatchCount,
    int LastProcessedCount);