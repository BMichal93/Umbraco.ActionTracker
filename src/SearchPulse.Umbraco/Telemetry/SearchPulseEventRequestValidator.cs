using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// Converts a browser request into a fixed, safe event model.
/// </summary>
public sealed partial class SearchPulseEventRequestValidator
{
    private const int MaximumPathLength = 512;
    private const int MaximumTargetLength = 80;

    public static bool TryValidate(SearchPulseEventRequest? request, [NotNullWhen(true)] out SearchPulseEvent? searchPulseEvent)
    {
        searchPulseEvent = null;

        if (request is null || !TryParseType(request.Type, out var type) || !IsSafePath(request.Path))
        {
            return false;
        }

        if (!IsSafeTarget(type, request.Target))
        {
            return false;
        }

        searchPulseEvent = new SearchPulseEvent(type, request.Path!, request.Target);
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
            _ => default,
        };

        return value is "page-view" or "page-exit" or "scroll-25" or "scroll-50" or "scroll-75" or "external-link-click" or "download-click" or "custom-action";
    }

    private static bool IsSafePath(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= MaximumPathLength
            && value.StartsWith('/')
            && !value.Contains('?')
            && !value.Contains('#')
            && !value.Contains("//", StringComparison.Ordinal)
            && !value.Contains('\\');
    }

    private static bool IsSafeTarget(SearchPulseEventType type, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return value.Length <= MaximumTargetLength
            && TargetPattern().IsMatch(value)
            && type is SearchPulseEventType.ExternalLinkClick or SearchPulseEventType.DownloadClick or SearchPulseEventType.CustomAction;
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex TargetPattern();
}
