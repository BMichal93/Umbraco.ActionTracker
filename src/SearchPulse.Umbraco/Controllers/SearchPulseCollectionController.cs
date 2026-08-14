using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SearchPulse.Umbraco.Consent;
using SearchPulse.Umbraco.Settings;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.Controllers;

/// <summary>
/// Receives the small, same-origin browser payload produced by SearchPulse's client script.
/// </summary>
[ApiController]
[Route("/searchpulse/collect")]
[Consumes("application/json")]
public sealed class SearchPulseCollectionController(
    ISearchPulseSettingsService settingsService,
    IAnalyticsConsentProvider analyticsConsentProvider,
    ISearchPulseEventStore eventStore) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(1024)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CollectAsync(
        [FromBody] SearchPulseEventRequest? request,
        CancellationToken cancellationToken)
    {
        if (!settingsService.IsEnabled())
        {
            return NotFound();
        }

        if (!IsSameOrigin(Request))
        {
            return BadRequest();
        }

        if (!await analyticsConsentProvider.HasAnalyticsConsentAsync(HttpContext, cancellationToken))
        {
            return NoContent();
        }

        if (!SearchPulseEventRequestValidator.TryValidate(request, out var searchPulseEvent))
        {
            return BadRequest();
        }

        var result = await eventStore.RecordAsync(searchPulseEvent, cancellationToken);
        return result == SearchPulseEventRecordResult.Accepted
            ? Accepted()
            : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool IsSameOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        var currentOrigin = $"{request.Scheme}://{request.Host}";
        return string.Equals(origin, currentOrigin, StringComparison.OrdinalIgnoreCase);
    }
}