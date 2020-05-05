using System.Collections.Generic;
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
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            Logger.LogInformation("Compound detector started.");

            DetectorResponse previousResponse = null;
            DetectorResponse response = null;

            foreach (var entry in DetectorList)
            {
                // Check if previous certainty meets required level.
                if (previousResponse != null && entry.Value.HasPreConditions() && !entry.Value.PreConditionsMatch(ref runtimeInformation))
                {
                    return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
                }

                // Get the new chain element's response.
                response = entry.Value.FindArtifact(ref runtimeInformation, sessionId);

                // If there is an artifact or there is none with 100% certainty, break.
                if (entry.Value.HasTargetConditions() && !entry.Value.TargetConditionsMatch(ref response))
                {
                    return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
                }

                // Copy to right variable for next run.
                previousResponse = response;
            }

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
