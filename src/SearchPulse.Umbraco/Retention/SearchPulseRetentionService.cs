using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Retention;

/// <summary>
/// Replaces expired detailed rows with immutable daily aggregates so all-time reporting stays available.
/// </summary>
public sealed class SearchPulseRetentionService(
    IScopeProvider scopeProvider,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ISearchPulseRetentionService
{
    public void PurgeExpiredEvents()
    {
        // Aggregate completed UTC days only. A partial day cannot represent an exact reporting range.
        var cutoffUtc = DateTime.UtcNow.Date.AddDays(-optionsMonitor.CurrentValue.DetailedDataRetentionDays);
        while (GetOldestExpiredEvent(cutoffUtc) is { } oldestEventUtc)
        {
            ArchiveDay(oldestEventUtc.Date);
        }
    }

    private DateTime? GetOldestExpiredEvent(DateTime cutoffUtc)
    {
        using var scope = scopeProvider.CreateScope();
        var oldestEventUtc = scope.Database.ExecuteScalar<DateTime?>(
            $"SELECT MIN(occurredUtc) FROM {SearchPulseEventDto.TableName} WHERE occurredUtc < @0",
            cutoffUtc);
        scope.Complete();
        return oldestEventUtc;
    }

    private void ArchiveDay(DateTime occurredDateUtc)
    {
        var nextDateUtc = occurredDateUtc.AddDays(1);
        using var scope = scopeProvider.CreateScope();
        var aggregates = scope.Database.Fetch<SearchPulseAggregateSource>(
            $"SELECT eventType AS EventType, path AS Path, COALESCE(target, '') AS Target, COUNT(*) AS EventCount " +
            $"FROM {SearchPulseEventDto.TableName} WHERE occurredUtc >= @0 AND occurredUtc < @1 " +
            "GROUP BY eventType, path, COALESCE(target, '')",
            occurredDateUtc,
            nextDateUtc);

        foreach (var aggregate in aggregates)
        {
            var target = aggregate.Target ?? string.Empty;
            scope.Database.Insert(new SearchPulseDailyAggregateDto
            {
                BucketKey = SearchPulseDailyAggregateDto.CreateBucketKey(
                    occurredDateUtc,
                    aggregate.EventType,
                    aggregate.Path,
                    target),
                OccurredDateUtc = occurredDateUtc,
                EventType = aggregate.EventType,
                Path = aggregate.Path,
                Target = target,
                EventCount = aggregate.EventCount,
            });
        }

        scope.Database.Execute(
            $"DELETE FROM {SearchPulseEventDto.TableName} WHERE occurredUtc >= @0 AND occurredUtc < @1",
            occurredDateUtc,
            nextDateUtc);
        scope.Complete();
    }

    private sealed class SearchPulseAggregateSource
    {
        public string EventType { get; init; } = string.Empty;

        public string Path { get; init; } = string.Empty;

        public string Target { get; init; } = string.Empty;

        public long EventCount { get; init; }
    }
}