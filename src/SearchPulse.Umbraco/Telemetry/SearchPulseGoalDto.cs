using NPoco;

namespace SearchPulse.Umbraco.Telemetry;

[TableName(TableName)]
[PrimaryKey(nameof(Id), AutoIncrement = true)]
public sealed class SearchPulseGoalDto
{
    public const string TableName = "searchPulseGoal";

    [Column("id")] public long Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("eventType")] public string EventType { get; set; } = string.Empty;
    [Column("target")] public string Target { get; set; } = string.Empty;
    [Column("isEnabled")] public bool IsEnabled { get; set; }
    [Column("createdUtc")] public DateTime CreatedUtc { get; set; }
}
