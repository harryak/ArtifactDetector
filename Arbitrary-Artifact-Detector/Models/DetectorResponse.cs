namespace ArbitraryArtifactDetector.Models
{
    class DetectorResponse
    {
        public bool ArtifactPresent { get; set; }
        public int Certainty { get; set; }
    }
}
