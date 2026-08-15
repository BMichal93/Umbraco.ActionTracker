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
        var healthCheck = new SearchPulseHealthCheck(
            new StubEventStore(new SearchPulseQueueStatus(10, null)),
            state,
            new StubOptionsMonitor(new SearchPulseOptions { MaximumQueuedEvents = 100, QueueWarningThresholdPercent = 75 }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal(10, result.Data["pendingEvents"]);
    }

    [Fact]
    public async Task CheckHealthAsyncReportsUnhealthyWhenQueueIsAtCapacity()
    {
        var state = new SearchPulseOperationalState();
        state.MarkWorkerStarted();
        state.MarkBatchSucceeded(0);
        var healthCheck = new SearchPulseHealthCheck(
            new StubEventStore(new SearchPulseQueueStatus(100, DateTime.UtcNow.AddMinutes(-1))),
            state,
            new StubOptionsMonitor(new SearchPulseOptions { MaximumQueuedEvents = 100, QueueWarningThresholdPercent = 75 }));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    private sealed class StubEventStore(SearchPulseQueueStatus queueStatus) : ISearchPulseEventStore
    {
        public Task<SearchPulseEventRecordResult> RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default) =>
            Task.FromResult(SearchPulseEventRecordResult.Accepted);

        public SearchPulseQueueStatus GetQueueStatus() => queueStatus;
    }

    private sealed class StubOptionsMonitor(SearchPulseOptions options) : IOptionsMonitor<SearchPulseOptions>
    {
        public SearchPulseOptions CurrentValue => options;

        public SearchPulseOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<SearchPulseOptions, string?> listener) => null;
    }
}