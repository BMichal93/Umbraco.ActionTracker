namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Stores validated anonymous content signals.
/// </summary>
public interface ISearchPulseEventStore
{
    Task RecordAsync(SearchPulseEvent searchPulseEvent, CancellationToken cancellationToken = default);
}
