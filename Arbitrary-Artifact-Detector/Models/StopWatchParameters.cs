using Newtonsoft.Json;

namespace ArbitraryArtifactDetector.Models
{
    internal class StopWatchParameters
    {
        [JsonProperty("error_window_size")]
        public int ErrorWindowSize { get; set; }
    }
}