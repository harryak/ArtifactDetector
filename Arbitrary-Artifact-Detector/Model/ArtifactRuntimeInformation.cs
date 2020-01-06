using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    class ArtifactRuntimeInformation
    {
        public List<ProcessedImage> ReferenceImages { get; set; }

        public ProcessedImage Screenshot { get; set; }

        public IntPtr WindowHandle { get; set; }

        public string WindowTitle { get; set; }
    }
}
