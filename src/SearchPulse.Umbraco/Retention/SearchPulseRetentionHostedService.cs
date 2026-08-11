using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SearchPulse.Umbraco.Retention;

/// <summary>
/// Runs cleanup at startup and daily thereafter. Cleanup failures are logged and retried
/// rather than interrupting the host website.
/// </summary>
internal sealed class SearchPulseRetentionHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SearchPulseRetentionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupPeriod = TimeSpan.FromDays(1);
    private static readonly Action<ILogger, Exception?> LogPurgeFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1001, "SearchPulseRetentionFailed"),
        "SearchPulse could not purge expired detailed events.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PurgeAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupPeriod);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PurgeAsync(stoppingToken);
        }
    }

    private Task PurgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var scope = serviceScopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ISearchPulseRetentionService>().PurgeExpiredEvents();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogPurgeFailure(logger, exception);
        }

        return Task.CompletedTask;
    }
}
