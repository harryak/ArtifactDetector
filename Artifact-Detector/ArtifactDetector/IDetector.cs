/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArtifactDetector.Model;
using Emgu.CV;
using Emgu.CV.Util;
using System.Diagnostics;

namespace ArtifactDetector.ArtifactDetector
{
    interface IArtifactDetector
    {
        ObservedImage ExtractFeatures(string imagePath, Stopwatch stopwatch = null);

        Mat AnalyzeImage(ObservedImage observedImage, ArtifactType artifactType);

        void FindMatch(ArtifactType artifactType, ObservedImage observedImage, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null);
    }
}
