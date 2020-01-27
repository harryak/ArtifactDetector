using ArbitraryArtifactDetector.Converter;
using ArbitraryArtifactDetector.Detector;
using Newtonsoft.Json;

namespace ArbitraryArtifactDetector.Model
{
    internal class ArtifactConfiguration
    {
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
        /// Interval for the detector service to try to detect this artifact.
        /// </summary>
        [JsonProperty("detection_interval")]
        public int DetectionInterval { get; set; }
    }
}