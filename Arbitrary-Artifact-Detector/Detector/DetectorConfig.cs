using ArbitraryArtifactDetector.Models;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Holds the configuration for a detector and its instance.
    /// </summary>
    class DetectorConfig
    {
        /// <summary>
        /// The detector's instance.
        /// </summary>
        public IDetector Detector { get; }

        /// <summary>
        /// Certainty of previously called detectors to call this one.
        /// </summary>
        public int RequiredCertainty { get; }

        /// <summary>
        /// Simple constructor.
        /// </summary>
        /// <param name="detector">The detector's instance.</param>
        /// <param name="requiredCertainty">Certainty of previously called detectors to call this one.</param>
        public DetectorConfig(IDetector detector, int requiredCertainty = 0)
        {
            Detector = detector;
            RequiredCertainty = requiredCertainty;
        }

        /// <summary>
        /// Check whether the detector has requirements before getting called.
        /// </summary>
        /// <returns>True if there are any requirements.</returns>
        public bool HasRequirements()
        {
            // Go through all possible requirements.
            return RequiredCertainty > 0;
        }

        /// <summary>
        /// Check whether this detector has to be called.
        /// </summary>
        /// <param name="response">The previous detector's response.</param>
        /// <returns>True if this detector's prerequisites allow the call of it.</returns>
        public bool IsToCall(DetectorResponse response = null)
        {
            if (response == null)
            {
                // If there is no response from before return whether this _not_ has any requirements.
                return !HasRequirements();
            }

            // Go through all possible requirements.
            return response.Certainty >= RequiredCertainty;
        }
    }
}
