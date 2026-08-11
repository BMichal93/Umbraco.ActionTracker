using Microsoft.AspNetCore.Http;

namespace SearchPulse.Umbraco.Consent;

/// <summary>
/// Lets the host website connect its existing consent-management solution.
/// SearchPulse never assumes that consent has been granted.
/// </summary>
public interface IAnalyticsConsentProvider
{
    ValueTask<bool> HasAnalyticsConsentAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
