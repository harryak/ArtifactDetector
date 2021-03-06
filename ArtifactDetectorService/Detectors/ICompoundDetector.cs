namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// A detector composed from several detectors in a prioritized list.
    /// </summary>
    internal interface ICompoundDetector : IDetector
    {
        /// <summary>
        /// Add a new detector to the chain of detectors.
        /// </summary>
        /// <param name="detector">Instance of the new detector to add.</param>
        /// <param name="priority">Optional. Prioritize this detector.</param>
        /// <returns>The set priority.</returns>
        int AddDetector(IDetector detector, int priority = -1);

        /// <summary>
        /// Set a new priority for the given detector instance.
        /// </summary>
        /// <param name="detector">Which detector to prioritize.</param>
        /// <param name="oldPriority">The old priority.</param>
        /// <param name="newPriority">The new priority.</param>
        /// <returns>True on success. false otherwise.</returns>
        bool PrioritizeDetector(IDetector detector, int oldPriority, int newPriority);

        /// <summary>
        /// Remove a detector from the chain of detectors.
        /// </summary>
        /// <param name="detector">Instance to remove.</param>
        /// <param name="oldPriority">The priority of the instance.</param>
        /// <returns>True on success, false otherwise.</returns>
        bool RemoveDetector(IDetector detector, int priority);
    }
}
