using Microsoft.AspNetCore.Http;

namespace SearchPulse.Umbraco.Consent;

internal sealed class DenyAnalyticsConsentProvider : IAnalyticsConsentProvider
{
    public ValueTask<bool> HasAnalyticsConsentAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(false);
    }
}
