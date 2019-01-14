/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArtifactDetector.ArtifactDetector
{
    class AkazeArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public AkazeArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("AkazeArtifactDetector");

            FeatureDetector = new AKAZE();

            //LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            //SearchParams searchParams = new SearchParams();
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
