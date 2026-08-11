using Microsoft.AspNetCore.Http;
using SearchPulse.Umbraco.Consent;

namespace SearchPulse.Umbraco.Integration;

/// <summary>
/// Test-only consent boundary. Integration requests grant analytics consent by sending
/// the SearchPulseIntegrationConsent=yes cookie; all other requests remain denied.
/// </summary>
public sealed class TestAnalyticsConsentProvider : IAnalyticsConsentProvider
{
    public ValueTask<bool> HasAnalyticsConsentAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var isGranted = httpContext.Request.Cookies.TryGetValue("SearchPulseIntegrationConsent", out var value)
            && string.Equals(value, "yes", StringComparison.Ordinal);

        return ValueTask.FromResult(isGranted);
    }
}
