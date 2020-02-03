using System.ServiceModel;

namespace ArbitraryArtifactDetector.Services
{
    [ServiceContract(
        Name = "DetectorService",
        Namespace = "http://cs.uni-bonn.de/itsape/artifact-detector/v1.0"
        )]
    public interface IDetectorService
    {
        [OperationContract]
        bool StartWatch(string jsonEncodedParameters);

        [OperationContract]
        string StopWatch(string jsonEncodedParameters);
    }
}