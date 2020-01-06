using Emgu.CV.Features2D;
using Emgu.CV.Flann;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    class KazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public KazeDetector(Setup setup) : base(setup)
        {
            FeatureDetector = new KAZE();

            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}
