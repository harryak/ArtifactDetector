namespace ArbitraryArtifactDetector.Models
{
    internal class ServiceState
    {
        /// <summary>
        /// The current configuration of artifact to be looked for.
        /// </summary>
        public ArtifactConfiguration ArtifactConfiguration { get; set; } = null;

        /// <summary>
        /// Path to store the compiled detector responses.
        /// </summary>
        public string CompiledResponsesPath { get; set; }

        /// <summary>
        /// Flag to tell if the watch task is currently running.
        /// </summary>
        public bool IsRunning { get; set; } = false;

        /// <summary>
        /// Path to collect all detector responses.
        /// </summary>
        public string ResponsesPath { get; set; }
    }
}