namespace ArbitraryArtifactDetector.Models
{
    class DetectorResponse
    {
        public bool ArtifactFound { get; set; }
        public bool ArtifactLikely { get; set; }
        public int Certainty { get; set; }
    }
}
