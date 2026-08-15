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
    /// Caps unprocessed events so analytics cannot exhaust the host database during a sustained overload.
    /// </summary>
    public int MaximumQueuedEvents { get; set; } = 100_000;

    /// <summary>
    /// Limits each background database transaction cycle.
    /// </summary>
    public int EventProcessingBatchSize { get; set; } = 250;

    /// <summary>
    /// Controls how frequently the package moves queued events into reporting storage.
    /// </summary>
    public int EventProcessingIntervalMilliseconds { get; set; } = 1_000;

    /// <summary>
    /// Marks the queue unhealthy before it reaches hard capacity, giving operators time to act.
    /// </summary>
    public int QueueWarningThresholdPercent { get; set; } = 75;

    /// <summary>
    /// Prevents accidental collection from administration and API routes.
    /// </summary>
    public IReadOnlyCollection<string> ExcludedPaths { get; set; } = ["/umbraco", "/api"];
}