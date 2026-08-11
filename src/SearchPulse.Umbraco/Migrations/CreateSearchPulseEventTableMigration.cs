using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Creates the minimal, package-owned event table once.
/// </summary>
public sealed class CreateSearchPulseEventTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Create.Table(SearchPulseEventDto.TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("occurredUtc").AsDateTime().NotNullable()
            .WithColumn("eventType").AsString(32).NotNullable()
            .WithColumn("path").AsString(512).NotNullable()
            .WithColumn("target").AsString(80).Nullable()
            .Do();
        return Task.CompletedTask;
    }
}
