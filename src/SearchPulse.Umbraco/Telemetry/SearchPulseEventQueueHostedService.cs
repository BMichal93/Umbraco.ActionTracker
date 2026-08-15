using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Processes small batches continuously without making the visitor request wait for reporting work.
/// </summary>
internal sealed class SearchPulseEventQueueHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor,
    ISearchPulseOperationalState operationalState,
    ILogger<SearchPulseEventQueueHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogProcessingFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1002, "SearchPulseQueueProcessingFailed"),
        "SearchPulse could not process a batch of queued events.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        operationalState.MarkWorkerStarted();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var processedCount = scope.ServiceProvider.GetRequiredService<ISearchPulseEventQueueProcessor>().ProcessBatch();
                    operationalState.MarkBatchSucceeded(processedCount);
                    SearchPulseMetrics.RecordProcessedEvents(processedCount);
                }
                catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    operationalState.MarkBatchFailed();
                    SearchPulseMetrics.RecordFailedBatch();
                    LogProcessingFailure(logger, exception);
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(optionsMonitor.CurrentValue.EventProcessingIntervalMilliseconds),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // A normal host shutdown must not be reported as a failed background service.
        }
    }
}