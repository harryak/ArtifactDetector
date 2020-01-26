using System.ServiceModel;

namespace ArbitraryArtifactDetector.Service
{
    [ServiceContract(
        Name = "DetectorService",
        Namespace = "http://cs.uni-bonn.de/itsape/artifact-detector/v1.0"
        )]
    public interface IDetectorService
    {
        [OperationContract]
        bool StartWatch(string artifactType, string detectorConfigurationString, string referenceImagePath, int intervalLength);

        [OperationContract]
        bool StopWatch();
    }
}