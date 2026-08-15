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

    [Column("id")] public long Id { get; set; }
    [Column("bucketKey")] public string BucketKey { get; set; } = string.Empty;
    [Column("occurredDateUtc")] public DateTime OccurredDateUtc { get; set; }
    [Column("eventType")] public string EventType { get; set; } = string.Empty;
    [Column("path")] public string Path { get; set; } = string.Empty;
    [Column("target")] public string Target { get; set; } = string.Empty;
    [Column("contentKey")] public string? ContentKey { get; set; }
    [Column("referrerDomain")] public string? ReferrerDomain { get; set; }
    [Column("utmSource")] public string? UtmSource { get; set; }
    [Column("utmMedium")] public string? UtmMedium { get; set; }
    [Column("utmCampaign")] public string? UtmCampaign { get; set; }
    [Column("eventCount")] public long EventCount { get; set; }

    public static string CreateBucketKey(
        DateTime occurredDateUtc,
        string eventType,
        string path,
        string? target,
        string? contentKey = null,
        string? referrerDomain = null,
        string? utmSource = null,
        string? utmMedium = null,
        string? utmCampaign = null)
    {
        var normalizedDate = occurredDateUtc.Date;
        if (contentKey is null && referrerDomain is null && utmSource is null && utmMedium is null && utmCampaign is null)
        {
            var legacyValue = string.Format(CultureInfo.InvariantCulture, "{0:yyyyMMdd}|{1}:{2}|{3}:{4}|{5}:{6}", normalizedDate, eventType.Length, eventType, path.Length, path, (target ?? string.Empty).Length, target ?? string.Empty);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(legacyValue))).ToLowerInvariant();
        }

        var value = string.Join('|',
            normalizedDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            eventType,
            path,
            target ?? string.Empty,
            contentKey ?? string.Empty,
            referrerDomain ?? string.Empty,
            utmSource ?? string.Empty,
            utmMedium ?? string.Empty,
            utmCampaign ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
