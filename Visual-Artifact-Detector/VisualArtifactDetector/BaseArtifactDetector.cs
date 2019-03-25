/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using VisualArtifactDetector.Model;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using VisualArtifactDetector.Helper;

namespace VisualArtifactDetector.VisualArtifactDetector
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
        public ProcessedImage ExtractFeatures(string imagePath, VADStopwatch stopwatch = null)
        {
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            Mat image;
            try
            {
                image = LoadImage(imagePath);
            }
            catch (Exception e)
            {
                Logger.LogError("Could not load the image at {0}: {1}", imagePath, e.Message);
                return null;
            }

            VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
            Mat descriptors = new Mat();

            FeatureDetector.DetectAndCompute(image.GetUMat(AccessType.Read), null, keyPoints, descriptors, false);

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Extracted features from image in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return new ProcessedImage(image, keyPoints, descriptors);
        }

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>A homography or null if none was found.</returns>
        public bool ImageContainsArtifactType(ProcessedImage observedImage, ArtifactType artifactType, out Mat drawingResult, VADStopwatch stopwatch = null)
        {
            // Only needed for debugging purposes, otherwise will always be null.
            drawingResult = null;

            // Same.
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            Mat matchesMask = new Mat();
            Mat homography = new Mat();

            // Get a matched artifact image or null.
            ProcessedImage matchedArtifact = FindMatch(
                observedImage,
                artifactType,
                out matches,
                out matchesMask,
                out homography,
                stopwatch
            );

#if DEBUG
            if (homography != null)
                drawingResult = Draw(observedImage, matchedArtifact, matches, matchesMask, homography);
#endif
            return homography != null;
        }

        /// <summary>
        /// Matching function using the DescriptorMatcher and voting/filtering the matches.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="matchesMask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <param name="stopwatch">An optional stopwatch used for evaluation.</param>
        /// <returns>A matched artifact image, if available.</returns>
        public ProcessedImage FindMatch(ProcessedImage observedImage, ArtifactType artifactType, out VectorOfVectorOfDMatch goodMatches, out Mat matchesMask, out Mat homography, VADStopwatch stopwatch = null)
        {
            int minMatches = VADConfig.MinimumMatchesRequired;
            double uniquenessThreshold = VADConfig.MatchUniquenessThreshold;

            ProcessedImage matchingArtifact = null;

            // Initialize out variables.
            goodMatches = new VectorOfVectorOfDMatch();

            VectorOfVectorOfDMatch matches;
            matchesMask = new Mat();
            // Only needed for debugging output, otherwise will always be null.
            homography = null;
            
            // Get the stopwatch for matching.
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            foreach (var currentArtifactImage in artifactType.Images)
            {
                // Add model descriptors to matcher.
                DescriptorMatcher.Add(currentArtifactImage.Descriptors);
                // Match this set with the observed image descriptors.
                matches = new VectorOfVectorOfDMatch();
                DescriptorMatcher.KnnMatch(observedImage.Descriptors, matches, 2, null);

                // Apply Lowe's ratio test for 0.7 to the matches.
                // Also, look if the distance is < 80 ().
                MDMatch[][] matchesArray = matches.ToArrayOfArray();
                foreach (MDMatch[] match in matchesArray)
                {
                    // FLANN does not always output two matching points.
                    if (match.Length > 1)
                    {
                        if (match[0].Distance < 0.7 * match[1].Distance && match[1].Distance < 80)
                        {
                            goodMatches.Push(new VectorOfDMatch(match));
                        }
                    }
                }
                matches.Dispose();

                // Check the matches, only take unique ones.
                matchesMask = new Mat(goodMatches.Size, 1, DepthType.Cv8U, 1);
                matchesMask.SetTo(new MCvScalar(255));
                Features2DToolbox.VoteForUniqueness(goodMatches, uniquenessThreshold, matchesMask);

                // Do we have a minimum amount of unique matches?
                int nonZeroCount = CvInvoke.CountNonZero(matchesMask);
                if (nonZeroCount >= minMatches)
                {
                    // Filter further for size and orientation of the matches.
                    try
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(currentArtifactImage.KeyPoints, observedImage.KeyPoints, goodMatches, matchesMask, 1.2, 20);
                    }
                    catch (System.Runtime.InteropServices.SEHException)
                    {
                        Logger.LogWarning("Can not call the voting for size and orientation. Treat as no match.");
                        continue;
                    }

                    // Still enough matches?
                    if (nonZeroCount >= minMatches)
                    {
                        // Can we find a homography? Then it's a match.
                        Mat stupidMask = new Mat();
                        matchesMask.CopyTo(stupidMask);

                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(currentArtifactImage.KeyPoints, observedImage.KeyPoints, goodMatches, stupidMask, 1);

                        if (IsHomographyApplicable(homography, currentArtifactImage, observedImage))
                        {
                            // Assign the match.
                            matchingArtifact = currentArtifactImage;
                            break;
                        }
                    }
                }

                DescriptorMatcher.Clear();
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Matching finished in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return matchingArtifact;
        }

        private Mat GetRanSaCTransformationMatrix(Mat modelKeyPoints, Mat imageKeyPoints, Mat matches, Mat matchesMask, int iterations)
        {
            return null;
        }

        private bool IsHomographyApplicable(Mat homography, ProcessedImage modelImage, ProcessedImage observedImage)
        {
            int modelHeight = modelImage.Image.Height;
            int modelWidth  = modelImage.Image.Width;
            int modelArea = modelHeight * modelWidth;

            Rectangle modelRectangle = new Rectangle(Point.Empty, modelImage.Image.Size);
            PointF[] modelRectanglePoints = new PointF[]
                {
                    new PointF(modelRectangle.Left, modelRectangle.Bottom),
                    new PointF(modelRectangle.Right, modelRectangle.Bottom),
                    new PointF(modelRectangle.Right, modelRectangle.Top),
                    new PointF(modelRectangle.Left, modelRectangle.Top)
                };
            PointF[] observedRectanglePoints = CvInvoke.PerspectiveTransform(modelRectanglePoints, homography);

            return true;
        }

        /// <summary>
        /// Helper function for debug purposes.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactImage">The matched artifact image containing visual information.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="matchesMask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <returns>The result of DrawMatches.</returns>
        public Mat Draw(ProcessedImage observedImage, ProcessedImage artifactImage, VectorOfVectorOfDMatch matches, Mat matchesMask, Mat homography)
        {
            //Draw the matched keypoints
            Mat result = new Mat();
            Features2DToolbox.DrawMatches(
                artifactImage.Image,
                artifactImage.KeyPoints,
                observedImage.Image,
                observedImage.KeyPoints,
                matches,
                result,
                new MCvScalar(0, 128, 0),
                new MCvScalar(0, 128, 255),
                matchesMask
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
                    CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 255, 0, 255), 5);
                }
            }

            return result;
        }

        /// <summary>
        /// Shorthand to load an image from a file.
        /// </summary>
        /// <param name="imagePath">Path to the image.</param>
        /// <exception cref="FileNotFoundException">If the file doesn't exist.</exception>
        /// <returns>A EmguCV Mat.</returns>
        public Mat LoadImage(string imagePath)
        {
            Mat image;

            if (File.Exists(imagePath)) {
                image = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            } else
            {
                throw new FileNotFoundException("The image couldn't be found");
            }

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
