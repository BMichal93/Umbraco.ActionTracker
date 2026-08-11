using NPoco;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// The private database representation of an anonymous event.
/// </summary>
[TableName(TableName)]
[PrimaryKey(nameof(Id), AutoIncrement = true)]
public sealed class SearchPulseEventDto
{
    public const string TableName = "searchPulseEvent";

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
}
