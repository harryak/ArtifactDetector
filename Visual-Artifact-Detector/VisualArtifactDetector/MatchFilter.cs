/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace VisualArtifactDetector.VisualArtifactDetector
{
    public struct IndexedMDMatch
    {
        public int index;
        public MDMatch[] match;

        public IndexedMDMatch(int i, MDMatch[] mDMatch) : this()
        {
            this.index = i;
            this.match = mDMatch;
        }
    }

    public class MatchFilter
    {
        public Matrix<float> GetRanSaCTransformationMatrix(VectorOfKeyPoint modelKeyPoints, VectorOfKeyPoint queryKeyPoints, VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize)
        {
            MKeyPoint[] modelKeyPointsArray = modelKeyPoints.ToArray();
            MKeyPoint[] queryKeyPointsArray  = queryKeyPoints.ToArray();

            List<IndexedMDMatch> filteredMatches = FilterMDMatchArrayOfArray(matches.ToArrayOfArray(), new Matrix<byte>(mask.GetData()));

            int bestMatchCount = 0;
            float bestMatchLambda = 1, bestMatchSigma = 1;
            SizeF bestTranslation = new SizeF(0, 0);

            Random random = new Random();

            // Define variables needed for RANSAC runs.
            int matchIndex1, matchIndex2, matchCount;
            PointF modelPoint1, modelPoint2, queryPoint1, queryPoint2;
            SizeF translation, modelPointsDistance, queryPointsDistance, scaleFactors;

            Matrix<byte> maskInitial = new Matrix<byte>(mask.GetData());
            Matrix<byte> maskCurrent = new Matrix<byte>(maskInitial.Size);
            Matrix<byte> bestMask    = new Matrix<byte>(maskInitial.Size);

            for (int i = 0; i < iterations; i++)
            {
                // Reset match count and mask.
                matchCount = 0;
                maskInitial.CopyTo(maskCurrent);

                // Get two random matches that aren't masked.
                matchIndex1 = random.Next(filteredMatches.Count);
                matchIndex2 = -1;

                // Make sure they're distinct.
                do
                {
                    matchIndex2 = random.Next(filteredMatches.Count);
                } while (matchIndex1 == matchIndex2);

                // Get the two points at the current indices.
                modelPoint1 = modelKeyPointsArray[filteredMatches[matchIndex1].match[0].TrainIdx].Point;
                queryPoint1 = queryKeyPointsArray[filteredMatches[matchIndex1].match[0].QueryIdx].Point;

                modelPoint2 = modelKeyPointsArray[filteredMatches[matchIndex2].match[0].TrainIdx].Point;
                queryPoint2 = queryKeyPointsArray[filteredMatches[matchIndex2].match[0].QueryIdx].Point;

                // To find a 2D scale factor via the point distances, X and Y of both point sets have to be different from each other.
                if (modelPoint1.X == modelPoint2.X || modelPoint1.Y == modelPoint2.Y)
                    continue;
                if (queryPoint1.X == queryPoint2.X || queryPoint1.Y == queryPoint2.Y)
                    continue;

                // Try to find applicable transformation matrix for these matches.
                // Get distance vector between point sets.
                modelPointsDistance = new SizeF(Math.Abs(modelPoint1.X - modelPoint2.X), Math.Abs(modelPoint1.Y - modelPoint2.Y));
                queryPointsDistance = new SizeF(Math.Abs(queryPoint1.X - queryPoint2.X), Math.Abs(queryPoint1.Y - queryPoint2.Y));

                // The distance tells us the scale factor in each direction.
                scaleFactors = GetScaleFactors(modelPointsDistance, queryPointsDistance);

                translation = GetTranslationVector(modelPoint1, queryPoint1, scaleFactors);

                // If not even our second point matches this, abort this run and try anew.
                if (!PointFitsModel(modelPoint2, queryPoint2, scaleFactors.Width, scaleFactors.Height, translation, patchSize))
                    continue;

                // Count how many matches fit to this model. This also counts the current points.
                foreach (IndexedMDMatch indexedMatch in filteredMatches)
                {
                    if (PointFitsModel(modelKeyPointsArray[indexedMatch.match[0].TrainIdx].Point, queryKeyPointsArray[indexedMatch.match[0].QueryIdx].Point, scaleFactors.Width, scaleFactors.Height, translation, patchSize))
                    {
                        matchCount++;
                    } else
                    {
                        // Set the mask at the matche's original index to zero.
                        maskCurrent[indexedMatch.index, 0] = 0;
                    }
                }

                // Compare this match to previous one's.
                if (matchCount > bestMatchCount)
                {
                    // It's the best match so far.
                    bestMatchCount = matchCount;
                    bestMatchLambda = scaleFactors.Width;
                    bestMatchSigma = scaleFactors.Height;
                    bestTranslation = translation;
                    maskCurrent.CopyTo(bestMask);
                }
            }

            // If we got less inliers than we wanted, treat as no match.
            if (bestMatchCount < inlierRatio * filteredMatches.Count)
            {
                return null;
            }

            //PrintMatrix(homography);
            Matrix<float> transformationMatrix = new Matrix<float>(3, 3);
            transformationMatrix.SetZero();
            transformationMatrix[0, 0] = bestMatchLambda;
            transformationMatrix[1, 1] = bestMatchSigma;
            transformationMatrix[2, 2] = 1;
            transformationMatrix[2, 0] = bestMatchLambda * bestTranslation.Width;
            transformationMatrix[2, 1] = bestMatchSigma * bestTranslation.Height;

            mask = bestMask.Mat;
            return transformationMatrix;
        }

        private bool IsInTargetPatch(PointF point, PointF patchCenter, int patchSize)
        {
            return point.X >= patchCenter.X - patchSize && point.X <= patchCenter.X + patchSize
                && point.Y >= patchCenter.Y - patchSize && point.Y <= patchCenter.Y + patchSize;
        }

        private bool IsInTargetPatch(float pointX, float pointY, PointF patchCenter, int patchSize)
        {
            return pointX >= patchCenter.X - patchSize && pointX <= patchCenter.X + patchSize
                && pointY >= patchCenter.Y - patchSize && pointY <= patchCenter.Y + patchSize;
        }

        private bool PointFitsModel(PointF srcPoint, PointF desPoint, float modelLambda, float modelSigma, SizeF translation, int patchSize)
        {
            return IsInTargetPatch(modelLambda * (srcPoint.X + translation.Width), modelSigma * (srcPoint.Y + translation.Height), desPoint, patchSize);
        }

        private List<IndexedMDMatch> FilterMDMatchArrayOfArray(MDMatch[][] inputArray, Matrix<byte> mask)
        {
            List<IndexedMDMatch> filteredMatches = new List<IndexedMDMatch>();

            for (int i = 0; i < inputArray.Length && i < mask.Size.Height; i++)
            {
                if (mask[i, 0] > 0) filteredMatches.Add(new IndexedMDMatch(i, inputArray[i]));
            }

            return filteredMatches;
        }

        private SizeF GetScaleFactors(SizeF modelDistance, SizeF queryDistance)
        {
            float lambda = 0, sigma = 0;

            // There is a test so that the second with should never be 0, but just to be sure...
            if (modelDistance.Width != 0)
                lambda = queryDistance.Width / modelDistance.Width;
            if (modelDistance.Height != 0)
                sigma = queryDistance.Height / modelDistance.Height;

            return new SizeF(lambda, sigma);
        }

        private SizeF GetTranslationVector(PointF modelPoint1, PointF queryPoint1, SizeF scaleFactors)
        {
            return new SizeF(queryPoint1.X - scaleFactors.Width  * modelPoint1.X,
                             queryPoint1.Y - scaleFactors.Height * modelPoint1.Y);
        }

        /// <summary>
        /// Uses formula l * (p1.X - t.X) = p2.X
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second, scaled and offsetted point.</param>
        /// <param name="translation">The offset vector.</param>
        /// <returns></returns>
        private float CalcMatchLambda(PointF point1, PointF point2, SizeF translation)
        {
            // Try not do divide by zero.
            float translatedX1 = point1.X + translation.Width;
            if (translatedX1 == 0)
            {
                if (point2.X == 0)
                {
                    return -1; // We cannot tell anything about l.
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return point2.X / translatedX1;
            }
        }

        /// <summary>
        /// Uses formula s * (p1.Y - t.Y) = p2.Y
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second, scaled and offsetted point.</param>
        /// <param name="translation">The offset vector.</param>
        /// <returns></returns>
        private float CalcMatchSigma(PointF point1, PointF point2, SizeF translation)
        {
            // Try not do divide by zero.
            float translatedY1 = point1.Y + translation.Height;
            if (translatedY1 == 0)
            {
                if (point2.Y == 0)
                {
                    return -1; // We cannot tell anything about l.
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return point2.Y / translatedY1;
            }
        }
    }
}
