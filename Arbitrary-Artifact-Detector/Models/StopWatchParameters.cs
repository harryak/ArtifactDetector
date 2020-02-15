using Newtonsoft.Json;

namespace ItsApe.ArtifactDetector.Models
{
    internal class StopWatchParameters
    {
        [JsonProperty("error_window_size")]
        public int ErrorWindowSize { get; set; }
    }
}