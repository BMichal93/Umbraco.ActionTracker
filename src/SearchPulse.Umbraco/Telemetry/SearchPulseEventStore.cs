using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Persists SearchPulse signals through Umbraco's scoped database API.
/// </summary>
public sealed class SearchPulseEventStore(IScopeProvider scopeProvider) : ISearchPulseEventStore
{
    public Task RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var scope = scopeProvider.CreateScope();
        scope.Database.Insert(new SearchPulseEventDto
        {
            OccurredUtc = DateTime.UtcNow,
            EventType = searchPulseEvent.Type.ToString(),
            Path = searchPulseEvent.Path,
            Target = searchPulseEvent.Target,
        });
        scope.Complete();

        return Task.CompletedTask;
    }
}
