using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Durably accepts anonymous signals before background processing writes them to reporting storage.
/// </summary>
public sealed class SearchPulseEventStore(
    IScopeProvider scopeProvider,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ISearchPulseEventStore
{
    public Task<SearchPulseEventRecordResult> RecordAsync(
        SearchPulseEvent searchPulseEvent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = optionsMonitor.CurrentValue;
        object targetParameter = (object?)searchPulseEvent.Target ?? DBNull.Value;
        using var scope = scopeProvider.CreateScope();
        var inserted = scope.Database.Execute(
            $"INSERT INTO {SearchPulseEventQueueDto.TableName} (occurredUtc, eventType, path, target) " +
            $"SELECT @0, @1, @2, @3 WHERE (SELECT COUNT(*) FROM {SearchPulseEventQueueDto.TableName} " +
            "WHERE processedUtc IS NULL) < @4",
            DateTime.UtcNow,
            searchPulseEvent.Type.ToString(),
            searchPulseEvent.Path,
            targetParameter,
            options.MaximumQueuedEvents);
        scope.Complete();

        return Task.FromResult(inserted == 1
            ? SearchPulseEventRecordResult.Accepted
            : SearchPulseEventRecordResult.QueueFull);
    }

    public int GetPendingEventCount()
    {
        using var scope = scopeProvider.CreateScope();
        var pendingEventCount = scope.Database.ExecuteScalar<int>(
            $"SELECT COUNT(*) FROM {SearchPulseEventQueueDto.TableName} WHERE processedUtc IS NULL");
        scope.Complete();
        return pendingEventCount;
    }
}