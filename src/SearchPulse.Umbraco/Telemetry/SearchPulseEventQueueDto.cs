using NPoco;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// A durable, package-owned inbox row awaiting reporting storage.
/// </summary>
[TableName(TableName)]
[PrimaryKey(nameof(Id), AutoIncrement = true)]
public sealed class SearchPulseEventQueueDto
{
    public const string TableName = "searchPulseEventQueue";

    [Column("id")]
    public long Id { get; set; }

    [Column("occurredUtc")]
    public DateTime OccurredUtc { get; set; }

    [Column("eventType")]
    public string EventType { get; set; } = string.Empty;

    [Column("path")]
    public string Path { get; set; } = string.Empty;

    [Column("target")]
    public string? Target { get; set; }

    [Column("leaseExpiresUtc")]
    public DateTime? LeaseExpiresUtc { get; set; }

    [Column("leaseToken")]
    public string? LeaseToken { get; set; }

    [Column("processedUtc")]
    public DateTime? ProcessedUtc { get; set; }
}