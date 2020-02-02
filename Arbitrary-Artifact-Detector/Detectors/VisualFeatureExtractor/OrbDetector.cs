using Emgu.CV.Features2D;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    internal class OrbDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public OrbDetector()
        {
            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}