using ItsApe.ArtifactDetector.Converters;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Detectors;
using Newtonsoft.Json;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class (serializable as JSON) to hold information about an artifact.
    /// </summary>
    internal class DetectorConfiguration
    {
        /// <summary>
        /// Interval for the detector service to try to detect this artifact.
        /// </summary>
        [JsonProperty("detection_interval")]
        public int DetectionInterval { get; set; }

        /// <summary>
        /// The detector (most probably a compound detector) to use for this artifact.
        /// </summary>
        [JsonProperty("detectors")]
        [JsonConverter(typeof(DetectorConverter))]
        public IDetector Detector { get; private set; }

        /// <summary>
        /// Information on this instance of the artifact during its runtime.
        /// </summary>
        [JsonProperty("runtime_information")]
        [JsonConverter(typeof(ArtifactRuntimeInformationConverter))]
        public ArtifactRuntimeInformation RuntimeInformation { get; private set; }

        /// <summary>
        /// Global target conditions to define whether an artifact was found in a detection run.
        /// </summary>
        [JsonProperty("match_condition")]
        [JsonConverter(typeof(DetectorConditionConverter<ArtifactRuntimeInformation>))]
        public IDetectorCondition<ArtifactRuntimeInformation> MatchConditions { get; private set; }
    }
}
