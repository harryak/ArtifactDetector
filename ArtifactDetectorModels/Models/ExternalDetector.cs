namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Enumeration of detectors available in the detector process.
    /// </summary>
    public enum ExternalProcessCommand
    {
        None,
        DesktopIconDetector,
        OpenWindowDetector,
        TrayIconDetector,
        ScreenshotCapturer
    }
}