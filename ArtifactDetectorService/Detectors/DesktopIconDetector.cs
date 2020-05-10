using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                Logger.LogInformation("No possible icon titles given. Could not find matching desktop icons.");
                return DetectorResponse.PresencePossible;
            }

            runtimeInformation.ProcessCommand = ExternalProcessCommand.DesktopIconDetector;
            if (!SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation))
            {
                Logger.LogInformation("Could not call detector process to find matching desktop icons.");
            }

            if (runtimeInformation.CountDesktopIcons > 0)
            {
                if (matchConditions != null)
                {
                    if (matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                    {
                        Logger.LogInformation("Found {0} matching desktop icons and conditions for a match are met.", runtimeInformation.CountDesktopIcons);
                        return DetectorResponse.PresenceCertain;
                    }
                    else
                    {
                        Logger.LogInformation("Found {0} matching desktop icons, but conditions for a match are not met.", runtimeInformation.CountDesktopIcons);
                        return DetectorResponse.PresenceImpossible;
                    }
                }

                Logger.LogInformation("Found {0} matching desktop icons.", runtimeInformation.CountDesktopIcons);
                return DetectorResponse.PresenceCertain;
            }

            Logger.LogInformation("No matching desktop icons found.");
            return DetectorResponse.PresenceImpossible;
        }
    }
}
