namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data wrapper to hold information about a detection result.
    /// </summary>
    internal class DetectorResponse
    {
        /// <summary>
        /// Limited set of possibilites for the presence of the artifact.
        /// </summary>
        public enum ArtifactPresence
        {
            Impossible,
            Possible,
            Certain
        }

        /// <summary>
        /// Getter and setter for the artifact's presence indicator.
        /// </summary>
        public ArtifactPresence ArtifactPresent { get; set; }
    }
}
