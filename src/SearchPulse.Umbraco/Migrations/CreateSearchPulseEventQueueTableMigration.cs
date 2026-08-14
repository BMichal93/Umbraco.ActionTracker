using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Creates the durable package-owned inbox used to protect visitor requests from reporting work.
/// </summary>
public sealed class CreateSearchPulseEventQueueTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Create.Table(SearchPulseEventQueueDto.TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("occurredUtc").AsDateTime().NotNullable()
            .WithColumn("eventType").AsString(32).NotNullable()
            .WithColumn("path").AsString(512).NotNullable()
            .WithColumn("target").AsString(80).Nullable()
            .WithColumn("leaseExpiresUtc").AsDateTime().Nullable()
            .WithColumn("leaseToken").AsString(36).Nullable()
            .WithColumn("processedUtc").AsDateTime().Nullable()
            .Do();
        Create.Index("IX_searchPulseEventQueue_pending")
            .OnTable(SearchPulseEventQueueDto.TableName)
            .OnColumn("processedUtc")
            .Ascending()
            .OnColumn("leaseExpiresUtc")
            .Ascending()
            .Do();
        return Task.CompletedTask;
    }
}