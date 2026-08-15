using System.Text.RegularExpressions;
using SearchPulse.Umbraco.Telemetry;

namespace SearchPulse.Umbraco.Goals;

public static partial class SearchPulseGoalValidator
{
    public static bool TryValidate(string? name, string? eventType, string? target, out SearchPulseEventType parsedEventType)
    {
        parsedEventType = default;
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80 || string.IsNullOrWhiteSpace(target) || target.Trim().Length > 80)
        {
            return false;
        }

        if (!Enum.TryParse(eventType, true, out parsedEventType)
            || parsedEventType is not (SearchPulseEventType.CustomAction or SearchPulseEventType.FormSubmit or SearchPulseEventType.FormSuccess or SearchPulseEventType.DownloadClick or SearchPulseEventType.ExternalLinkClick or SearchPulseEventType.SiteSearch)
            || !IsSafeTarget(parsedEventType, target.Trim()))
        {
            parsedEventType = default;
            return false;
        }

        return true;
    }

    private static bool IsSafeTarget(SearchPulseEventType eventType, string target) => eventType == SearchPulseEventType.DownloadClick
        ? target.StartsWith('/') && !target.Contains('?') && !target.Contains('#') && !target.Contains("//", StringComparison.Ordinal) && !target.Contains('\\')
        : TargetPattern().IsMatch(target);

    [GeneratedRegex("^[a-z0-9][a-z0-9._:/-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TargetPattern();
}
