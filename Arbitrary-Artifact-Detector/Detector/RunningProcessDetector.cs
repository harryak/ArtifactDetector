using ArbitraryArtifactDetector.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Detector to detect runnign processes.
    ///
    /// Needs:  ProcessName
    /// Yields: WindowHandle
    /// </summary>
    internal class RunningProcessDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Constructor for this detector, taking the setup and its configuration.
        /// </summary>
        /// <param name="setup">Global setup object for the application.</param>
        public RunningProcessDetector(Setup setup) : base(setup)
        {
        }

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            List<Process> processes = new List<Process>();

            StartStopwatch();
            foreach (string possibleProcessName in runtimeInformation.PossibleProcessNames)
            {
                processes.AddRange(Process.GetProcessesByName(possibleProcessName));
            }

            if (processes.Count < 1)
            {
                StopStopwatch("FindArtifact finished in {0}ms.");
                return new DetectorResponse() { ArtifactPresent = false, Certainty = 100 };
            }

            foreach (Process currentProcess in processes)
            {
                if (currentProcess.MainWindowHandle != IntPtr.Zero)
                {
                    runtimeInformation.MatchingWindowsInformation.Add(currentProcess.MainWindowHandle, new WindowToplevelInformation());
                }
            }

            StopStopwatch("FindArtifact finished in {0}ms.");

            int certainty = 100 / processes.Count;
            return new DetectorResponse() { ArtifactPresent = true, Certainty = certainty };
        }
    }
}