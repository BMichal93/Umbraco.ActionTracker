using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Retention;

/// <summary>
/// Keeps detailed event storage bounded without asking an editor to schedule cleanup.
/// </summary>
public sealed class SearchPulseRetentionService(
    IScopeProvider scopeProvider,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ISearchPulseRetentionService
{
    public void PurgeExpiredEvents()
    {
        var cutoff = DateTime.UtcNow.AddDays(-optionsMonitor.CurrentValue.DetailedDataRetentionDays);

        using var scope = scopeProvider.CreateScope();
        scope.Database.Execute(
            $"DELETE FROM {SearchPulseEventDto.TableName} WHERE occurredUtc < @0",
            cutoff);
        scope.Complete();
    }
}
