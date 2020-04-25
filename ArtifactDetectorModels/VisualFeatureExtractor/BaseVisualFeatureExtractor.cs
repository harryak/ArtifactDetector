using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor.VisualMatchFilter;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Feature extraction base class doing most of the work but missing the EmguCV.Feature2D feature detector instantiation.
    /// </summary>
    internal abstract class BaseVisualFeatureExtractor : IVisualFeatureExtractor
    {
        /// <summary>
        /// Map for selecting a feature match filter by its name.
        /// </summary>
        private readonly Dictionary<string, Func<IMatchFilter>> visualFilterSelectionMap =
            new Dictionary<string, Func<IMatchFilter>>()
            {
                { "simple", () => { return new SimpleMatchFilter(); } },
                { "affine", () => { return new AffineMatchFilter(); } },
            };

        /// <summary>
        /// Constructor setting up the match filter.
        /// </summary>
        /// <param name="setup">The current excecution's setup.</param>
        public BaseVisualFeatureExtractor(double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
        {
            MatchDistanceThreshold = matchDistanceThreshold;
            MatchUniquenessThreshold = matchUniquenessThreshold;
            MinimumMatchesRequired = minimumMatchesRequired;

            Logger = logger;
            SetupMatchFilter(matchFilterSelection);
        }

        /// <summary>
        /// Internal descriptor matcher, to be set by children classes.
        /// </summary>
        protected DescriptorMatcher DescriptorMatcher { get; set; }

        /// <summary>
        /// Internal feature detector, to be set by children classes.
        /// </summary>
        protected Feature2D FeatureDetector { get; set; }

        /// <summary>
        /// Logging instance to write messages to.
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Maximum distance to consider matches.
        /// </summary>
        protected double MatchDistanceThreshold { get; set; }

        /// <summary>
        /// The filter used for matching the feature sets of two images.
        /// </summary>
        protected IMatchFilter MatchFilter { get; set; }

        /// <summary>
        /// Maximum uniqueness to consider matches.
        /// </summary>
        protected double MatchUniquenessThreshold { get; set; }

        /// <summary>
        /// Minimum threshold as to what matches count.
        /// </summary>
        protected int MinimumMatchesRequired { get; set; }

        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="image">Image to extract features from.</param>
        /// <returns>The observed image with keypoints and descriptors or null on error.</returns>
        public ProcessedImage ExtractFeatures(Mat image)
        {
            var keyPoints = new VectorOfKeyPoint();
            var descriptors = new Mat();

            if (image.Height < 64 || image.Width < 64)
            {
                var borderImage = new Mat();
                int verticalBorder = 32;
                int horizontalBorder = 32;
                CvInvoke.CopyMakeBorder(image, borderImage, verticalBorder, verticalBorder, horizontalBorder, horizontalBorder, BorderType.Replicate);
                image = borderImage;
            }

            // Get the descriptors from the EmguCV feature detector.
            FeatureDetector.DetectAndCompute(image.GetUMat(AccessType.Read), null, keyPoints, descriptors, false);

            if (descriptors.Width < 1)
            {
                Logger.LogError("Could not load any descriptors for image.");
                return null;
            }

            return new ProcessedImage(image, keyPoints, descriptors);
        }

        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath">Path to image to extract features from.</param>
        /// <returns>The observed image with keypoints and descriptors or null on error.</returns>
        public ProcessedImage ExtractFeatures(string imagePath)
        {
            Mat image;
            try
            {
                // Get image from file system.
                image = LoadImage(imagePath);
            }
            catch (Exception e)
            {
                Logger.LogError("Could not load the image at {0}: {1}", imagePath, e.Message);
                return null;
            }

            return ExtractFeatures(image);
        }

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="referenceImages">Reference images for the artifact type.</param>
        /// <param name="drawingResult">The result of the drawing (for Debug).</param>
        /// <param name="matchCount">Count of matches, if found.</param>
        /// <returns>A homography or null if none was found.</returns>
        public bool ImageMatchesReference(ProcessedImage observedImage, ICollection<ProcessedImage> referenceImages, out Mat drawingResult, out int matchCount)
        {
            // Only needed for debugging purposes, otherwise will always be null.
            drawingResult = null;

            // Same for this.
            var matches = new VectorOfVectorOfDMatch();
            var matchesMask = new Mat();
            var homography = new Matrix<float>(3, 3);

            // Get a matched artifact image or null.
            var matchedArtifact = FindMatch(
                observedImage,
                referenceImages,
                out matches,
                out matchesMask,
                out homography,
                out matchCount
            );

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
        /// <returns>A matched artifact image, if available.</returns>
        private ProcessedImage FindMatch(ProcessedImage observedImage, ICollection<ProcessedImage> referenceImages, out VectorOfVectorOfDMatch goodMatches, out Mat matchesMask, out Matrix<float> homography, out int matchCount)
        {
            ProcessedImage matchingArtifact = null;

            // Initialize out variables.
            goodMatches = new VectorOfVectorOfDMatch();

            VectorOfVectorOfDMatch matches;
            matchesMask = new Mat();
            matchCount = 0;

            // Only needed for debugging output, otherwise will always be null.
            homography = null;

            int artifactNumber = 0;
            foreach (var currentArtifactImage in referenceImages)
            {
                // Add model descriptors to matcher.
                DescriptorMatcher.Add(currentArtifactImage.Descriptors);

                // Match this set with the observed image descriptors.
                matches = new VectorOfVectorOfDMatch();
                DescriptorMatcher.KnnMatch(observedImage.Descriptors, matches, 2, null);

                // Is the best matche's distance below the threshold?
                // Also: Apply Lowe's ratio test for 0.7 to the matches.
                MDMatch[][] matchesArray = matches.ToArrayOfArray();
                goodMatches = new VectorOfVectorOfDMatch();
                foreach (MDMatch[] match in matchesArray)
                {
                    // FLANN does not always output two matching points.
                    if (match.Length > 1)
                    {
                        if (match[0].Distance < MatchDistanceThreshold)
                        {
                            if (match[0].Distance < 0.7 * match[1].Distance)
                            {
                                goodMatches.Push(new VectorOfDMatch(match));
                            }
                        }
                    }
                }
                // Get rid of the original matches object, no need now.
                matches.Dispose();

                matchesMask = new Mat(goodMatches.Size, 1, DepthType.Cv8U, 1);
                matchesMask.SetTo(new MCvScalar(255));

                // Check the matches, only take unique ones.
                Features2DToolbox.VoteForUniqueness(goodMatches, MatchUniquenessThreshold, matchesMask);

                // Do we have a minimum amount of unique matches?
                int nonZeroCount = CvInvoke.CountNonZero(matchesMask);
                // Calculate the ratio between the reference image and the current artifact image to rule out influence of feature density.
                double sizeRatio = GetSizeRatio(observedImage.Dimensions, currentArtifactImage.Dimensions);

                if (nonZeroCount >= MinimumMatchesRequired / sizeRatio)
                {
                    // Filter further for size and orientation of the matches.
                    try
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(currentArtifactImage.KeyPoints, observedImage.KeyPoints, goodMatches, matchesMask, 1.2, 20);
                    }
                    catch (System.Runtime.InteropServices.SEHException)
                    {
                        Logger.LogWarning("Can not call the voting for size and orientation. Better not continueing.");
                        continue;
                    }

                    // Still enough matches?
                    if (nonZeroCount >= MinimumMatchesRequired / sizeRatio)
                    {
                        // Can we find a homography? Then it's a match.
                        homography = MatchFilter.GetRanSaCTransformationMatrix(currentArtifactImage.KeyPoints, observedImage.KeyPoints, goodMatches, ref matchesMask, 1000, 0.85, 5);

                        if (homography != null)
                        {
                            // Assign the match.
                            matchingArtifact = currentArtifactImage;
                            matchCount = CvInvoke.CountNonZero(matchesMask);
                            // Break, we do not need further searching.
                            break;
                        }
                    }
                }

                DescriptorMatcher.Clear();
                artifactNumber++;
            }

            return matchingArtifact;
        }

        /// <summary>
        /// Calculate the ratio between two image sizes.
        /// </summary>
        /// <param name="image1">Size of image one.</param>
        /// <param name="image2">Size of image two.</param>
        /// <returns>The ratio.</returns>
        private double GetSizeRatio(SizeF image1, SizeF image2)
        {
            double ratio;
            double area1 = image1.Width * image1.Height;
            double area2 = image2.Width * image2.Height;

            double squaredAreaRatio;

            if (area1 < area2)
            {
                squaredAreaRatio = area2 / area1;
                ratio = Math.Sqrt(squaredAreaRatio);
            }
            else
            {
                squaredAreaRatio = area1 / area2;
                ratio = Math.Sqrt(squaredAreaRatio);
            }

            return Math.Min(ratio, 2);
        }

        /// <summary>
        /// Shorthand to load an image from a file.
        /// </summary>
        /// <param name="imagePath">Path to the image.</param>
        /// <exception cref="FileNotFoundException">If the file doesn't exist.</exception>
        /// <returns>A EmguCV Mat.</returns>
        private Mat LoadImage(string imagePath)
        {
            Mat image;

            if (File.Exists(imagePath))
            {
                image = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            }
            else
            {
                throw new FileNotFoundException("The image couldn't be found.");
            }

            if (image.IsEmpty)
            {
                Logger.LogError("Image couldn't be loaded ({imagePath}).", imagePath);
                throw new ArgumentException("The image was loaded as empty.");
            }

            return image;
        }

        /// <summary>
        /// Simple setup of the match filter by getting the right instance from the map.
        /// </summary>
        /// <param name="setup">The current excecution's setup.</param>
        private void SetupMatchFilter(string matchFilterSelection)
        {
            if (!visualFilterSelectionMap.ContainsKey(matchFilterSelection))
            {
                throw new ArgumentNullException("Could not get match filter type: { 0 }", matchFilterSelection);
            }

            Logger.LogInformation("Using match filter {filterSelection}.", matchFilterSelection);
            MatchFilter = visualFilterSelectionMap[matchFilterSelection]();
        }

        /// <summary>
        /// Transform all points in input list with the given matrix.
        /// </summary>
        /// <param name="input">List of points to transform.</param>
        /// <param name="matrix">Matrix used to transform.</param>
        /// <returns>List of all transformed points.</returns>
        private PointF[] Transform(PointF[] input, Matrix<float> matrix)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i].X = input[i].X * matrix[0, 0] + input[i].Y * matrix[1, 0] + matrix[2, 0];
                input[i].Y = input[i].X * matrix[0, 1] + input[i].Y * matrix[1, 1] + matrix[2, 1];
            }

            return input;
        }
    }
}