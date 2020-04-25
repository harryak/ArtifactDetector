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
            // Backup non-serialized property.
            var referenceImageBackup = runtimeInformation.ReferenceImages;

            // Get runtime information into memory mapped file for external process.
            var ProcessName = GetExternalProcessName();
            var binaryFormatter = new BinaryFormatter();
            GetSerializedObjectLength(ref runtimeInformation, ref binaryFormatter, out long serializedObjectLength);
            GetSecurityIdentifier(out var fileSecurity);

            // Open a memory mapped file to exchange data with the external process.
            using (var memoryMappedFile = MemoryMappedFile.CreateOrOpen(
                @"Global\" + ApplicationConfiguration.MemoryMappedFileName,
                serializedObjectLength * 2,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None,
                fileSecurity, HandleInheritability.Inheritable))
            {
                // Get the runtime information into the memory mapped file.
                using (var memoryStream = memoryMappedFile.CreateViewStream())
                {
                    binaryFormatter.Serialize(memoryStream, runtimeInformation);
                }

                // Start external executable in user session.
                int processId = CreateProcessAsUser(ProcessName);
                if (processId < 0)
                {
                    Logger.LogError("Could not start external detection program.");
                    return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
                }

                // Wait for process to end.
                var process = Process.GetProcessById(processId);
                process.WaitForExit();

                // Get runtime information back from memory mapped file from external process.
                using (var memoryStream = memoryMappedFile.CreateViewStream())
                {
                    runtimeInformation = (ArtifactRuntimeInformation) binaryFormatter.Deserialize(memoryStream);
                }
            }

            // Restore backed up non-serialized property.
            runtimeInformation.ReferenceImages = referenceImageBackup;

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
        /// Get the full executable path (in quotes for space characters) of the external process to start.
        /// </summary>
        /// <returns>The full path.</returns>
        private string GetExternalProcessName()
        {
            var ProcessDirectory = ApplicationSetup.GetInstance().GetExecutingDirectory().FullName;
            return "\"" + Uri.UnescapeDataString(Path.Combine(ProcessDirectory, ApplicationConfiguration.OpenWindowDetectorExe)) + "\"";
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

        /// <summary>
        /// Get security identifier for a memory mapped file which allows authenticated local users full control.
        /// </summary>
        /// <param name="fileSecurity">The security object.</param>
        private void GetSecurityIdentifier(out MemoryMappedFileSecurity fileSecurity)
        {
            fileSecurity = new MemoryMappedFileSecurity();
            fileSecurity.AddAccessRule(
                new AccessRule<MemoryMappedFileRights>(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Translate(typeof(NTAccount)),
                MemoryMappedFileRights.FullControl,
                AccessControlType.Allow));
        }
    }
}
