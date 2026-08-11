using Microsoft.Extensions.Options;
using SearchPulse.Umbraco.Configuration;
using Umbraco.Cms.Core.Services;

namespace SearchPulse.Umbraco.Settings;

/// <summary>
/// Persists the backoffice toggle while allowing app settings to provide its safe default.
/// </summary>
public sealed class SearchPulseSettingsService(
    IKeyValueService keyValueService,
    IOptionsMonitor<SearchPulseOptions> optionsMonitor) : ISearchPulseSettingsService
{
    private const string EnabledKey = "SearchPulse.Enabled";

    public bool IsEnabled()
    {
        var persistedValue = keyValueService.GetValue(EnabledKey);
        return bool.TryParse(persistedValue, out var isEnabled)
            ? isEnabled
            : optionsMonitor.CurrentValue.Enabled;
    }

    public void SetEnabled(bool enabled) => keyValueService.SetValue(EnabledKey, enabled.ToString());
}
