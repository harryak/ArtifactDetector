using System.Collections.Generic;
using System.Diagnostics;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// A detector made up from several detectors.
    /// </summary>
    internal class CompoundDetector : BaseDetector, ICompoundDetector
    {
        /// <summary>
        /// List of detector configurations.
        /// </summary>
        private SortedDictionary<int, IDetector> DetectorList { get; set; } = new SortedDictionary<int, IDetector>();

        /// <summary>
        /// Add a new detector to the chain of detectors.
        /// </summary>
        /// <param name="detector">Instance of the new detector to add.</param>
        /// <param name="priority">Optional. Prioritize this detector.</param>
        /// <returns>The set priority.</returns>
        public int AddDetector(IDetector detector, int priority = -1)
        {
            // If no priority is given: Take current chain count times ten.
            if (priority < 0)
            {
                priority = DetectorList.Count * 10;
            }

            // Add detector to the chain with the (calculated) priority.
            DetectorList.Add(priority, detector);

            return priority;
        }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            Logger.LogInformation("Compound detector started.");

            DetectorResponse previousResponse = null;
            DetectorResponse response = null;
            //TODO: Remove this line after evaluation.
            long[] timestamps = new long[DetectorList.Count * 2 + 2]; int i = 0;

            //TODO: Remove this line after evaluation.
            long startingTimestamp = Stopwatch.GetTimestamp();
            foreach (var entry in DetectorList)
            {
                // Check if previously found information meets the required level.
                if (previousResponse != null && entry.Value.HasPreConditions() && !entry.Value.PreConditionsMatch(ref runtimeInformation))
                {
                    //TODO: Change these lines after evaluation.
                    timestamps[timestamps.Length - 2] = startingTimestamp;
                    timestamps[timestamps.Length - 1] = Stopwatch.GetTimestamp();
                    return new DetectorResponse
                    {
                        ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible,
                        timestamps = timestamps
                    };
                }

                // Find the artifact with the next detector in the chain.
                //TODO: Remove this line after evaluation.
                timestamps[i] = Stopwatch.GetTimestamp(); i++;
                response = entry.Value.FindArtifact(ref runtimeInformation, null, sessionId);
                //TODO: Remove this line after evaluation.
                timestamps[i] = Stopwatch.GetTimestamp(); i++;

                // Check whether the response meets the target conditions.
                if (entry.Value.HasTargetConditions() && !entry.Value.TargetConditionsMatch(ref response))
                {
                    //TODO: Change these lines after evaluation.
                    timestamps[timestamps.Length - 2] = startingTimestamp;
                    timestamps[timestamps.Length - 1] = Stopwatch.GetTimestamp();
                    return new DetectorResponse
                    {
                        ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible,
                        timestamps = timestamps
                    };
                }

                // Test the global conditions for a match, return success if they are met.
                if (matchConditions != null && matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                {
                    Logger.LogInformation("Match conditions match.");
                    timestamps[timestamps.Length - 2] = startingTimestamp;
                    timestamps[timestamps.Length - 1] = Stopwatch.GetTimestamp();
                    return new DetectorResponse
                    {
                        ArtifactPresent = DetectorResponse.ArtifactPresence.Certain,
                        timestamps = timestamps
                    };
                }

                // Copy to right variable for next run.
                previousResponse = response;
            }
            //TODO: Remove these lines after evaluation.
            timestamps[timestamps.Length - 2] = startingTimestamp;
            timestamps[timestamps.Length - 1] = Stopwatch.GetTimestamp();

            // If match conditions are set, they have already been checked and failed.
            if (matchConditions != null)
            {
                //TODO: Change these lines after evaluation.
                return new DetectorResponse
                {
                    ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible,
                    timestamps = timestamps
                };
            }

            // Fallback without match condition: Return last response.
            //TODO: Remove this line after evaluation.
            response.timestamps = timestamps;
            return response;
        }

        /// <summary>
        /// Return the detectors of the list.
        /// </summary>
        /// <returns>All detectors.</returns>
        public SortedDictionary<int, IDetector> GetDetectors()
        {
            return DetectorList;
        }

        /// <summary>
        /// Set a new priority for the given detector instance.
        /// </summary>
        /// <param name="detector">Which detector to prioritize.</param>
        /// <param name="oldPriority">The old priority.</param>
        /// <param name="newPriority">The new priority.</param>
        /// <returns>True on success. false otherwise.</returns>
        public bool PrioritizeDetector(IDetector detector, int oldPriority, int newPriority)
        {
            if (DetectorList.ContainsKey(oldPriority))
            {
                RemoveDetector(detector, oldPriority);
            }

            return AddDetector(detector, newPriority) == newPriority;
        }

        /// <summary>
        /// Remove a detector from the chain of detectors.
        /// </summary>
        /// <param name="detector">Instance to remove.</param>
        /// <param name="oldPriority">The priority of the instance.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool RemoveDetector(IDetector detector, int priority)
        {
            return DetectorList.Remove(priority);
        }
    }
}
