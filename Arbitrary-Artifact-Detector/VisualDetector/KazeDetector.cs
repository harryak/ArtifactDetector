using ArbitraryArtifactDetector.Helper;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.VisualDetector
{
    class KazeDetector : BaseVisualDetector, IVisualDetector
    {
        public KazeDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {
            FeatureDetector = new KAZE();

            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}
