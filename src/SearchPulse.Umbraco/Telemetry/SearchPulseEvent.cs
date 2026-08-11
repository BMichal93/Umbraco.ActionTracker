namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// A validated, anonymous content signal ready for persistence.
/// </summary>
public sealed record SearchPulseEvent(
    SearchPulseEventType Type,
    string Path,
    string? Target);
