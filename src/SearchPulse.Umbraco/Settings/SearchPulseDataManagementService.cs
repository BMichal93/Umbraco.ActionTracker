using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SearchPulse.Umbraco.Settings;

/// <summary>
/// Clears both unprocessed and reported rows so a reset cannot be undone by an active queue worker.
/// </summary>
public sealed class SearchPulseDataManagementService(IScopeProvider scopeProvider) : ISearchPulseDataManagementService
{
    public void Clear(int rangeDays)
    {
        var since = SearchPulseOverviewService.GetReportingStartUtc(DateTime.UtcNow, rangeDays);
        object sinceParameter = since.HasValue ? since.Value : DBNull.Value;
        var clearedAtUtc = DateTime.UtcNow;

        using var scope = scopeProvider.CreateScope();
        scope.Database.Execute(
            $"UPDATE {SearchPulseEventQueueDto.TableName} SET processedUtc = @0 " +
            "WHERE processedUtc IS NULL AND (@1 IS NULL OR occurredUtc >= @1)",
            clearedAtUtc,
            sinceParameter);
        scope.Database.Execute(
            $"DELETE FROM {SearchPulseEventDto.TableName} WHERE (@0 IS NULL OR occurredUtc >= @0)",
            sinceParameter);
        scope.Database.Execute(
            $"DELETE FROM {SearchPulseEventQueueDto.TableName} " +
            "WHERE processedUtc IS NOT NULL AND (@0 IS NULL OR occurredUtc >= @0)",
            sinceParameter);
        if (rangeDays == 0)
        {
            scope.Database.Delete<SearchPulseDailyAggregateDto>("WHERE 1 = 1");
        }

        scope.Complete();
    }
}