using System;
using System.Collections.Generic;
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
        private int foundMatches;
        private IList<string> PossibleProcessSubstrings { get; set; }
        private IList<string> ProcessNames { get; set; }
        private IList<IntPtr> WindowHandles { get; } = new List<IntPtr>();

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (!IsScreenActive(ref runtimeInformation))
            {
                Logger.LogInformation("Not detecting, screen is locked.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }
            Logger.LogInformation("Detecting running processes now.");

            if (runtimeInformation.ProgramExecutables.Count < 1 && runtimeInformation.PossibleProcessSubstrings.Count < 1)
            {
                Logger.LogInformation("No matching program executables or possible process names given for detector. Could not find matching runnign processes.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeProcesses();

            if (foundMatches > 0)
            {
                runtimeInformation.WindowHandles = WindowHandles;
                runtimeInformation.CountRunningProcesses = foundMatches;
                
                Logger.LogInformation("Found {0} matching running processes.", foundMatches);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }
            
            Logger.LogInformation("Found no matching running processes.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            foundMatches = 0;
            ProcessNames = runtimeInformation.ProgramExecutables;
            PossibleProcessSubstrings = runtimeInformation.PossibleProcessSubstrings;
        }

        /// <summary>
        /// If the main window handle is set add it to the list.
        /// </summary>
        /// <param name="windowHandle">Main window handle to use.</param>
        private void AddMainWindowHandle(IntPtr windowHandle)
        {
            if (windowHandle != IntPtr.Zero)
            {
                WindowHandles.Add(windowHandle);
            }
        }

        /// <summary>
        /// Check whether the process matches the constraints.
        /// </summary>
        /// <param name="process"></param>
        private void AnalyzeProcess(Process process)
        {
            if (ProcessMatchesConstraints(process.ProcessName))
            {
                foundMatches++;
                AddMainWindowHandle(process.MainWindowHandle);
            }
        }

        /// <summary>
        /// Get all processes and call analyze process for each.
        /// </summary>
        private void AnalyzeProcesses()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                AnalyzeProcess(process);
            }
        }

        /// <summary>
        /// Check whether the process name matches.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns>True if the process name matches.</returns>
        private bool ProcessMatchesConstraints(string processName)
        {
            return ProcessNames.Contains(processName)
                || processName.ContainsAny(PossibleProcessSubstrings);
        }
    }
}
