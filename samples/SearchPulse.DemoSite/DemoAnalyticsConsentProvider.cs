using Microsoft.AspNetCore.Http;
using SearchPulse.Umbraco.Consent;

namespace SearchPulse.DemoSite;

/// <summary>
/// Keeps the demo consent boundary explicit. Production sites replace this with their existing CMP integration.
/// </summary>
public sealed class DemoAnalyticsConsentProvider : IAnalyticsConsentProvider
{
    public ValueTask<bool> HasAnalyticsConsentAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var isGranted = httpContext.Request.Cookies.TryGetValue("SearchPulseDemoConsent", out var value)
            && string.Equals(value, "yes", StringComparison.Ordinal);
        return ValueTask.FromResult(isGranted);
    }
}