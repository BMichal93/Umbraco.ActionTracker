using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Uses short database leases so independently running Umbraco nodes cannot process the same event.
/// </summary>
public sealed class SearchPulseEventQueueProcessor(
    IScopeProvider scopeProvider,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ISearchPulseEventQueueProcessor
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public int ProcessBatch()
    {
        var now = DateTime.UtcNow;
        var leaseExpiresUtc = now.Add(LeaseDuration);
        var batchSize = optionsMonitor.CurrentValue.EventProcessingBatchSize;
        var candidates = GetCandidates(now, batchSize);
        var processedCount = 0;

        foreach (var candidate in candidates)
        {
            var leaseToken = Guid.NewGuid().ToString("N");
            if (TryClaim(candidate.Id, now, leaseExpiresUtc, leaseToken)
                && TryPersist(candidate, leaseToken, now))
            {
                processedCount++;
            }
        }

        return processedCount;
    }

    private List<SearchPulseEventQueueDto> GetCandidates(DateTime now, int batchSize)
    {
        using var scope = scopeProvider.CreateScope();
        var page = scope.Database.Page<SearchPulseEventQueueDto>(
            1,
            batchSize,
            $"SELECT * FROM {SearchPulseEventQueueDto.TableName} " +
            "WHERE processedUtc IS NULL AND (leaseExpiresUtc IS NULL OR leaseExpiresUtc < @0) ORDER BY id",
            now);
        scope.Complete();
        return page.Items;
    }

    private bool TryClaim(long id, DateTime now, DateTime leaseExpiresUtc, string leaseToken)
    {
        using var scope = scopeProvider.CreateScope();
        var claimed = scope.Database.Execute(
            $"UPDATE {SearchPulseEventQueueDto.TableName} SET leaseExpiresUtc = @0, leaseToken = @1 " +
            "WHERE id = @2 AND processedUtc IS NULL AND (leaseExpiresUtc IS NULL OR leaseExpiresUtc < @3)",
            leaseExpiresUtc,
            leaseToken,
            id,
            now);
        scope.Complete();
        return claimed == 1;
    }

    private bool TryPersist(SearchPulseEventQueueDto queueEvent, string leaseToken, DateTime processedUtc)
    {
        using var scope = scopeProvider.CreateScope();
        var markedProcessed = scope.Database.Execute(
            $"UPDATE {SearchPulseEventQueueDto.TableName} SET processedUtc = @0 " +
            "WHERE id = @1 AND processedUtc IS NULL AND leaseToken = @2",
            processedUtc,
            queueEvent.Id,
            leaseToken);
        if (markedProcessed != 1)
        {
            return false;
        }

        scope.Database.Insert(new SearchPulseEventDto
        {
            OccurredUtc = queueEvent.OccurredUtc,
            EventType = queueEvent.EventType,
            Path = queueEvent.Path,
            Target = queueEvent.Target,
            ContentKey = queueEvent.ContentKey,
            ReferrerDomain = queueEvent.ReferrerDomain,
            UtmSource = queueEvent.UtmSource,
            UtmMedium = queueEvent.UtmMedium,
            UtmCampaign = queueEvent.UtmCampaign,
        });
        scope.Database.Execute(
            $"DELETE FROM {SearchPulseEventQueueDto.TableName} WHERE id = @0 AND processedUtc = @1",
            queueEvent.Id,
            processedUtc);
        scope.Complete();
        return true;
    }
}
