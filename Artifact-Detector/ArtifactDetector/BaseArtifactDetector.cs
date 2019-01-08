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
using Emgu.Util;
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
        public bool AnalyzeScreenshot(ObservedImage observedImage, ArtifactType artifactType)
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
                new Stopwatch()
            );

# if DEBUG
            Draw(observedImage, artifactType, matches, mask, homography);
# endif

            return true;
        }

        /**
         * Extracts features of a model image, given by its path.
         */
        public ObservedImage ExtractFeatures(string imagePath, Stopwatch stopwatch = null)
        {
            throw new NotImplementedException("This function can't be implemented in the base class.");
        }

        /**
         * Try to find a match between a model and an observed image.
         */
        public void FindMatch(ObservedImage observedImage, ArtifactType artifactType, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            throw new NotImplementedException("This function can't be implemented in the base class.");
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
