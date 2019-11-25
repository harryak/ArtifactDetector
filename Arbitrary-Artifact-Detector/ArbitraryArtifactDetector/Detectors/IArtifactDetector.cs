/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Model;
using Emgu.CV;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.ArbitraryArtifactDetector.MatchFilters;

namespace ArbitraryArtifactDetector.ArbitraryArtifactDetector.Detectors
{
    interface IArtifactDetector
    {
        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
        ProcessedImage ExtractFeatures(string imagePath, VADStopwatch stopwatch = null);

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="matchFilter">The filter used for matching.</param>
        /// <param name="drawingResult">The resulting image to draw in a window.</param>
        /// <param name="matchCount">Count of matches, if one was found.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>Whether a match was found.</returns>
        bool ImageContainsArtifactType(ProcessedImage observedImage, ArtifactType artifactType, IMatchFilter matchFilter, out Mat drawingResult, out int matchCount, VADStopwatch stopwatch = null);
    }
}
