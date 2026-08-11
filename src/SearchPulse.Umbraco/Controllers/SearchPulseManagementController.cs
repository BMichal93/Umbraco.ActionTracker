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
    public ActionResult<SearchPulseOverview> GetOverview() => Ok(overviewService.GetLastThirtyDays());

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
}

/// <summary>
/// The one persisted backoffice setting.
/// </summary>
public sealed record SearchPulseSettingsResponse(bool IsEnabled);

/// <summary>
/// A request to turn collection on or off.
/// </summary>
public sealed record SearchPulseSettingsRequest(bool IsEnabled);
