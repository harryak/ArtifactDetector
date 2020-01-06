using YamlDotNet.Serialization;

namespace ArbitraryArtifactDetector.RecipeParser.Model
{
    /// <summary>
    /// This is a raw state detector entry coming from the YAML recipe.
    /// </summary>
    internal class RawDetectorEntry
    {
        /// <summary>
        /// Which class to instantiate for the detector.
        /// </summary>
        [YamlMember(Alias = "detector")]
        public string DetectorClassName { get; set; }

        /// <summary>
        /// An internal ID to distinguish detectors easily.
        /// </summary>
        [YamlMember(Alias = "id")]
        public string DetectorId { get; set; }

        /// <summary>
        /// Condition-string for determining the success of the detector.
        /// </summary>
        public string Goals { get; set; } = "none";

        /// <summary>
        /// Condition-string for determining pre-conditions before this detector should be called.
        /// </summary>
        [YamlMember(Alias = "pre_conditions", ApplyNamingConventions = false)]
        public string PreConditions { get; set; } = "none";
    }
}
