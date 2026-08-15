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

    [Column("id")] public long Id { get; set; }
    [Column("occurredUtc")] public DateTime OccurredUtc { get; set; }
    [Column("eventType")] public string EventType { get; set; } = string.Empty;
    [Column("path")] public string Path { get; set; } = string.Empty;
    [Column("target")] public string? Target { get; set; }
    [Column("contentKey")] public string? ContentKey { get; set; }
    [Column("referrerDomain")] public string? ReferrerDomain { get; set; }
    [Column("utmSource")] public string? UtmSource { get; set; }
    [Column("utmMedium")] public string? UtmMedium { get; set; }
    [Column("utmCampaign")] public string? UtmCampaign { get; set; }
    [Column("leaseExpiresUtc")] public DateTime? LeaseExpiresUtc { get; set; }
    [Column("leaseToken")] public string? LeaseToken { get; set; }
    [Column("processedUtc")] public DateTime? ProcessedUtc { get; set; }
}
