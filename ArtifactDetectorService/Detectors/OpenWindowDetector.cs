using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect if a window is opened and visible.
    /// </summary>
    internal class OpenWindowDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            if (runtimeInformation.PossibleWindowTitleSubstrings.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            runtimeInformation.DetectorToRun = ExternalDetector.OpenWindowDetector;
            SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation);

            if (runtimeInformation.CountOpenWindows > 0)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
