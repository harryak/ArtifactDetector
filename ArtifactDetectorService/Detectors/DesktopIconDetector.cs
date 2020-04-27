using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect desktop icons.
    /// </summary>
    internal class DesktopIconDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            runtimeInformation.DetectorToRun = ExternalDetector.DesktopIconDetector;
            SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation);

            if (runtimeInformation.CountDesktopIcons > 0)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
