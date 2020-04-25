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
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            var ProcessDirectory = ApplicationSetup.GetInstance().GetExecutingDirectory().FullName;
            var ProcessName = "\"" + Uri.UnescapeDataString(Path.Combine(ProcessDirectory, ApplicationConfiguration.OpenWindowDetectorExe)) + "\"";

            // Get runtime information into memory mapped file for external process.
            byte[] serializedRuntimeInformation;
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, runtimeInformation);
                serializedRuntimeInformation = memoryStream.ToArray();
            }
            var security = new MemoryMappedFileSecurity();
            security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Translate(typeof(NTAccount)), MemoryMappedFileRights.FullControl, AccessControlType.Allow));
            using (var memoryMappedFile = MemoryMappedFile.CreateOrOpen(@"Global\" + ApplicationConfiguration.MemoryMappedFileName, serializedRuntimeInformation.LongLength, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, HandleInheritability.Inheritable))
            {
                using (var memoryStream = memoryMappedFile.CreateViewStream())
                {
                    binaryFormatter.Serialize(memoryStream, runtimeInformation);
                }

                int processId = CreateProcessAsUser(ProcessName);
                if (processId < 0)
                {
                    Logger.LogError("Could not start external detection program.");
                    return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
                }

                var process = Process.GetProcessById(processId);
                var exec = process.StartInfo.FileName;
                process.WaitForExit();

                // Get runtime information back from memory mapped file from external process.
                using (var memoryStream = memoryMappedFile.CreateViewStream())
                {
                    runtimeInformation = (ArtifactRuntimeInformation) binaryFormatter.Deserialize(memoryStream);
                }
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };

            /*
            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.WindowHandles.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                Logger.LogInformation("No matching windows or possible window titles given for detector. Only getting visible windows now.");
                return FindArtifactLight(ref runtimeInformation);
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeVisibleWindows(ref runtimeInformation);

            return PrepareResponse(ref runtimeInformation);*/
        }
    }
}
