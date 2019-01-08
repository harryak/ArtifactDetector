/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArtifactDetector.ArtifactDetector
{
    class OrbArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public OrbArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("OrbArtifactDetector");

            FeatureDetector = new ORBDetector();
        }
    }
}
