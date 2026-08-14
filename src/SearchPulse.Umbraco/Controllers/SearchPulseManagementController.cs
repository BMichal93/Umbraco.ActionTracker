using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchPulse.Umbraco.Overview;
using SearchPulse.Umbraco.Settings;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace SearchPulse.Umbraco.Controllers;

/// <summary>
/// The authenticated, backoffice-only API behind the two SearchPulse views.
/// </summary>
[VersionedApiBackOfficeRoute("searchpulse")]
public sealed class SearchPulseManagementController(
    ISearchPulseSettingsService settingsService,
    ISearchPulseOverviewService overviewService) : ManagementApiControllerBase
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

    [HttpDelete("overview")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ClearOverview([FromQuery] int rangeDays = 0)
    {
        if (!SearchPulseOverviewService.IsSupportedRange(rangeDays))
        {
            return BadRequest();
        }

        overviewService.Clear(rangeDays);
        return NoContent();
    }

    [HttpGet("settings")]
    [ProducesResponseType<SearchPulseSettingsResponse>(StatusCodes.Status200OK)]
    public ActionResult<SearchPulseSettingsResponse> GetSettings() => Ok(new SearchPulseSettingsResponse(settingsService.IsEnabled()));

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult UpdateSettings(SearchPulseSettingsRequest request)
    {
        settingsService.SetEnabled(request.IsEnabled);
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
/// The one persisted backoffice setting.
/// </summary>
public sealed record SearchPulseSettingsResponse(bool IsEnabled);

/// <summary>
/// A request to turn collection on or off.
/// </summary>
public sealed record SearchPulseSettingsRequest(bool IsEnabled);