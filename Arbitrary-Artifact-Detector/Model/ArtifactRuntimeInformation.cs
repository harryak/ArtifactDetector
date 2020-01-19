using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    internal class ArtifactRuntimeInformation
    {
        public string ProcessName { get; set; } = "";
        public ProcessedImage Screenshot { get; set; }
        public IList<WindowToplevelInformation> MatchingWindows { get; set; } = new List<WindowToplevelInformation>();
        public string WindowTitle { get; internal set; } = "";
        public IntPtr WindowHandle { get; internal set; } = IntPtr.Zero;

        public void PreFillWith(ArtifactRuntimeInformation runtimeInformation)
        {
            ProcessName = runtimeInformation.ProcessName;
            Screenshot = runtimeInformation.Screenshot;
            WindowHandle = runtimeInformation.WindowHandle;
            MatchingWindows = runtimeInformation.MatchingWindows;
            WindowTitle = runtimeInformation.WindowTitle;
        }
    }
}