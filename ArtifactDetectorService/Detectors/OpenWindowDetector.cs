using System;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect if a window is opened and visible.
    ///
    /// Needs:  WindowHandle _or_ WindowTitle
    /// Yields: WindowHandle, WindowTitle
    /// </summary>
    internal class OpenWindowDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Special window handle to "desktop window".
        /// </summary>
        private IntPtr ProgramManagerWindowHandle { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            var startTime = DateTime.Now;
            runtimeInformation.DetectorToRun = ExternalDetector.OpenWindowDetector;
            SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation);
            var fromMemoryTime = DateTime.Now;

            Logger.LogDebug("Timings were: {0:ssffff},{1:ssffff}", startTime, fromMemoryTime);

            if (runtimeInformation.PossibleWindowTitleSubstrings.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }
            else if (runtimeInformation.CountOpenWindows > 0)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
