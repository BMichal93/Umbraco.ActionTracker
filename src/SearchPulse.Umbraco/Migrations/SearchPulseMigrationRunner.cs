using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace SearchPulse.Umbraco.Migrations;

/// <summary>
/// Runs SearchPulse's one-time schema migration when Umbraco has a usable database.
/// </summary>
public sealed class SearchPulseMigrationRunner(
    ICoreScopeProvider coreScopeProvider,
    IMigrationPlanExecutor migrationPlanExecutor,
    IKeyValueService keyValueService,
    IRuntimeState runtimeState) : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        var migrationPlan = new MigrationPlan("SearchPulse");
        migrationPlan.From(string.Empty)
            .To<CreateSearchPulseEventTableMigration>("searchpulse-initial")
            .To<CreateSearchPulseEventQueueTableMigration>("searchpulse-event-queue")
            .To<CreateSearchPulseReportingIndexesMigration>("searchpulse-reporting-indexes")
            .To<CreateSearchPulseDailyAggregateTableMigration>("searchpulse-daily-aggregates")
            .To<AddSearchPulseContextColumnsMigration>("searchpulse-context-columns")
            .To<CreateSearchPulseGoalTableMigration>("searchpulse-goals");

        var upgrader = new Upgrader(migrationPlan);
        await upgrader.ExecuteAsync(migrationPlanExecutor, coreScopeProvider, keyValueService);
    }
}
