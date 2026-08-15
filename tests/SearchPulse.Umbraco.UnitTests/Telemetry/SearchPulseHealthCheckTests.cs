using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.UnitTests.Telemetry;

public sealed class SearchPulseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsyncReportsHealthyForAnActiveWorkerAndQueueBelowWarningThreshold()
    {
        var state = new SearchPulseOperationalState();
        state.MarkWorkerStarted();
        state.MarkBatchSucceeded(4);
        var healthCheck = CreateHealthCheck(new SearchPulseQueueStatus(10, null), state);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(10, result.Data["pendingEvents"]);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsUnhealthyWhenQueueIsAtCapacity()
    {
        var healthCheck = CreateHealthCheck(
            new SearchPulseQueueStatus(100, DateTime.UtcNow.AddMinutes(-1)),
            RunningWorker());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsDegradedBeforeTheWorkerHasStarted()
    {
        var healthCheck = CreateHealthCheck(
            new SearchPulseQueueStatus(0, null),
            new StubOperationalState(new SearchPulseWorkerStatus(false, null, null, 0, 0)));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsDegradedWhenTheWorkerHeartbeatIsStale()
    {
        var healthCheck = CreateHealthCheck(
            new SearchPulseQueueStatus(0, null),
            new StubOperationalState(new SearchPulseWorkerStatus(true, DateTime.UtcNow.AddSeconds(-10), null, 0, 0)),
            intervalMilliseconds: 100);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsDegradedWhenTheLatestBatchFailed()
    {
        var healthCheck = CreateHealthCheck(
            new SearchPulseQueueStatus(0, null),
            new StubOperationalState(new SearchPulseWorkerStatus(true, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow, 1, 0)));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsUnhealthyWhenTheQueueCannotBeRead()
    {
        var healthCheck = new SearchPulseHealthCheck(
            new ThrowingEventStore(),
            RunningWorker(),
            new StubOptionsMonitor(new SearchPulseOptions { MaximumQueuedEvents = 100, QueueWarningThresholdPercent = 75 }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsDegradedAtTheWarningThreshold()
    {
        var healthCheck = CreateHealthCheck(new SearchPulseQueueStatus(75, null), RunningWorker());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    private static SearchPulseHealthCheck CreateHealthCheck(
        SearchPulseQueueStatus queueStatus,
        ISearchPulseOperationalState operationalState,
        int intervalMilliseconds = 1_000) =>
        new(
            new StubEventStore(queueStatus),
            operationalState,
            new StubOptionsMonitor(new SearchPulseOptions
            {
                MaximumQueuedEvents = 100,
                QueueWarningThresholdPercent = 75,
                EventProcessingIntervalMilliseconds = intervalMilliseconds,
            }));

    private static SearchPulseOperationalState RunningWorker()
    {
        var state = new SearchPulseOperationalState();
        state.MarkWorkerStarted();
        state.MarkBatchSucceeded(0);
        return state;
    }

    private sealed class StubEventStore(SearchPulseQueueStatus queueStatus) : ISearchPulseEventStore
    {
        public Task<SearchPulseEventRecordResult> RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchPulseEventRecordResult.Accepted);

        public SearchPulseQueueStatus GetQueueStatus() => queueStatus;
    }

    private sealed class ThrowingEventStore : ISearchPulseEventStore
    {
        public Task<SearchPulseEventRecordResult> RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchPulseEventRecordResult.Accepted);

        public SearchPulseQueueStatus GetQueueStatus() => throw new InvalidOperationException("Database is unavailable.");
    }

    private sealed class StubOperationalState(SearchPulseWorkerStatus status) : ISearchPulseOperationalState
    {
        public void MarkWorkerStarted() { }

        public void MarkBatchSucceeded(int processedCount) { }

        public void MarkBatchFailed() { }

        public SearchPulseWorkerStatus GetStatus() => status;
    }

    private sealed class StubOptionsMonitor(SearchPulseOptions options) : IOptionsMonitor<SearchPulseOptions>
    {
        public SearchPulseOptions CurrentValue => options;

        public SearchPulseOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<SearchPulseOptions, string?> listener) => null;
    }
}