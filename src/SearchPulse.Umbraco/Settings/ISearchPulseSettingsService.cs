namespace SearchPulse.Umbraco.Settings;

/// <summary>
/// Reads and updates the one operational control exposed in the backoffice.
/// </summary>
public interface ISearchPulseSettingsService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}
