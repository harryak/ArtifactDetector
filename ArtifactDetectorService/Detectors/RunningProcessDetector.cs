using System;
using System.Diagnostics;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect running processes.
    ///
    /// Needs:  ProcessName
    /// Yields: WindowHandle
    /// </summary>
    internal class RunningProcessDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            Logger.LogInformation("Detecting running processes now.");

            if (runtimeInformation.ProgramExecutables.Count < 1 && runtimeInformation.PossibleProcessSubstrings.Count < 1)
            {
                Logger.LogInformation("No matching program executables or possible process names given for detector. Could not find matching runnign processes.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeProcesses(ref runtimeInformation);

            if (runtimeInformation.CountRunningProcesses > 0)
            {
                Logger.LogInformation("Found {0} matching running processes.", runtimeInformation.CountRunningProcesses);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            Logger.LogInformation("Found no matching running processes.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        public void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountRunningProcesses = 0;
        }

        /// <summary>
        /// If the main window handle is set add it to the list.
        /// </summary>
        /// <param name="windowHandle">Main window handle to use.</param>
        private void AddMainWindowHandle(IntPtr windowHandle, ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (windowHandle != IntPtr.Zero)
            {
                runtimeInformation.WindowHandles.Add(windowHandle);
            }
        }

        /// <summary>
        /// Check whether the process matches the constraints.
        /// </summary>
        /// <param name="process"></param>
        private void AnalyzeProcess(Process process, ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (ProcessMatchesConstraints(process.ProcessName, ref runtimeInformation))
            {
                runtimeInformation.CountRunningProcesses++;
                AddMainWindowHandle(process.MainWindowHandle, ref runtimeInformation);
            }
        }

        /// <summary>
        /// Get all processes and call analyze process for each.
        /// </summary>
        private void AnalyzeProcesses(ref ArtifactRuntimeInformation runtimeInformation)
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                AnalyzeProcess(process, ref runtimeInformation);
            }
        }

        /// <summary>
        /// Check whether the process name matches.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns>True if the process name matches.</returns>
        private bool ProcessMatchesConstraints(string processName, ref ArtifactRuntimeInformation runtimeInformation)
        {
            return runtimeInformation.ProgramExecutables.Contains(processName)
                || processName.ContainsAny(runtimeInformation.PossibleProcessSubstrings);
        }
    }
}
