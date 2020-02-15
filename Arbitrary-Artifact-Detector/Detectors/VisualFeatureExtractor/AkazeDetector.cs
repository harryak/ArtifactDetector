using Emgu.CV.Features2D;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
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
