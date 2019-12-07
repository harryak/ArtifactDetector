using ArbitraryArtifactDetector.Models;

namespace ArbitraryArtifactDetector.Detector
{
    interface IDetector
    {
        DetectorResponse FindArtifact(Setup setup);
    }
}
