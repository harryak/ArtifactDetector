using System;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Principal;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using ItsApe.ArtifactDetector.Utilities;
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
            SessionManager.GetInstance().CallDetectorProcess(sessionId, ref runtimeInformation);
            var fromMemoryTime = DateTime.Now;

            Logger.LogDebug("Timings were: {0:mmssffff},{1:mmssffff}", startTime, fromMemoryTime);

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

        /// <summary>
        /// Get the serialized object's length by ... well serializing it.
        /// </summary>
        /// <param name="runtimeInformation">Object to serialize</param>
        /// <param name="binaryFormatter">Formatter instance to use.</param>
        /// <param name="serializedObjectLength">Out parameter of instance.</param>
        private void GetSerializedObjectLength(ref ArtifactRuntimeInformation runtimeInformation, ref BinaryFormatter binaryFormatter, out long serializedObjectLength)
        {
            serializedObjectLength = 0;
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, runtimeInformation);
                serializedObjectLength = memoryStream.Length;
            }
        }
    }
}
