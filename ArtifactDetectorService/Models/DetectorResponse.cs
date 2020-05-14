namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data wrapper to hold information about a detection result.
    /// </summary>
    internal class DetectorResponse
    {
        /// <summary>
        /// Shorthand for a response with the presence set to "certain".
        /// </summary>
        public static DetectorResponse PresenceCertain    = new DetectorResponse { ArtifactPresent = ArtifactPresence.Certain };

        /// <summary>
        /// Shorthand for a response with the presence set to "impossible".
        /// </summary>
        public static DetectorResponse PresenceImpossible = new DetectorResponse { ArtifactPresent = ArtifactPresence.Impossible };

        /// <summary>
        /// Shorthand for a response with the presence set to "possible".
        /// </summary>
        public static DetectorResponse PresencePossible   = new DetectorResponse { ArtifactPresent = ArtifactPresence.Possible };

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

        /// <summary>
        /// Timestamps for evaluation of runtime.
        /// 
        /// TODO: Remove this after evaluation.
        /// </summary>
        public long[] timestamps;
    }
}
