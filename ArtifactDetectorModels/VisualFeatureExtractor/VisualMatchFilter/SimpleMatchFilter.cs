using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor.VisualMatchFilter
{
    /// <summary>
    /// Filter for matching VectorOfKeyPoints using a simpler approach.
    /// </summary>
    internal class SimpleMatchFilter : BaseMatchFilter, IMatchFilter
    {
        public SimpleMatchFilter()
        {
        }

        /// <summary>
        /// Core function: Tries to get a transformation matrix via RanSaC from modelKeyPoints to queryKeyPoints
        /// matching the inlierRatio while allowing errors of patchSize.
        /// </summary>
        /// <param name="modelKeyPoints">Starting set of key points.</param>
        /// <param name="queryKeyPoints">Goal set of key points.</param>
        /// <param name="matches">Previously found matches between modelKeyPoints and queryKeyPoints masked by mask.</param>
        /// <param name="mask">Mask for previously found matches.</param>
        /// <param name="iterations">RanSaC maximum iterations.</param>
        /// <param name="inlierRatio">How many previously found matches should support the hypothesis.</param>
        /// <param name="patchSize">Error threshold for applying the hypothesis on the starting set to get to the goal set.</param>
        /// <returns>A transformation matrix from model to query or null.</returns>
        public override Matrix<float> GetRanSaCTransformationMatrix(ProcessedImage modelKeyPoints, [In] ref ProcessedImage queryKeyPoints, [In] ref VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize)
        {
            // Get arrays of key points for easier access.
            MKeyPoint[] modelKeyPointsArray = modelKeyPoints.KeyPoints.ToArray();
            MKeyPoint[] queryKeyPointsArray  = queryKeyPoints.KeyPoints.ToArray();

            // Setup test variables for return value.
            int bestMatchCount = 0;

            // Get random for Ran(SaC).
            var random = new Random();

            // Define variables needed for RanSaC runs.
            int matchIndex1, matchIndex2, matchCount;
            var translationVector = new PointF();
            var scaleFactors = new SizeF();
            PointF modelPoint1, modelPoint2, queryPoint1, queryPoint2;

            var modelPointsDistance = new SizeF();
            var queryPointsDistance = new SizeF();

            // Setup transformation matrix return.
            var transformationMatrix = new Matrix<float>(3, 3);
            transformationMatrix.SetIdentity();

            // Prepare handling of the matches' mask.
            var maskInitial = new Matrix<byte>(mask.GetRawData());
            var maskCurrent = new Matrix<byte>(maskInitial.Size);
            var bestMask    = new Matrix<byte>(maskInitial.Size);

            // Get list of masked matches for easier access.
            FilterMDMatchArrayOfArray(matches.ToArrayOfArray(), maskInitial, out var maskedMatchesList);

            // The core loop for RanSaC.
            for (int i = 0; i < iterations; i++)
            {
                // Reset match count and mask for current run.
                matchCount = 0;
                maskCurrent.SetZero();

                // Get three random matches that aren't masked.
                matchIndex1 = random.Next(maskedMatchesList.Count);
                matchIndex2 = -1;

                // Make sure the matches are distinct.
                do
                {
                    matchIndex2 = random.Next(maskedMatchesList.Count);
                } while (matchIndex1 == matchIndex2);

                modelPoint1 = modelKeyPointsArray[maskedMatchesList[matchIndex1].match[0].TrainIdx].Point;
                queryPoint1 = queryKeyPointsArray[maskedMatchesList[matchIndex1].match[0].QueryIdx].Point;

                modelPoint2 = modelKeyPointsArray[maskedMatchesList[matchIndex2].match[0].TrainIdx].Point;
                queryPoint2 = queryKeyPointsArray[maskedMatchesList[matchIndex2].match[0].QueryIdx].Point;

                // Try to find applicable transformation matrix for these matches.
                // Get distance vector between point sets.
                modelPointsDistance.Width = Math.Abs(modelPoint1.X - modelPoint2.X);
                modelPointsDistance.Height = Math.Abs(modelPoint1.Y - modelPoint2.Y);
                queryPointsDistance.Width = Math.Abs(queryPoint1.X - queryPoint2.X);
                queryPointsDistance.Height = Math.Abs(queryPoint1.Y - queryPoint2.Y);

                // The distance tells us the scale factor in each direction.
                GetScaleFactors(ref modelPointsDistance, ref queryPointsDistance, ref scaleFactors);

                GetTranslationVector(ref modelPoint1, ref queryPoint1, ref scaleFactors, ref translationVector);

                // Count how many matches fit to this model. This also counts the current points.
                foreach (var indexedMatch in maskedMatchesList)
                {
                    if (PointFitsModel(ref modelKeyPointsArray[indexedMatch.match[0].TrainIdx].Point, ref queryKeyPointsArray[indexedMatch.match[0].QueryIdx].Point, ref translationVector, ref scaleFactors, patchSize))
                    {
                        matchCount++;
                        // Set the mask at the match's original index to "match".
                        maskCurrent[indexedMatch.index, 0] = 255;
                    }
                }

                // Compare this match to previous one's.
                if (matchCount > bestMatchCount)
                {
                    // It's the best match so far.
                    bestMatchCount = matchCount;

                    bestMask.SetZero();
                    maskCurrent.CopyTo(bestMask);

                    transformationMatrix[0, 0] = scaleFactors.Width;
                    transformationMatrix[1, 1] = scaleFactors.Height;
                    transformationMatrix[2, 0] = scaleFactors.Width * translationVector.X;
                    transformationMatrix[2, 1] = scaleFactors.Height * translationVector.Y;

                    // If we matched all input matches we don't have to iterate.
                    if (matchCount == maskedMatchesList.Count)
                    {
                        break;
                    }
                }
            }

            // If we got less inliers than we wanted, treat as no match.
            if (bestMatchCount < inlierRatio * maskedMatchesList.Count)
            {
                return null;
            }

            mask = bestMask.Mat;

            return transformationMatrix;
        }

        /// <summary>
        /// Calculate scale factors based on the given distances.
        /// </summary>
        /// <param name="modelDistance"></param>
        /// <param name="queryDistance"></param>
        /// <param name="scaleFactors"></param>
        private void GetScaleFactors([In] ref SizeF modelDistance, [In] ref SizeF queryDistance, ref SizeF scaleFactors)
        {
            // Default is a scale factor of "1" if any of the distances are zero.
            scaleFactors.Width = 1;
            scaleFactors.Height = 1;

            if (modelDistance.Width != 0 && queryDistance.Width != 0)
                scaleFactors.Width = queryDistance.Width / modelDistance.Width;
            if (modelDistance.Height != 0 && queryDistance.Height != 0)
                scaleFactors.Height = queryDistance.Height / modelDistance.Height;
        }

        /// <summary>
        /// Calculate the translation vector between the two points.
        /// </summary>
        /// <param name="modelPoint1"></param>
        /// <param name="queryPoint1"></param>
        /// <param name="scaleFactors"></param>
        /// <param name="translationVector"></param>
        private void GetTranslationVector([In] ref PointF modelPoint1, [In] ref PointF queryPoint1, [In] ref SizeF scaleFactors, ref PointF translationVector)
        {
            translationVector.X = queryPoint1.X - scaleFactors.Width * modelPoint1.X;
            translationVector.Y = queryPoint1.Y - scaleFactors.Height * modelPoint1.Y;
        }

        /// <summary>
        /// Tell if the given srcPoint fits to the desPoint with respect to an error patch size.
        /// </summary>
        /// <param name="srcPoint">The point to transform.</param>
        /// <param name="desPoint">Goal point.</param>
        /// <param name="translationVector"></param>
        /// <param name="scaleFactors"></param>
        /// <param name="patchSize">Half length of error square around desPoint.</param>
        /// <returns>True or false.</returns>
        private bool PointFitsModel([In] ref PointF srcPoint, [In] ref PointF desPoint, [In] ref PointF translationVector, [In] ref SizeF scaleFactors, [In] int patchSize)
        {
            return IsInTargetPatch(srcPoint.X * scaleFactors.Width + translationVector.X, srcPoint.Y * scaleFactors.Height + translationVector.Y, desPoint, patchSize);
        }
    }
}
