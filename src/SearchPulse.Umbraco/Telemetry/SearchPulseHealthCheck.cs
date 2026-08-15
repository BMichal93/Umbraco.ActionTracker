using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Reports the durable queue and worker availability through the host application's health checks.
/// </summary>
public sealed class SearchPulseHealthCheck(
    ISearchPulseEventStore eventStore,
    ISearchPulseOperationalState operationalState,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var queue = eventStore.GetQueueStatus();
            var worker = operationalState.GetStatus();
            var options = optionsMonitor.CurrentValue;
            var queuePercent = queue.PendingEvents * 100d / options.MaximumQueuedEvents;
            var data = new Dictionary<string, object>
            {
                ["pendingEvents"] = queue.PendingEvents,
                ["maximumQueuedEvents"] = options.MaximumQueuedEvents,
                ["queuePercent"] = Math.Round(queuePercent, 2),
                ["oldestPendingEventUtc"] = queue.OldestPendingEventUtc?.ToString("O") ?? string.Empty,
                ["lastSuccessfulBatchUtc"] = worker.LastSuccessfulBatchUtc?.ToString("O") ?? string.Empty,
                ["failedBatchCount"] = worker.FailedBatchCount,
            };

            if (queue.PendingEvents >= options.MaximumQueuedEvents)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("SearchPulse queue is at capacity and is rejecting new events.", data: data));
            }

            var staleAfter = TimeSpan.FromMilliseconds(options.EventProcessingIntervalMilliseconds * 3d + 5_000);
            if (!worker.WorkerStarted || worker.LastSuccessfulBatchUtc is null || DateTime.UtcNow - worker.LastSuccessfulBatchUtc > staleAfter)
            {
                return Task.FromResult(HealthCheckResult.Degraded("SearchPulse queue worker has not completed a recent batch.", data: data));
            }

            if (queuePercent >= options.QueueWarningThresholdPercent || worker.LastFailureUtc > worker.LastSuccessfulBatchUtc)
            {
                return Task.FromResult(HealthCheckResult.Degraded("SearchPulse is accepting events but requires attention.", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("SearchPulse durable collection is healthy.", data));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("SearchPulse could not query its durable queue.", exception));
        }
    }
}