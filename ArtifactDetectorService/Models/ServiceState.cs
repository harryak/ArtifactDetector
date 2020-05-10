namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class to hold information used for and in the detector service.
    /// </summary>
    internal class ServiceState
    {
        /// <summary>
        /// The current configuration of artifact to be looked for.
        /// </summary>
        public DetectorConfiguration DetectorConfiguration { get; set; } = null;

        /// <summary>
        /// Flag to tell if the watch task is currently running.
        /// </summary>
        public bool IsRunning { get; set; } = false;

        /// <summary>
        /// Path to collect all detector responses.
        /// </summary>
        public string DetectionLogDirectory { get; set; }
    }
}
