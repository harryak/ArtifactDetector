using System.ServiceModel;

namespace ArbitraryArtifactDetector.Service
{
    [ServiceContract(
        Name = "ITS.APE Detector Service",
        Namespace = "http://cs.uni-bonn.de/itsape/artifact-detector/v1.0"
        )]
    public interface IDetectorService
    {
        [OperationContract]
        void StartWatch(string artifactType, string detectorConfigurationString, string referenceImagePath, int intervalLength);

        [OperationContract]
        void StopWatch();
    }
}