/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArtifactDetector.Model;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Drawing;

namespace ArtifactDetector.ArtifactDetector
{
    /// <summary>
    /// Base class for all artifact detectors.
    /// </summary>
    abstract class BaseArtifactDetector : IArtifactDetector
    {
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
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

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>A homography or null if none was found.</returns>
        public Mat AnalyzeImage(ObservedImage observedImage, ArtifactType artifactType, Stopwatch stopwatch = null)
        {
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            Mat mask = new Mat();
            Mat homography = new Mat();

            FindMatch(
                observedImage,
                artifactType,
                out matches,
                out mask,
                out homography,
                stopwatch
            );

#if DEBUG
            return Draw(observedImage, artifactType, matches, mask, homography);
#else
            return null;
#endif
        }

        /// <summary>
        /// Matching function using the DescriptorMatcher and voting/filtering the matches.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="mask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        public void FindMatch(ObservedImage observedImage, ArtifactType artifactType, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            //TODO: Move to config.
            int k = 2;
            double uniquenessThreshold = 0.80;
            ObservedImage artifactImage = artifactType.Images[0];

            // Initialize out variables.
            matches = new VectorOfVectorOfDMatch();
            homography = null;
            mask = new Mat();
            
            // Get the stopwatch for matching.
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            DescriptorMatcher.Add(artifactImage.Descriptors);

            DescriptorMatcher.KnnMatch(observedImage.Descriptors, matches, k, mask);
            mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
            mask.SetTo(new MCvScalar(255));
            Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

            int nonZeroCount = CvInvoke.CountNonZero(mask);
            if (nonZeroCount >= 4)
            {
                nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(
                    artifactImage.KeyPoints, observedImage.KeyPoints,
                    matches, mask, 1.5, 20);
                if (nonZeroCount >= 4)
                {
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(
                            artifactImage.KeyPoints, observedImage.KeyPoints, matches, mask, 2
                        );
                }
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Matching finished in {0} ms.", stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Helper function for debug purposes.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="mask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <returns>The result of DrawMatches.</returns>
        public Mat Draw(ObservedImage observedImage, ArtifactType artifactType, VectorOfVectorOfDMatch matches, Mat mask, Mat homography)
        {
            ObservedImage artifactImage = artifactType.Images[0];

            //Draw the matched keypoints
            Mat result = new Mat();
            Features2DToolbox.DrawMatches(
                artifactImage.Image,
                artifactImage.KeyPoints,
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
                Rectangle rect = new Rectangle(Point.Empty, artifactImage.Image.Size);
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

        /// <summary>
        /// Shorthand to load an image from a file.
        /// </summary>
        /// <param name="imagePath">Path to the image.</param>
        /// <returns>A EmguCV Mat.</returns>
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
