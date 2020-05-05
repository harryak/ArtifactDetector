using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using Microsoft.Extensions.Logging;

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
            if (runtimeInformation.WindowHandles.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                Logger.LogInformation("No windows information or possible window titles given. Could not find matching open windows.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.OpenWindowDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching open windows.");
            }

            if (runtimeInformation.CountOpenWindows > 0)
            {
                Logger.LogInformation("Found {0} matching open windows.", runtimeInformation.CountRunningProcesses);
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            Logger.LogInformation("Found no matching open windows.");
            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
