namespace ArbitraryArtifactDetector.RecipeParser.Model
{
    /// <summary>
    /// This is a raw state detector entry coming from the YAML recipe.
    /// </summary>
    internal class RawDetectorEntry
    {
        /// <summary>
        /// An internal ID to distinguish detectors easily.
        /// </summary>
        public string DetectorId { get; set; }

        /// <summary>
        /// Which class to instantiate for the detector.
        /// </summary>
        public string DetectorClassName { get; set; }

        /// <summary>
        /// Condition-string for determining the success of the detector.
        /// </summary>
        public string Goals { get; set; }

        /// <summary>
        /// Condition-string for determining pre-conditions before this detector should be called.
        /// </summary>
        public string PreConditions { get; set; }
    }
}
