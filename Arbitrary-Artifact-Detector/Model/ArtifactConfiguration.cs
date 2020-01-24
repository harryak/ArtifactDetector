using ArbitraryArtifactDetector.Detector;

namespace ArbitraryArtifactDetector.Model
{
    internal class ArtifactConfiguration
    {
        public IDetector Detector { get; private set; }
        public ArtifactRuntimeInformation RuntimeInformation { get; private set; }
    }
}