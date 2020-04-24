using Newtonsoft.Json;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Parameters for the "stop-watch" call.
    /// </summary>
    internal class StopWatchParameters
    {
        /// <summary>
        /// Define how many detection results should be used for the error correction sliding window.
        /// </summary>
        [JsonProperty("error_window_size")]
        public int ErrorWindowSize { get; set; }
    }
}
