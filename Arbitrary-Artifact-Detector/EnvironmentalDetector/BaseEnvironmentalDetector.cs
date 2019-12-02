/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Detector;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.EnvironmentalDetector
{
    abstract class BaseEnvironmentalDetector : BaseDetector, IEnvironmentalDetector
    {
        protected BaseEnvironmentalDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }
    }
}
