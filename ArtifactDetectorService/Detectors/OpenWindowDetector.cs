using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            if (runtimeInformation.WindowHandles.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                Logger.LogInformation("No windows information or possible window titles given. Could not find matching open windows.");
                return DetectorResponse.PresencePossible;
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.OpenWindowDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching open windows.");
            }

            if (runtimeInformation.CountOpenWindows > 0)
            {
                if (matchConditions != null)
                {
                    if (matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                    {
                        Logger.LogInformation("Found {0} matching open windows and conditions for a match are met.", runtimeInformation.CountOpenWindows);
                        return DetectorResponse.PresenceCertain;
                    }
                    else
                    {
                        Logger.LogInformation("Found {0} matching open windows, but conditions for a match are not met.", runtimeInformation.CountOpenWindows);
                        return DetectorResponse.PresenceImpossible;
                    }
                }

                Logger.LogInformation("Found {0} matching open windows.", runtimeInformation.CountOpenWindows);
                return DetectorResponse.PresenceCertain;
            }

            Logger.LogInformation("Found no matching open windows.");
            return DetectorResponse.PresenceImpossible;
        }
    }
}
