using Emgu.CV.Features2D;
using Emgu.CV.Flann;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    internal class KazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        public KazeDetector()
        {
            FeatureDetector = new KAZE();

            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}