using Emgu.CV.Features2D;

namespace ArbitraryArtifactDetector.Detector.VisualFeatureExtractor
{
    internal class OrbDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public OrbDetector(Setup setup) : base(setup)
        {
            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}