namespace ArbitraryArtifactDetector.Models
{
    class DetectorResponse
    {
        public ArtifactPresence ArtifactPresent { get; set; }

        public enum ArtifactPresence
        {
            Certain,
            Possible,
            Impossible
        }
    }
}
