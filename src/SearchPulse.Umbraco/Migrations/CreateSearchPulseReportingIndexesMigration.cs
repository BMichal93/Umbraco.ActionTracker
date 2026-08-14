using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Adds access paths for the time-bounded page and interaction summaries.
/// </summary>
public sealed class CreateSearchPulseReportingIndexesMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Create.Index("IX_searchPulseEvent_eventType_occurredUtc_path")
            .OnTable(SearchPulseEventDto.TableName)
            .OnColumn("eventType")
            .Ascending()
            .OnColumn("occurredUtc")
            .Ascending()
            .OnColumn("path")
            .Ascending()
            .Do();
        Create.Index("IX_searchPulseEvent_eventType_occurredUtc_target")
            .OnTable(SearchPulseEventDto.TableName)
            .OnColumn("eventType")
            .Ascending()
            .OnColumn("occurredUtc")
            .Ascending()
            .OnColumn("target")
            .Ascending()
            .Do();
        return Task.CompletedTask;
    }
}
