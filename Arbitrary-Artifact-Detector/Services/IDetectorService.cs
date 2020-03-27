using System.ServiceModel;

namespace ItsApe.ArtifactDetector.Services
{
    /// <summary>
    /// Service contract for interaction with this service.
    /// </summary>
    [ServiceContract(
        Name = "DetectorService",
        Namespace = "http://cs.uni-bonn.de/itsape/artifact-detector/v1.0"
        )]
    public interface IDetectorService
    {
        /// <summary>
        /// Start watching an arifact.
        /// </summary>
        /// <param name="jsonEncodedParameters">JSON-configuration string.</param>
        /// <returns></returns>
        [OperationContract]
        bool StartWatch(string jsonEncodedParameters);

        /// <summary>
        /// Stop the current watching of an arifact.
        /// </summary>
        /// <param name="jsonEncodedParameters">JSON-configuration string.</param>
        /// <returns></returns>
        [OperationContract]
        string StopWatch(string jsonEncodedParameters);
    }
}
