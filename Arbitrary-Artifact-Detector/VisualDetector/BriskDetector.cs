/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.VisualDetector
{
    class BriskDetector : BaseVisualDetector, IVisualDetector
    {
        public BriskDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {
            FeatureDetector = new Brisk();

            LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            SearchParams searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            //DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
