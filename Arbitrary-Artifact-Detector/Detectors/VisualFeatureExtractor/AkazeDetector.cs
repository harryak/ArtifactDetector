using Emgu.CV.Features2D;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    class AkazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public AkazeDetector()
        {
            FeatureDetector = new AKAZE();
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
