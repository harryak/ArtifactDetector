using System.ServiceModel;

namespace ArbitraryArtifactDetector.Service
{
    [ServiceContract(
        Name = "DetectorService",
        Namespace = "http://cs.uni-bonn.de/itsape/artifact-detector/v1.0"
        )]
    public interface IDetectorService
    {
        void StartWatch(string artifactConfigurationString);

        void StopWatch();
    }
}