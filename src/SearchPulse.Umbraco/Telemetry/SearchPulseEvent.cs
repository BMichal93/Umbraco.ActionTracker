namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// A validated, anonymous content signal ready for persistence.
/// </summary>
public sealed record SearchPulseEvent(
    SearchPulseEventType Type,
    string Path,
    string? Target,
    string? ContentKey = null,
    string? ReferrerDomain = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null);
