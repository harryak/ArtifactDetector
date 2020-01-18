using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArbitraryArtifactDetector.Detector.VisualFeatureExtractor.VisualMatchFilter
{
    /// <summary>
    /// Filter for matching VectorOfKeyPoints.
    /// </summary>
    class SimpleMatchFilter : BaseMatchFilter, IMatchFilter
    {
        public SimpleMatchFilter(Setup setup) : base(setup) { }

        public override Matrix<float> GetRanSaCTransformationMatrix(VectorOfKeyPoint modelKeyPoints, VectorOfKeyPoint queryKeyPoints, VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize)
        {
            // Get arrays of key points for easier access.
            MKeyPoint[] modelKeyPointsArray = modelKeyPoints.ToArray();
            MKeyPoint[] queryKeyPointsArray  = queryKeyPoints.ToArray();

            // Setup test variables for return value.
            int bestMatchCount = 0;

            // Get random for Ran(SaC).
            Random random = new Random();

            // Define variables needed for RanSaC runs.
            int matchIndex1, matchIndex2, matchCount;
            PointF translationVector = new PointF();
            SizeF scaleFactors = new SizeF();
            PointF modelPoint1, modelPoint2, queryPoint1, queryPoint2;

            SizeF modelPointsDistance = new SizeF();
            SizeF queryPointsDistance = new SizeF();

            // Setup transformation matrix return.
            Matrix<float> transformationMatrix = new Matrix<float>(3, 3);
            transformationMatrix.SetIdentity();

            // Prepare handling of the matches' mask.
            Matrix<byte> maskInitial = new Matrix<byte>(mask.GetRawData());
            Matrix<byte> maskCurrent = new Matrix<byte>(maskInitial.Size);
            Matrix<byte> bestMask    = new Matrix<byte>(maskInitial.Size);

            // Get list of masked matches for easier access.
            List<IndexedMDMatch> maskedMatchesList = FilterMDMatchArrayOfArray(matches.ToArrayOfArray(), maskInitial);

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
                GetScaleFactors(modelPointsDistance, queryPointsDistance, ref scaleFactors);

                GetTranslationVector(modelPoint1, queryPoint1, scaleFactors, ref translationVector);

                // Count how many matches fit to this model. This also counts the current points.
                foreach (IndexedMDMatch indexedMatch in maskedMatchesList)
                {
                    if (PointFitsModel(modelKeyPointsArray[indexedMatch.match[0].TrainIdx].Point, queryKeyPointsArray[indexedMatch.match[0].QueryIdx].Point, translationVector, scaleFactors, patchSize))
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
                    /*if (matchCount == maskedMatchesList.Count)
                    {
                        break;
                    }*/
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
        /// Tell if the given srcPoint fits to the desPoint with respect to an error patch size.
        /// </summary>
        /// <param name="srcPoint">The point to transform.</param>
        /// <param name="desPoint">Goal point.</param>
        /// <param name="translationVector"></param>
        /// <param name="scaleFactors"></param>
        /// <param name="patchSize">Half length of error square around desPoint.</param>
        /// <returns>True or false.</returns>
        private bool PointFitsModel(PointF srcPoint, PointF desPoint, PointF translationVector, SizeF scaleFactors, int patchSize)
        {
            return IsInTargetPatch(srcPoint.X * scaleFactors.Width + translationVector.X, srcPoint.Y * scaleFactors.Height + translationVector.Y, desPoint, patchSize);
        }

        private void GetScaleFactors(SizeF modelDistance, SizeF queryDistance, ref SizeF scaleFactors)
        {
            // Default is a scale factor of "1" if any of the distances are zero.
            scaleFactors.Width = 1;
            scaleFactors.Height = 1;

            if (modelDistance.Width != 0 && queryDistance.Width != 0)
                scaleFactors.Width = queryDistance.Width / modelDistance.Width;
            if (modelDistance.Height != 0 && queryDistance.Height != 0)
                scaleFactors.Height = queryDistance.Height / modelDistance.Height;
        }

        private void GetTranslationVector(PointF modelPoint1, PointF queryPoint1, SizeF scaleFactors, ref PointF translationVector)
        {
            translationVector.X = queryPoint1.X - scaleFactors.Width * modelPoint1.X;
            translationVector.Y = queryPoint1.Y - scaleFactors.Height * modelPoint1.Y;
        }
    }
}
