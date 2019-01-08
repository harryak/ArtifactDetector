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
using System;
using System.Diagnostics;
using System.Drawing;

namespace ArtifactDetector.ArtifactDetector
{
    /**
     * Base class for all detectors. Contains setup, if needed.
     */
    class BaseArtifactDetector : IArtifactDetector
    {
        protected ILogger Logger { get; set; }

        /**
         * Analyze a screenshot with respect to the given parameter type.
         * Also draws results (Debug only).
         */
        public Mat AnalyzeImage(ObservedImage observedImage, ArtifactType artifactType)
        {
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            Mat mask = new Mat();
            Mat homography = new Mat();

            FindMatch(
                artifactType,
                observedImage,
                out matches,
                out mask,
                out homography,
                new Stopwatch()
            );

#if DEBUG
            return Draw(observedImage, artifactType, matches, mask, homography);
#else
            return null;
#endif
        }

        /**
         * Extracts features of a model image, given by its path.
         */
        public ObservedImage ExtractFeatures(string imagePath, Stopwatch stopwatch = null)
        {
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            Mat image = LoadImage(imagePath);

            VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
            Mat descriptors = new Mat();
            FeatureDetector.DetectAndCompute(image.GetUMat(AccessType.Read), null, keyPoints, descriptors, false);

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Extracted features from image in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return new ObservedImage(image, keyPoints, descriptors);
        }

        /**
         * Try to find a match between a model and an observed image.
         */
        public void FindMatch(ArtifactType artifactType, ObservedImage observedImage, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            //TODO: Move to config.
            int k = 2;
            double uniquenessThreshold = 0.80;

            // Initialize out variables.
            matches = new VectorOfVectorOfDMatch();
            homography = null;
            mask = new Mat();
            
            // Get the stopwatch for matching.
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            DescriptorMatcher.Add(artifactType.Descriptors);

            DescriptorMatcher.KnnMatch(observedImage.Descriptors, matches, k, mask);
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

        /**
         * Draws the found matches.
         */
        public Mat Draw(ObservedImage observedImage, ArtifactType artifactType, VectorOfVectorOfDMatch matches, Mat mask, Mat homography)
        {
            //Draw the matched keypoints
            Mat result = new Mat();
            Features2DToolbox.DrawMatches(
                artifactType.Image,
                artifactType.KeyPoints,
                observedImage.Image,
                observedImage.KeyPoints,
                matches,
                result,
                new MCvScalar(255, 255, 255),
                new MCvScalar(255, 255, 255),
                mask
            );

            if (homography != null)
            {
                //draw a rectangle along the projected model
                Rectangle rect = new Rectangle(Point.Empty, artifactType.Image.Size);
                PointF[] pts = new PointF[]
                {
                    new PointF(rect.Left, rect.Bottom),
                    new PointF(rect.Right, rect.Bottom),
                    new PointF(rect.Right, rect.Top),
                    new PointF(rect.Left, rect.Top)
                };
                pts = CvInvoke.PerspectiveTransform(pts, homography);

                Point[] points = Array.ConvertAll(pts, Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(points))
                {
                    CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                }
            }

            return result;
        }

        /**
         *  Prepares an image to yield a Mat.
         */
        public Mat LoadImage(string imagePath)
        {
            Mat image = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);

            if (image.IsEmpty)
            {
                Logger.LogError("Image couldn't be loaded ({imagePath}).", imagePath);
                throw new ArgumentException("The image was loaded as empty.");
            }

            return image;
        }

        protected Feature2D FeatureDetector { get; set; }
        protected DescriptorMatcher DescriptorMatcher { get; set; }
    }
}
