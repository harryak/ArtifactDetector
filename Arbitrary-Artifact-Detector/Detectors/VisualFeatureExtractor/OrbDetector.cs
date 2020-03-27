using Emgu.CV.Features2D;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Visual feature detector using Orb and a brute-force feature matcher internally.
    /// </summary>
    internal class OrbDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        /// <summary>
        /// Visual feature detector using Orb and a brute-force feature matcher internally.
        /// </summary>
        public OrbDetector()
        {
            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
