using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Converts a browser request into a fixed, safe event model and rejects free-form data.
/// </summary>
public sealed partial class SearchPulseEventRequestValidator
{
    private const int MaximumPathLength = 512;
    private const int MaximumTargetLength = 80;
    private const int MaximumContextLength = 64;

    public static bool TryValidate(SearchPulseEventRequest? request, [NotNullWhen(true)] out SearchPulseEvent? searchPulseEvent)
    {
        searchPulseEvent = null;

        if (request is null || !TryParseType(request.Type, out var type) || !IsSafePath(request.Path)
            || !IsSafeToken(request.ContentKey, MaximumContextLength)
            || !IsSafeDomain(request.ReferrerDomain)
            || !IsSafeToken(request.UtmSource, MaximumContextLength)
            || !IsSafeToken(request.UtmMedium, MaximumContextLength)
            || !IsSafeToken(request.UtmCampaign, MaximumContextLength))
        {
            return false;
        }

        if (!IsSafeTarget(type, request.Target))
        {
            return false;
        }

        searchPulseEvent = new SearchPulseEvent(
            type,
            request.Path!,
            request.Target,
            request.ContentKey,
            request.ReferrerDomain,
            request.UtmSource,
            request.UtmMedium,
            request.UtmCampaign);
        return true;
    }

    private static bool TryParseType(string? value, out SearchPulseEventType type)
    {
        type = value switch
        {
            "page-view" => SearchPulseEventType.PageView,
            "page-exit" => SearchPulseEventType.PageExit,
            "scroll-25" => SearchPulseEventType.Scroll25,
            "scroll-50" => SearchPulseEventType.Scroll50,
            "scroll-75" => SearchPulseEventType.Scroll75,
            "external-link-click" => SearchPulseEventType.ExternalLinkClick,
            "download-click" => SearchPulseEventType.DownloadClick,
            "custom-action" => SearchPulseEventType.CustomAction,
            "form-submit" => SearchPulseEventType.FormSubmit,
            "form-success" => SearchPulseEventType.FormSuccess,
            "video-play" => SearchPulseEventType.VideoPlay,
            "site-search" => SearchPulseEventType.SiteSearch,
            "active-engagement" => SearchPulseEventType.ActiveEngagement,
            "low-engagement-exit" => SearchPulseEventType.LowEngagementExit,
            _ => default,
        };

        return value is "page-view" or "page-exit" or "scroll-25" or "scroll-50" or "scroll-75"
            or "external-link-click" or "download-click" or "custom-action" or "form-submit"
            or "form-success" or "video-play" or "site-search" or "active-engagement" or "low-engagement-exit";
    }

    private static bool IsSafePath(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaximumPathLength
        && value.StartsWith('/')
        && !value.Contains('?')
        && !value.Contains('#')
        && !value.Contains("//", StringComparison.Ordinal)
        && !value.Contains('\\');

    private static bool IsSafeTarget(SearchPulseEventType type, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (value.Length > MaximumTargetLength)
        {
            return false;
        }

        return type == SearchPulseEventType.DownloadClick
            ? IsSafePath(value)
            : TargetPattern().IsMatch(value)
                && type is SearchPulseEventType.ExternalLinkClick
                    or SearchPulseEventType.CustomAction
                    or SearchPulseEventType.FormSubmit
                    or SearchPulseEventType.FormSuccess
                    or SearchPulseEventType.VideoPlay
                    or SearchPulseEventType.SiteSearch
                    or SearchPulseEventType.ActiveEngagement
                    or SearchPulseEventType.LowEngagementExit;
    }

    private static bool IsSafeToken(string? value, int maximumLength) =>
        string.IsNullOrEmpty(value) || (value.Length <= maximumLength && TokenPattern().IsMatch(value));

    private static bool IsSafeDomain(string? value) =>
        string.IsNullOrEmpty(value) || (value.Length <= MaximumContextLength && DomainPattern().IsMatch(value));

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex TargetPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]*\\.[a-z]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainPattern();
}
