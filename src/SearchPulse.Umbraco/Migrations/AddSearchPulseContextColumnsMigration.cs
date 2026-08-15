using System.Globalization;
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
        foreach (var tableName in new[]
        {
            SearchPulseEventDto.TableName,
            SearchPulseEventQueueDto.TableName,
            SearchPulseDailyAggregateDto.TableName
        })
        {
            AddBoundedColumn(tableName, "contentKey");
            AddBoundedColumn(tableName, "referrerDomain");
            AddBoundedColumn(tableName, "utmSource");
            AddBoundedColumn(tableName, "utmMedium");
            AddBoundedColumn(tableName, "utmCampaign");
        }

        return Task.CompletedTask;
    }

    private void AddBoundedColumn(string tableName, string columnName)
    {
        if (ColumnExists(tableName, columnName))
        {
            return;
        }

        var columnDefinition = $"{SqlSyntax.GetQuotedColumnName(columnName)} NVARCHAR(64) NULL";
        Execute.Sql(string.Format(CultureInfo.InvariantCulture, SqlSyntax.AddColumn, SqlSyntax.GetQuotedTableName(tableName), columnDefinition)).Do();
    }
}
