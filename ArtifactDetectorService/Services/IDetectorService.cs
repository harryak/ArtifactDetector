using System.Collections.Generic;
using System.ServiceModel;
using ItsApe.ArtifactDetector.Models;

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
        /// <returns>True if successfully started.</returns>
        [OperationContract]
        bool StartWatch(string jsonEncodedParameters);

        /// <summary>
        /// Stop the current watching of an arifact.
        /// </summary>
        /// <param name="jsonEncodedParameters">JSON-configuration string.</param>
        /// <returns>File name of the output file.</returns>
        [OperationContract]
        string StopWatch(string jsonEncodedParameters);
    }
}
