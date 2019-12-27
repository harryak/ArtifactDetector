using ArbitraryArtifactDetector.Helper;
using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.Detector.VisualDetector
{
    class AkazeDetector : BaseVisualDetector, IVisualDetector
    {
        public AkazeDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {

            FeatureDetector = new AKAZE();

            //LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            //SearchParams searchParams = new SearchParams();
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
