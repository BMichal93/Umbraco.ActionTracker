namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// The intentionally narrow browser-to-server payload. Browser time, IP addresses,
/// client identifiers, user agents, and arbitrary properties are never accepted.
/// </summary>
public sealed class SearchPulseEventRequest
{
    public string? Type { get; init; }

    public string? Path { get; init; }

    public string? Target { get; init; }
}
