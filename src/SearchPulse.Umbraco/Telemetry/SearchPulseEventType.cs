namespace SearchPulse.Umbraco.Telemetry;

/// <summary>
/// The small, supported set of anonymous content signals collected by SearchPulse.
/// </summary>
public enum SearchPulseEventType
{
    PageView,
    PageExit,
    Scroll25,
    Scroll50,
    Scroll75,
    ExternalLinkClick,
    DownloadClick,
    CustomAction,
    FormSubmit,
    VideoPlay,
}
