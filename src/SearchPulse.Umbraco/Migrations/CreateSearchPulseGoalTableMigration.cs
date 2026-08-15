using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

public sealed class CreateSearchPulseGoalTableMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Create.Table(SearchPulseGoalDto.TableName)
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("name").AsString(80).NotNullable()
            .WithColumn("eventType").AsString(32).NotNullable()
            .WithColumn("target").AsString(80).NotNullable()
            .WithColumn("isEnabled").AsBoolean().NotNullable()
            .WithColumn("createdUtc").AsDateTime().NotNullable()
            .Do();
        Create.Index("IX_searchPulseGoal_eventType_target")
            .OnTable(SearchPulseGoalDto.TableName)
            .OnColumn("eventType").Ascending()
            .OnColumn("target").Ascending()
            .Do();
        return Task.CompletedTask;
    }
}
