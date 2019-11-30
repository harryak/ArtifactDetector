/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.VisualDetector
{
    class OrbArtifactDetector : BaseVisualArtifactDetector, IVisualArtifactDetector
    {
        public OrbArtifactDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {
            //IIndexParams indexParams = new LshIndexParams(6, 12, 1);
            //SearchParams searchParams = new SearchParams(50);
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);

            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
