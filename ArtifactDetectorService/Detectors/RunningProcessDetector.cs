using System.Diagnostics;
using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            Logger.LogInformation("Detecting running processes now.");

            if (runtimeInformation.ProgramExecutables.Count < 1 && runtimeInformation.PossibleProcessSubstrings.Count < 1)
            {
                Logger.LogInformation("No matching program executables or possible process names given for detector. Could not find matching running processes.");
                return DetectorResponse.PresencePossible;
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeProcesses(ref runtimeInformation);

            if (runtimeInformation.CountRunningProcesses > 0)
            {
                if (matchConditions != null)
                {
                    if (matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                    {
                        Logger.LogInformation("Found {0} matching running processes and conditions for a match are met.", runtimeInformation.CountRunningProcesses);
                        return DetectorResponse.PresenceCertain;
                    }
                    else
                    {
                        Logger.LogInformation("Found {0} matching running processes, but conditions for a match are not met.", runtimeInformation.CountRunningProcesses);
                        return DetectorResponse.PresenceImpossible;
                    }
                }

                Logger.LogInformation("Found {0} matching running processes.", runtimeInformation.CountRunningProcesses);
                return DetectorResponse.PresenceCertain;
            }

            Logger.LogInformation("Found no matching running processes.");
            return DetectorResponse.PresenceImpossible;
        }

        public void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountRunningProcesses = 0;
        }

        /// <summary>
        /// If the main window handle is set add it to the list.
        /// </summary>
        /// <param name="processHandle">Process handle to add.</param>
        private void AddProcessId(uint processId, ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (processId > 0)
            {
                runtimeInformation.ProcessIds.Add(processId);
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
                AddProcessId((uint)process.Id, ref runtimeInformation);
            }
        }

        /// <summary>
        /// Get all processes and call analyze process for each.
        /// </summary>
        private void AnalyzeProcesses(ref ArtifactRuntimeInformation runtimeInformation)
        {
            var processes = Process.GetProcesses();
            for (var i = 0; i < processes.Length; i++)
            {
                AnalyzeProcess(processes[i], ref runtimeInformation);
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
