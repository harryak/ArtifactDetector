using Emgu.CV.Features2D;
using Emgu.CV.Flann;

namespace ArbitraryArtifactDetector.Detector.VisualFeatureExtractor
{
    class BriskDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public BriskDetector(Setup setup) : base(setup)
        {
            FeatureDetector = new Brisk();

            LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            SearchParams searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
        }
    }
}
