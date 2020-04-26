using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetectorProcess.Detectors
{
    internal interface IDetector
    {
        void FindArtifact(ref ArtifactRuntimeInformation runtimeInformation);
    }
}
