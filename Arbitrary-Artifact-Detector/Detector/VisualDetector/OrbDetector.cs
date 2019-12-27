using ArbitraryArtifactDetector.Helper;
using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.Detector.VisualDetector
{
    class OrbDetector : BaseVisualDetector, IVisualDetector
    {
        public OrbDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {
            //IIndexParams indexParams = new LshIndexParams(6, 12, 1);
            //SearchParams searchParams = new SearchParams(50);
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);

            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
