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
        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
        ObservedImage ExtractFeatures(string imagePath, Stopwatch stopwatch = null);

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>A homography.</returns>
        Mat AnalyzeImage(ObservedImage observedImage, ArtifactType artifactType, Stopwatch stopwatch = null);
    }
}
