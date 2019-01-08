/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArtifactDetector.Model;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArtifactDetector.ArtifactDetector
{
    class FastArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public FastArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("FastArtifactDetector");

            // Set distinct featureDetector
            FeatureDetector = new FastDetector();
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming, true);
        }

        public new ObservedImage ExtractFeatures(string imagePath, Stopwatch stopwatch = null)
        {
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            Mat image = LoadImage(imagePath);

            VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
            Mat descriptors = new Mat();
            FeatureDetector.DetectRaw(image, keyPoints);
            FeatureDetector.Compute(image, keyPoints, descriptors);

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Extracted features from image in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return new ObservedImage(image, keyPoints, descriptors);
        }

        public new void FindMatch(ObservedImage observedImage, ArtifactType artifactType, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            //TODO: Move to config.
            int k = 2;
            double lowes_ratio = 0.75f;

            // Initialize out variables.
            matches = new VectorOfVectorOfDMatch();
            homography = null;
            mask = null;

            // Use temp structure for matches before filtering.
            VectorOfVectorOfDMatch pre_matches = null;

            // Add the artifact's descriptor to the matcher.
            DescriptorMatcher.Add(artifactType.Descriptors);

            // Get the stopwatch for matching.
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            DescriptorMatcher.KnnMatch(observedImage.Descriptors, pre_matches, k, mask);

            // Apply Lowe's ratio test.
            for (int i = 0; i < pre_matches.Size; i++)
            {
                if (pre_matches[i][0].Distance < lowes_ratio * pre_matches[i][1].Distance)
                {
                    matches.Push(pre_matches[i]);
                }
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Matching finished in {0} ms.", stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
