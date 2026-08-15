using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using SearchPulse.Umbraco.Goals;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace SearchPulse.Umbraco.Controllers;

[VersionedApiBackOfficeRoute("searchpulse")]
public sealed class SearchPulseManagementController(
    ISearchPulseSettingsService settingsService,
    ISearchPulseOverviewService overviewService,
    ISearchPulseDataManagementService dataManagementService,
    ISearchPulseEventStore eventStore,
    ISearchPulseOperationalState operationalState,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor,
    ISearchPulseGoalService? goalService = null) : ManagementApiControllerBase
{
    [HttpGet("overview")]
    [ProducesResponseType<SearchPulseOverview>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<SearchPulseOverview> GetOverview([FromQuery] int rangeDays = 30, [FromQuery] string sort = "count")
    {
        if (!SearchPulseOverviewService.IsSupportedRange(rangeDays) || !TryParseSort(sort, out var overviewSort)) return BadRequest();
        return Ok(overviewService.GetOverview(rangeDays, overviewSort));
    }

    [HttpGet("content-performance")]
    public ActionResult<IReadOnlyList<SearchPulseContentSummary>> GetContentPerformance([FromQuery] int rangeDays = 30, [FromQuery] string sort = "count")
    {
        if (!SearchPulseOverviewService.IsSupportedRange(rangeDays) || !TryParseSort(sort, out var overviewSort)) return BadRequest();
        return Ok(overviewService.GetOverview(rangeDays, overviewSort).ContentPerformance ?? []);
    }

    [HttpGet("settings")]
    public ActionResult<SearchPulseSettingsResponse> GetSettings()
    {
        var queue = eventStore.GetQueueStatus();
        var worker = operationalState.GetStatus();
        var options = optionsMonitor.CurrentValue;
        return Ok(new SearchPulseSettingsResponse(settingsService.IsEnabled(), queue.PendingEvents, options.MaximumQueuedEvents, options.QueueWarningThresholdPercent, queue.OldestPendingEventUtc, worker.WorkerStarted, worker.LastSuccessfulBatchUtc, worker.LastFailureUtc, worker.FailedBatchCount, worker.LastProcessedCount));
    }

    [HttpPut("settings")]
    public IActionResult UpdateSettings(SearchPulseSettingsRequest request) { settingsService.SetEnabled(request.IsEnabled); return NoContent(); }

    [HttpDelete("settings/data")]
    public IActionResult ClearData([FromQuery] int rangeDays = 0) { if (!SearchPulseOverviewService.IsSupportedRange(rangeDays)) return BadRequest(); dataManagementService.Clear(rangeDays); return NoContent(); }

    [HttpGet("goals")]
    public ActionResult<IReadOnlyList<SearchPulseGoalDto>> GetGoals() => Ok(goalService?.GetGoals() ?? []);

    [HttpPost("goals")]
    public ActionResult<SearchPulseGoalDto> CreateGoal(SearchPulseGoalRequest request)
    {
        if (!TryValidateGoal(request, out var eventType)) return BadRequest();
        return Ok(goalService!.Create(request.Name.Trim(), eventType, request.Target.Trim()));
    }

    [HttpPut("goals/{id:long}")]
    public IActionResult UpdateGoal(long id, SearchPulseGoalRequest request)
    {
        if (!TryValidateGoal(request, out var eventType) || !goalService!.Update(id, request.Name.Trim(), eventType, request.Target.Trim(), request.IsEnabled)) return BadRequest();
        return NoContent();
    }

    [HttpDelete("goals/{id:long}")]
    public IActionResult DeleteGoal(long id) => goalService!.Delete(id) ? NoContent() : NotFound();

    private static bool TryValidateGoal(SearchPulseGoalRequest request, out SearchPulseEventType eventType) =>
        SearchPulseGoalValidator.TryValidate(request?.Name, request?.EventType, request?.Target, out eventType);

    private static bool TryParseSort(string? value, out SearchPulseOverviewSort sort)
    {
        switch (value?.Trim().ToLowerInvariant()) { case "count": sort = SearchPulseOverviewSort.Count; return true; case "name": sort = SearchPulseOverviewSort.Name; return true; default: sort = default; return false; }
    }
}

public sealed record SearchPulseSettingsResponse(bool IsEnabled, int PendingEvents, int MaximumQueuedEvents, int QueueWarningThresholdPercent, DateTime? OldestPendingEventUtc, bool WorkerStarted, DateTime? LastSuccessfulBatchUtc, DateTime? LastFailureUtc, int FailedBatchCount, int LastProcessedCount);
public sealed record SearchPulseSettingsRequest(bool IsEnabled);
public sealed record SearchPulseGoalRequest(string Name, string EventType, string Target, bool IsEnabled = true);
