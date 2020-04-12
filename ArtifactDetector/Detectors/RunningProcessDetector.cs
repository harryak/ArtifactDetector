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
        private int foundMatches = 0;
        private IList<string> PossibleProcessSubstrings { get; set; }
        private IList<string> ProcessNames { get; set; }
        private IList<IntPtr> WindowHandles { get; } = new List<IntPtr>();

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            Logger.LogInformation("Detecting running processes now.");

            // Stopwatch for evaluation.
            StartStopwatch();

            if (runtimeInformation.ProgramExecutables.Count < 1 && runtimeInformation.PossibleProcessSubstrings.Count < 1)
            {
                StopStopwatch("Got all running processes in {0}ms.");
                Logger.LogInformation("No matching program executables or possible process names given for detector. Could not find matching runnign processes.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            ProcessNames = runtimeInformation.ProgramExecutables;
            PossibleProcessSubstrings = runtimeInformation.PossibleProcessSubstrings;

            AnalyzeProcesses();

            if (foundMatches > 0)
            {
                runtimeInformation.WindowHandles = WindowHandles;

                StopStopwatch("Got all running processes in {0}ms.");
                Logger.LogInformation("Found {0} matching running processes.", foundMatches);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            StopStopwatch("Got all running processes in {0}ms.");
            Logger.LogInformation("Found no matching running processes.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
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
