using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect tray icons.
    /// </summary>
    internal class TrayIconDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                Logger.LogInformation("No possible icon titles given. Could not find matching tray icons.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.TrayIconDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching tray icons.");
            }

            if (runtimeInformation.CountTrayIcons > 0)
            {
                Logger.LogInformation("Found {0} matching tray icons.", runtimeInformation.CountTrayIcons);
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            Logger.LogInformation("No matching tray icons found.");
            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
