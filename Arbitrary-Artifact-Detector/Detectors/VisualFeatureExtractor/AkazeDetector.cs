using Emgu.CV.Features2D;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    class AkazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public AkazeDetector(Setup setup) : base(setup)
        {
            FeatureDetector = new AKAZE();
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
