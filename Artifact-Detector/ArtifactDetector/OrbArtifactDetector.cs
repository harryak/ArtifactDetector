/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ArtifactDetector.ArtifactDetector
{
    class OrbArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public OrbArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("OrbArtifactDetector");

            /*IIndexParams indexParams = new LshIndexParams(6, 12, 1);
            SearchParams searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);*/

            FeatureDetector = new ORBDetector(500, 1.2f, 4, 50, 0, 2, ORBDetector.ScoreType.Harris, 50);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
