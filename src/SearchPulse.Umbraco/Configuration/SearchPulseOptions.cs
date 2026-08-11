namespace SearchPulse.Umbraco.Configuration;

/// <summary>
/// Controls the small number of installation-wide SearchPulse choices.
/// Detailed event settings intentionally stay out of the backoffice UI.
/// </summary>
public sealed class SearchPulseOptions
{
    public const string SectionName = "SearchPulse";

    /// <summary>
    /// Enables SearchPulse collection after the host site's consent provider allows it.
    /// Defaults to false so installing the package never starts tracking visitors.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Keeps detailed events for a short, administrator-selected period.
    /// </summary>
    public int DetailedDataRetentionDays { get; set; } = 30;

    /// <summary>
    /// Prevents accidental collection from administration and API routes.
    /// </summary>
    public IReadOnlyCollection<string> ExcludedPaths { get; set; } = ["/umbraco", "/api"];
}
