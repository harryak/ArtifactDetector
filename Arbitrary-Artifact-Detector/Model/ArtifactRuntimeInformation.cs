using System;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    class ArtifactRuntimeInformation
    {
        public float WindowVisibility { get; set; }

        public ProcessedImage Screenshot { get; set; }

        public IntPtr WindowHandle { get; set; }

        public string WindowTitle { get; set; }
    }
}
