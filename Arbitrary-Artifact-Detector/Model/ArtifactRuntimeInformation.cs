using System;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    class ArtifactRuntimeInformation
    {
        public float WindowVisibility { get; set; } = 0f;

        public ProcessedImage Screenshot { get; set; }

        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        public string WindowTitle { get; set; } = "";
    }
}
