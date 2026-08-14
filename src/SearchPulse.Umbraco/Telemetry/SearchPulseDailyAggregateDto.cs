using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NPoco;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Stores one immutable anonymous event count per UTC day and signal dimension.
/// </summary>
[TableName(TableName)]
[PrimaryKey(nameof(Id), AutoIncrement = true)]
public sealed class SearchPulseDailyAggregateDto
{
    public const string TableName = "searchPulseDailyAggregate";

    [Column("id")]
    public long Id { get; set; }

    [Column("bucketKey")]
    public string BucketKey { get; set; } = string.Empty;

    [Column("occurredDateUtc")]
    public DateTime OccurredDateUtc { get; set; }

    [Column("eventType")]
    public string EventType { get; set; } = string.Empty;

    [Column("path")]
    public string Path { get; set; } = string.Empty;

    [Column("target")]
    public string Target { get; set; } = string.Empty;

    [Column("eventCount")]
    public long EventCount { get; set; }

    public static string CreateBucketKey(DateTime occurredDateUtc, string eventType, string path, string? target)
    {
        var normalizedDate = occurredDateUtc.Date;
        var normalizedTarget = target ?? string.Empty;
        var value = string.Format(
            CultureInfo.InvariantCulture,
            "{0:yyyyMMdd}|{1}:{2}|{3}:{4}|{5}:{6}",
            normalizedDate,
            eventType.Length,
            eventType,
            path.Length,
            path,
            normalizedTarget.Length,
            normalizedTarget);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}