using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Migrations;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Adds bounded attribution and content dimensions to installations upgraded from alpha.18.
/// </summary>
public sealed class AddSearchPulseContextColumnsMigration(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Alter.Table(SearchPulseEventDto.TableName)
            .AddColumn("contentKey").AsString(64).Nullable()
            .AddColumn("referrerDomain").AsString(64).Nullable()
            .AddColumn("utmSource").AsString(64).Nullable()
            .AddColumn("utmMedium").AsString(64).Nullable()
            .AddColumn("utmCampaign").AsString(64).Nullable()
            .Do();
        Alter.Table(SearchPulseEventQueueDto.TableName)
            .AddColumn("contentKey").AsString(64).Nullable()
            .AddColumn("referrerDomain").AsString(64).Nullable()
            .AddColumn("utmSource").AsString(64).Nullable()
            .AddColumn("utmMedium").AsString(64).Nullable()
            .AddColumn("utmCampaign").AsString(64).Nullable()
            .Do();
        Alter.Table(SearchPulseDailyAggregateDto.TableName)
            .AddColumn("contentKey").AsString(64).Nullable()
            .AddColumn("referrerDomain").AsString(64).Nullable()
            .AddColumn("utmSource").AsString(64).Nullable()
            .AddColumn("utmMedium").AsString(64).Nullable()
            .AddColumn("utmCampaign").AsString(64).Nullable()
            .Do();
        return Task.CompletedTask;
    }
}
