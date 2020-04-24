namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data wrapper to hold information about a detection result.
    /// </summary>
    internal class DetectorResponse
    {
        public enum ArtifactPresence
        {
            Impossible,
            Possible,
            Certain
        }

        /// <summary>
        /// A ternary value.
        /// </summary>
        public ArtifactPresence ArtifactPresent { get; set; }
    }
}
