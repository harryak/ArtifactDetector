/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.ArbitraryArtifactDetector.Detectors
{
    class BriskArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public BriskArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("BriskArtifactDetector");

            FeatureDetector = new Brisk();

            LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            SearchParams searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            //DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
