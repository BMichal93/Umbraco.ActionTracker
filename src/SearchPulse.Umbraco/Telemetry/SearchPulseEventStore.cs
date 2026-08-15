using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Durably accepts anonymous signals before background processing writes them to reporting storage.
/// </summary>
public sealed class SearchPulseEventStore(
    IScopeProvider scopeProvider,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor,
    ILogger<SearchPulseEventStore> logger) : ISearchPulseEventStore
{
    private static readonly Action<ILogger, int, Exception?> LogQueueFull = LoggerMessage.Define<int>(
        LogLevel.Warning,
        new EventId(1003, "SearchPulseQueueFull"),
        "SearchPulse durable queue reached its configured capacity of {QueueCapacity}; new events are being rejected with HTTP 503.");

    public Task<SearchPulseEventRecordResult> RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var options = optionsMonitor.CurrentValue;
        using var scope = scopeProvider.CreateScope();
        var inserted = scope.Database.Execute(
            $"INSERT INTO {SearchPulseEventQueueDto.TableName} (occurredUtc, eventType, path, target, contentKey, referrerDomain, utmSource, utmMedium, utmCampaign) " +
            $"SELECT @0, @1, @2, @3, @4, @5, @6, @7, @8 WHERE (SELECT COUNT(*) FROM {SearchPulseEventQueueDto.TableName} WHERE processedUtc IS NULL) < @9",
            DateTime.UtcNow,
            searchPulseEvent.Type.ToString(),
            searchPulseEvent.Path,
            (object?)searchPulseEvent.Target ?? DBNull.Value,
            (object?)searchPulseEvent.ContentKey ?? DBNull.Value,
            (object?)searchPulseEvent.ReferrerDomain ?? DBNull.Value,
            (object?)searchPulseEvent.UtmSource ?? DBNull.Value,
            (object?)searchPulseEvent.UtmMedium ?? DBNull.Value,
            (object?)searchPulseEvent.UtmCampaign ?? DBNull.Value,
            options.MaximumQueuedEvents);
        scope.Complete();

        if (inserted == 1)
        {
            SearchPulseMetrics.RecordAcceptedEvent();
            return Task.FromResult(SearchPulseEventRecordResult.Accepted);
        }

        SearchPulseMetrics.RecordRejectedEvent();
        LogQueueFull(logger, options.MaximumQueuedEvents, null);
        return Task.FromResult(SearchPulseEventRecordResult.QueueFull);
    }

    public SearchPulseQueueStatus GetQueueStatus()
    {
        using var scope = scopeProvider.CreateScope();
        var queueStatus = scope.Database.FirstOrDefault<SearchPulseQueueStatus>(
            $"SELECT COUNT(*) AS PendingEvents, MIN(occurredUtc) AS OldestPendingEventUtc FROM {SearchPulseEventQueueDto.TableName} WHERE processedUtc IS NULL")
            ?? new SearchPulseQueueStatus(0, null);
        scope.Complete();
        return queueStatus;
    }
}
