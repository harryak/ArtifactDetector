using Emgu.CV.Features2D;
using Emgu.CV.Flann;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    class BriskDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public BriskDetector()
        {
            FeatureDetector = new Brisk();

            LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            SearchParams searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
        }
    }
}
