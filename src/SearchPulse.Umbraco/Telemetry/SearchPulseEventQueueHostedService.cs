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
    ILogger<SearchPulseEventQueueHostedService> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogProcessingFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1002, "SearchPulseQueueProcessingFailed"),
        "SearchPulse could not process a batch of queued events.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                scope.ServiceProvider.GetRequiredService<ISearchPulseEventQueueProcessor>().ProcessBatch();
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                LogProcessingFailure(logger, exception);
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(optionsMonitor.CurrentValue.EventProcessingIntervalMilliseconds),
                stoppingToken);
        }
    }
}