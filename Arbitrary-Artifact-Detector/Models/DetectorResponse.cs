namespace ItsApe.ArtifactDetector.Models
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
