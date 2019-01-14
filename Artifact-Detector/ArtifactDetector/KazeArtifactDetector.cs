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
    class KazeArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public KazeArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("KazeArtifactDetector");

            FeatureDetector = new KAZE();

            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}
