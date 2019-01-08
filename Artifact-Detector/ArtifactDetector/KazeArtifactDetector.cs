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
    class KazeArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public KazeArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("KazeArtifactDetector");

            FeatureDetector = new KAZE();
        }

        public override void FindMatch(ArtifactType artifactType, ObservedImage observedImage, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            //TODO: Move to config.
            int k = 2;
            double uniquenessThreshold = 0.80;

            // Initialize out variables.
            matches = new VectorOfVectorOfDMatch();
            homography = null;
            mask = null;

            using (LinearIndexParams ip = new LinearIndexParams())
            using (SearchParams sp = new SearchParams())
            using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
            {
                // Get the stopwatch for matching.
                if (stopwatch != null)
                {
                    stopwatch.Restart();
                }

                matcher.Add(artifactType.Descriptors);

                matcher.KnnMatch(observedImage.Descriptors, matches, k, null);
                mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                mask.SetTo(new MCvScalar(255));
                Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                int nonZeroCount = CvInvoke.CountNonZero(mask);
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(
                        artifactType.KeyPoints, observedImage.KeyPoints,
                        matches, mask, 1.5, 20);
                    if (nonZeroCount >= 4)
                    {
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(
                                artifactType.KeyPoints, observedImage.KeyPoints, matches, mask, 2
                            );
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
}
