using ArbitraryArtifactDetector.Detector;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Models;
using ArbitraryArtifactDetector.Viewer;
using ArbitraryArtifactDetector.VisualMatchFilter;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.VisualDetector
{
    /// <summary>
    /// Base class for all artifact detectors.
    /// </summary>
    abstract class BaseVisualDetector : BaseDetector, IVisualDetector
    {
        protected BaseVisualDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }

        protected Feature2D FeatureDetector { get; set; }
        protected DescriptorMatcher DescriptorMatcher { get; set; }

        public override DetectorResponse FindArtifact(Setup setup)
        {
            ArtifactType artifactType = null;
            try
            {
                artifactType = setup.ArtifactLibrary.GetArtifactType(setup.ArtifactGoal);
            }
            catch (Exception e)
            {
                Logger.LogError("Could not get artifact type: {0}", e.Message);
                throw new ArgumentNullException("Could not get artifact type: { 0 }", e.Message);
            }

            if (artifactType.Images.Count < 1)
            {
                Logger.LogError("Could not get any images for the artifact type.");
                throw new ArgumentNullException("No images for the artifact type found.");
            }

            ProcessedImage observedImage = ExtractFeatures(setup.ScreenshotPath);

            bool artifactTypeFound = ImageContainsArtifactType(observedImage, artifactType, setup.MatchFilter, out Mat drawingResult, out int matchCount);

#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Show the results in a window.
            if (drawingResult != null)
                Application.Run(new ImageViewer(drawingResult));
#endif

            // Chache the artifact library.
            if (setup.ShouldCache)
            {
                setup.ArtifactLibrary.ExportToFile(setup.LibraryFileName, setup.WorkingDirectory);
                Logger.LogInformation("Exported artifact library to {libraryFileName}.", setup.WorkingDirectory + setup.LibraryFileName);
            }

            Logger.LogInformation("The comparison yielded {0}.", artifactTypeFound);

            if (setup.ShouldEvaluate)
            {
                try
                {
                    bool printHeader = false;
                    if (!File.Exists("output.csv")) printHeader = true;
                    using (StreamWriter file = new StreamWriter("output.csv", true))
                    {
                        if (printHeader) file.WriteLine("artifactDetector;screenshot_path;artifact_goal;" + setup.Stopwatch.LabelsToCSV() + ";found;match_count");

                        file.WriteLine(setup.DetectorSelection + ";" + setup.ScreenshotPath + ";" + setup.ArtifactGoal + ";" + setup.Stopwatch.TimesToCSVinNSprecision() + ";" + artifactTypeFound + ";" + matchCount);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not write to output.csv: {0}", e.Message);
                }
            }

            return new DetectorResponse() { ArtifactFound = artifactTypeFound, ArtifactLikely = artifactTypeFound, Certainty = 100 };
        }

        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
        public ProcessedImage ExtractFeatures(string imagePath)
        {
            StartStopwatch();

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

            StopStopwatch("Extracted features from image in {0} ms.");

            if (descriptors.Width < 1)
            {
                Logger.LogError("Could not load any descriptors for image {0}.", imagePath);
                return null;
            }

            return new ProcessedImage(image, keyPoints, descriptors);
        }

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="matchFilter">The filter used for matching.</param>
        /// <param name="drawingResult">The result of the drawing (for Debug).</param>
        /// <param name="matchCount">Count of matches, if found.</param>
        /// <returns>A homography or null if none was found.</returns>
        public bool ImageContainsArtifactType(ProcessedImage observedImage, ArtifactType artifactType, IMatchFilter matchFilter, out Mat drawingResult, out int matchCount)
        {
            // Only needed for debugging purposes, otherwise will always be null.
            drawingResult = null;

            // Same.
            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            Mat matchesMask = new Mat();
            Matrix<float> homography = new Matrix<float>(3, 3);

            // Get a matched artifact image or null.
            ProcessedImage matchedArtifact = FindMatch(
                observedImage,
                artifactType,
                matchFilter,
                out matches,
                out matchesMask,
                out homography,
                out matchCount
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
        /// <param name="matchFilter">The filter used for matching.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="matchesMask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <returns>A matched artifact image, if available.</returns>
        private ProcessedImage FindMatch(ProcessedImage observedImage, ArtifactType artifactType, IMatchFilter matchFilter, out VectorOfVectorOfDMatch goodMatches, out Mat matchesMask, out Matrix<float> homography, out int matchCount)
        {
            int minMatches = AADConfig.MinimumMatchesRequired;
            double uniquenessThreshold = AADConfig.MatchUniquenessThreshold;
            double distanceThreshold = AADConfig.MatchDistanceThreshold;

            ProcessedImage matchingArtifact = null;

            // Initialize out variables.
            goodMatches = new VectorOfVectorOfDMatch();

            VectorOfVectorOfDMatch matches;
            matchesMask = new Mat();
            matchCount = 0;

            // Only needed for debugging output, otherwise will always be null.
            homography = null;

            // Get the stopwatch for matching.
            StartStopwatch();

            int artifactNumber = 0;
            foreach (var currentArtifactImage in artifactType.Images)
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
                        if (match[0].Distance < distanceThreshold)
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
                Features2DToolbox.VoteForUniqueness(goodMatches, uniquenessThreshold, matchesMask);

                // Do we have a minimum amount of unique matches?
                int nonZeroCount = CvInvoke.CountNonZero(matchesMask);
                //TODO is this a good idea and why?
                double sizeRatio = GetSizeRatio(observedImage.Dimensions, currentArtifactImage.Dimensions);

                if (nonZeroCount >= minMatches / sizeRatio)
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
                    if (nonZeroCount >= minMatches / sizeRatio)
                    {
                        // Can we find a homography? Then it's a match.
                        homography = matchFilter.GetRanSaCTransformationMatrix(currentArtifactImage.KeyPoints, observedImage.KeyPoints, goodMatches, ref matchesMask, 1000, 0.85, 5);

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

            StopStopwatch("Matching finished in {0} ms.");

            return matchingArtifact;
        }

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

            return ratio;
        }

#if DEBUG
        /// <summary>
        /// Helper function for debug purposes.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactImage">The matched artifact image containing visual information.</param>
        /// <param name="matches">Reference to a match vector.</param>
        /// <param name="matchesMask">Reference to the used result mask.</param>
        /// <param name="homography">Reference to a possible homography.</param>
        /// <returns>The result of DrawMatches.</returns>
        private Mat Draw(ProcessedImage observedImage, ProcessedImage artifactImage, VectorOfVectorOfDMatch matches, Mat matchesMask, Matrix<float> homography)
        {
            //Draw the matched keypoints
            Mat resultingImage = new Mat();

            Features2DToolbox.DrawMatches(
                artifactImage.Image,
                artifactImage.KeyPoints,
                observedImage.Image,
                observedImage.KeyPoints,
                matches,
                resultingImage,
                new MCvScalar(0, 128, 0),
                new MCvScalar(0, 128, 255),
                matchesMask
            );

            if (homography != null)
            {
                // Draw a rectangle along the projected model.
                Rectangle rect = new Rectangle(Point.Empty, artifactImage.Image.Size);
                // Set corner points of rectangle and transform them according to homography.
                PointF[] pointFs = new PointF[]
                {
                    new PointF(rect.Left, rect.Bottom),
                    new PointF(rect.Right, rect.Bottom),
                    new PointF(rect.Right, rect.Top),
                    new PointF(rect.Left, rect.Top)
                };
                pointFs = Transform(pointFs, homography);

                // Draw the outlines using rounded coordinates of previously calculated points.
                using (VectorOfPoint pointVector = new VectorOfPoint(Array.ConvertAll(pointFs, Point.Round)))
                {
                    CvInvoke.Polylines(resultingImage, pointVector, true, new MCvScalar(0, 0, 255, 255), 8);
                }
            }

            return resultingImage;
        }
#endif

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
                throw new FileNotFoundException("The image couldn't be found");
            }

            if (image.IsEmpty)
            {
                Logger.LogError("Image couldn't be loaded ({imagePath}).", imagePath);
                throw new ArgumentException("The image was loaded as empty.");
            }

            return image;
        }
    }
}
