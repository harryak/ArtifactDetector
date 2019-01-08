/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArtifactDetector.Model;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArtifactDetector.ArtifactDetector
{
    class OrbArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public OrbArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("OrbArtifactDetector");

            FeatureDetector = new ORBDetector();

            //LshIndexParams indexParams = new LshIndexParams(6, 12, 1);
            //SearchParams searchParams = new SearchParams();
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
