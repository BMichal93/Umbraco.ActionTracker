using System.Text.Json.Serialization;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// The intentionally narrow browser-to-server payload. Browser time, IP addresses,
/// client identifiers, user agents, and arbitrary properties are never accepted.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SearchPulseEventRequest
{
    public string? Type { get; init; }

    public string? Path { get; init; }

    public string? Target { get; init; }

    public string? ContentKey { get; init; }

    public string? ReferrerDomain { get; init; }

    public string? UtmSource { get; init; }

    public string? UtmMedium { get; init; }

    public string? UtmCampaign { get; init; }
}
