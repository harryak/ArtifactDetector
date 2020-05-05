using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using Microsoft.Extensions.Logging;

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
                Logger.LogInformation("No possible icon titles given. Could not find matching desktop icons.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.DesktopIconDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching desktop icons.");
            }

            if (runtimeInformation.CountDesktopIcons > 0)
            {
                Logger.LogInformation("Found {0} matching desktop icons.", runtimeInformation.CountDesktopIcons);
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            Logger.LogInformation("No matching desktop icons found.");
            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
