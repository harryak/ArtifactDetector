using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                Logger.LogInformation("No possible icon titles given. Could not find matching tray icons.");
                return DetectorResponse.PresencePossible;
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.TrayIconDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching tray icons.");
            }

            if (runtimeInformation.CountTrayIcons > 0)
            {
                if (matchConditions != null)
                {
                    if (matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                    {
                        Logger.LogInformation("Found {0} matching tray icons and conditions for a match are met.", runtimeInformation.CountTrayIcons);
                        return DetectorResponse.PresenceCertain;
                    }
                    else
                    {
                        Logger.LogInformation("Found {0} matching tray icons, but conditions for a match are not met.", runtimeInformation.CountTrayIcons);
                        return DetectorResponse.PresenceImpossible;
                    }
                }

                Logger.LogInformation("Found {0} matching tray icons.", runtimeInformation.CountTrayIcons);
                return DetectorResponse.PresenceCertain;
            }

            Logger.LogInformation("No matching tray icons found.");
            return DetectorResponse.PresenceImpossible;
        }
    }
}
