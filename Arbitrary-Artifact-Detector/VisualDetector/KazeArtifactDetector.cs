﻿/**
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
    class KazeArtifactDetector : BaseVisualArtifactDetector, IVisualArtifactDetector
    {
        public KazeArtifactDetector(ILogger logger, VADStopwatch stopwatch) : base(logger, stopwatch)
        {
            FeatureDetector = new KAZE();

            LinearIndexParams ip = new LinearIndexParams();
            SearchParams sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}