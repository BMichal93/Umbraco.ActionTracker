using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace SearchPulse.Umbraco.Controllers;

/// <summary>
/// The authenticated, backoffice-only API behind the two SearchPulse views.
/// </summary>
[VersionedApiBackOfficeRoute("searchpulse")]
public sealed class SearchPulseManagementController(
    ISearchPulseSettingsService settingsService,
    ISearchPulseOverviewService overviewService,
    ISearchPulseDataManagementService dataManagementService,
    ISearchPulseEventStore eventStore,
    ISearchPulseOperationalState operationalState,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ManagementApiControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<SearchPulseOverview>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SearchPulseOverview> GetOverview(
        [FromQuery] int rangeDays = 30,
        [FromQuery] string sort = "count")
    {
        if (!SearchPulseOverviewService.IsSupportedRange(rangeDays)
            || !TryParseSort(sort, out var overviewSort))
        {
            return BadRequest();
        }

        return Ok(overviewService.GetOverview(rangeDays, overviewSort));
    }

    [HttpGet("settings")]
    [ProducesResponseType<SearchPulseSettingsResponse>(StatusCodes.Status200OK)]
    public ActionResult<SearchPulseSettingsResponse> GetSettings()
    {
        var queue = eventStore.GetQueueStatus();
        var worker = operationalState.GetStatus();
        var options = optionsMonitor.CurrentValue;
        return Ok(new SearchPulseSettingsResponse(
            settingsService.IsEnabled(),
            queue.PendingEvents,
            options.MaximumQueuedEvents,
            options.QueueWarningThresholdPercent,
            queue.OldestPendingEventUtc,
            worker.WorkerStarted,
            worker.LastSuccessfulBatchUtc,
            worker.LastFailureUtc,
            worker.FailedBatchCount,
            worker.LastProcessedCount));
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult UpdateSettings(SearchPulseSettingsRequest request)
    {
        settingsService.SetEnabled(request.IsEnabled);
        return NoContent();
    }

    [HttpDelete("settings/data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ClearData([FromQuery] int rangeDays = 0)
    {
        if (!SearchPulseOverviewService.IsSupportedRange(rangeDays))
        {
            return BadRequest();
        }

        dataManagementService.Clear(rangeDays);
        return NoContent();
    }

    private static bool TryParseSort(string? value, out SearchPulseOverviewSort sort)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "count":
                sort = SearchPulseOverviewSort.Count;
                return true;
            case "name":
                sort = SearchPulseOverviewSort.Name;
                return true;
            default:
                sort = default;
                return false;
        }
    }
}

/// <summary>
/// The operational controls and queue state presented in the backoffice.
/// </summary>
public sealed record SearchPulseSettingsResponse(
    bool IsEnabled,
    int PendingEvents,
    int MaximumQueuedEvents,
    int QueueWarningThresholdPercent,
    DateTime? OldestPendingEventUtc,
    bool WorkerStarted,
    DateTime? LastSuccessfulBatchUtc,
    DateTime? LastFailureUtc,
    int FailedBatchCount,
    int LastProcessedCount);

/// <summary>
/// A request to turn collection on or off.
/// </summary>
public sealed record SearchPulseSettingsRequest(bool IsEnabled);