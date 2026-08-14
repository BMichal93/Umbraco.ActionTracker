using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Creates compact, immutable daily totals before detailed anonymous rows expire.
/// </summary>
public sealed class CreateSearchPulseDailyAggregateTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Create.Table(SearchPulseDailyAggregateDto.TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("bucketKey").AsString(64).NotNullable().Unique()
            .WithColumn("occurredDateUtc").AsDateTime().NotNullable()
            .WithColumn("eventType").AsString(32).NotNullable()
            .WithColumn("path").AsString(512).NotNullable()
            .WithColumn("target").AsString(80).NotNullable()
            .WithColumn("eventCount").AsInt64().NotNullable()
            .Do();
        Create.Index("IX_searchPulseDailyAggregate_eventType_occurredDateUtc_path")
            .OnTable(SearchPulseDailyAggregateDto.TableName)
            .OnColumn("eventType")
            .Ascending()
            .OnColumn("occurredDateUtc")
            .Ascending()
            .OnColumn("path")
            .Ascending()
            .Do();
        Create.Index("IX_searchPulseDailyAggregate_eventType_occurredDateUtc_target")
            .OnTable(SearchPulseDailyAggregateDto.TableName)
            .OnColumn("eventType")
            .Ascending()
            .OnColumn("occurredDateUtc")
            .Ascending()
            .OnColumn("target")
            .Ascending()
            .Do();
        return Task.CompletedTask;
    }
}