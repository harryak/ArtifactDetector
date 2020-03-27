using System;
using System.Collections.Generic;
using System.Diagnostics;
using ItsApe.ArtifactDetector.Models;

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
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            var processes = new List<Process>();

            StartStopwatch();
            foreach (string possibleProcessName in runtimeInformation.PossibleProcessNames)
            {
                processes.AddRange(Process.GetProcessesByName(possibleProcessName));
            }

            if (processes.Count < 1)
            {
                StopStopwatch("FindArtifact finished in {0}ms.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }

            foreach (var currentProcess in processes)
            {
                if (currentProcess.MainWindowHandle != IntPtr.Zero)
                {
                    runtimeInformation.MatchingWindowsInformation.Add(currentProcess.MainWindowHandle, new WindowToplevelInformation());
                }
            }

            StopStopwatch("FindArtifact finished in {0}ms.");

            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
        }
    }
}
