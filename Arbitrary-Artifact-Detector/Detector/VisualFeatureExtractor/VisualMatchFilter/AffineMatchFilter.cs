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
    class AffineMatchFilter : BaseMatchFilter, IMatchFilter
    {
        public AffineMatchFilter(Setup setup) : base(setup) { }

        public override Matrix<float> GetRanSaCTransformationMatrix(VectorOfKeyPoint modelKeyPoints, VectorOfKeyPoint queryKeyPoints, VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize)
        {
            // Get arrays of key points for easier access.
            MKeyPoint[] modelKeyPointsArray = modelKeyPoints.ToArray();
            MKeyPoint[] queryKeyPointsArray  = queryKeyPoints.ToArray();

            // Get list of masked matches for easier access.
            List<IndexedMDMatch> maskedMatchesList = FilterMDMatchArrayOfArray(matches.ToArrayOfArray(), new Matrix<byte>(mask.GetRawData()));

            // Setup test variables for return value.
            int bestMatchCount = 0;
            Matrix<float> bestTransformationMatrix = new Matrix<float>(3, 3);
            bestTransformationMatrix.SetIdentity();

            // Get random for Ran(SaC).
            Random random = new Random();

            // Define variables needed for RanSaC runs.
            Matrix<float> modelTriangle = new Matrix<float>(3, 3);
            Matrix<float> invertedModelTriangle = new Matrix<float>(3, 3);
            Matrix<float> adjunctModelTriangle = new Matrix<float>(3, 3);
            Matrix<float> queryTriangle = new Matrix<float>(3, 3);
            int matchIndex1, matchIndex2, matchIndex3, matchCount;

            Matrix<float> transformationMatrix = new Matrix<float>(3, 3);
            transformationMatrix.SetIdentity();

            // Prepare handling of the matches' mask.
            Matrix<byte> maskInitial = new Matrix<byte>(mask.GetRawData());
            Matrix<byte> maskCurrent = new Matrix<byte>(maskInitial.Size);
            Matrix<byte> bestMask    = new Matrix<byte>(maskInitial.Size);

            // The core loop for RanSaC.
            for (int i = 0; i < iterations; i++)
            {
                // Reset match count and mask for current run.
                matchCount = 0;
                maskInitial.CopyTo(maskCurrent);

                // Get three random matches that aren't masked.
                matchIndex1 = random.Next(maskedMatchesList.Count);
                matchIndex2 = -1;
                matchIndex3 = -1;

                // Make sure the matches are distinct.
                do
                {
                    matchIndex2 = random.Next(maskedMatchesList.Count);
                } while (matchIndex1 == matchIndex2);

                do
                {
                    matchIndex3 = random.Next(maskedMatchesList.Count);
                } while (matchIndex1 == matchIndex3 || matchIndex2 == matchIndex3);

                // Get three points each (from the model's set and the query's set) at the current indices and use them for the triangles.
                modelTriangle.SetValue(1f);
                modelTriangle[0, 0] = modelKeyPointsArray[maskedMatchesList[matchIndex1].match[0].TrainIdx].Point.X;
                modelTriangle[1, 0] = modelKeyPointsArray[maskedMatchesList[matchIndex1].match[0].TrainIdx].Point.Y;
                modelTriangle[0, 1] = modelKeyPointsArray[maskedMatchesList[matchIndex2].match[0].TrainIdx].Point.X;
                modelTriangle[1, 1] = modelKeyPointsArray[maskedMatchesList[matchIndex2].match[0].TrainIdx].Point.Y;
                modelTriangle[0, 2] = modelKeyPointsArray[maskedMatchesList[matchIndex3].match[0].TrainIdx].Point.X;
                modelTriangle[1, 2] = modelKeyPointsArray[maskedMatchesList[matchIndex3].match[0].TrainIdx].Point.Y;

                queryTriangle.SetValue(1f);
                queryTriangle[0, 0] = queryKeyPointsArray[maskedMatchesList[matchIndex1].match[0].TrainIdx].Point.X;
                queryTriangle[1, 0] = queryKeyPointsArray[maskedMatchesList[matchIndex1].match[0].TrainIdx].Point.Y;
                queryTriangle[0, 1] = queryKeyPointsArray[maskedMatchesList[matchIndex2].match[0].TrainIdx].Point.X;
                queryTriangle[1, 1] = queryKeyPointsArray[maskedMatchesList[matchIndex2].match[0].TrainIdx].Point.Y;
                queryTriangle[0, 2] = queryKeyPointsArray[maskedMatchesList[matchIndex3].match[0].TrainIdx].Point.X;
                queryTriangle[1, 2] = queryKeyPointsArray[maskedMatchesList[matchIndex3].match[0].TrainIdx].Point.Y;

                // We can only find a transformation matrix if the three points are not colinear.
                if (!IsNonemptyTriangle(modelTriangle) && !IsNonemptyTriangle(queryTriangle))
                    continue;

                // Invert model triangle for transformation matrix. If inversion does not exist, continue.
                if (!Invert(modelTriangle, ref invertedModelTriangle, ref adjunctModelTriangle))
                    continue;

                // Try to find applicable transformation matrix for these matches.
                transformationMatrix = queryTriangle * invertedModelTriangle;

                // Count how many matches fit to this model. This also counts the current points.
                foreach (IndexedMDMatch indexedMatch in maskedMatchesList)
                {
                    if (PointFitsModel(modelKeyPointsArray[indexedMatch.match[0].TrainIdx].Point, queryKeyPointsArray[indexedMatch.match[0].QueryIdx].Point, transformationMatrix, patchSize))
                    {
                        matchCount++;
                    }
                    else
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
                    transformationMatrix.CopyTo(bestTransformationMatrix);

                    bestMask.SetZero();
                    maskCurrent.CopyTo(bestMask);

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
            return bestTransformationMatrix;
        }

        /// <summary>
        /// Tell if the given srcPoint fits to the desPoint with respect to an error patch size.
        /// </summary>
        /// <param name="srcPoint">The point to transform.</param>
        /// <param name="desPoint">Goal point.</param>
        /// <param name="transformationMatrix">Speaks for itself.</param>
        /// <param name="patchSize">Half length of error square around desPoint.</param>
        /// <returns>True or false.</returns>
        private bool PointFitsModel(PointF srcPoint, PointF desPoint, Matrix<float> transformationMatrix, int patchSize)
        {
            Matrix<float> srcPointMat = new Matrix<float>(3, 1);
            srcPointMat.SetValue(1f);
            srcPointMat[0, 0] = srcPoint.X;
            srcPointMat[1, 0] = srcPoint.Y;

            Matrix<float> calculatedDesPoint = new Matrix<float>(3, 1);
            calculatedDesPoint = transformationMatrix * srcPointMat;

            return IsInTargetPatch(calculatedDesPoint[0, 0], calculatedDesPoint[1, 0], desPoint, patchSize);
        }

        /// <summary>
        /// Check to determine whether three points form a triangle with an area > 0.
        /// </summary>
        /// <param name="matrix">3 x 2 matrix with coordinates of points.</param>
        /// <returns>Whether the area is larger than zero.</returns>
        private bool IsNonemptyTriangle(Matrix<float> matrix)
        {
            // Try skipping rest of calculation as soon as possible.
            if (matrix[0, 0] * (matrix[1, 1] - matrix[1, 2]) > 0)
            {
                return true;
            }

            if (matrix[0, 1] * (matrix[1, 2] - matrix[1, 0]) > 0)
            {
                return true;
            }

            return matrix[0, 2] * (matrix[1, 0] - matrix[1, 1]) > 0;
        }

        /// <summary>
        /// Calculates the adjunct matrix of the first matrix parameter.
        /// </summary>
        /// <param name="matrix">Matrix to find the adjunct to.</param>
        /// <param name="adjunctMatrix">Reference to the adjunct matrix memory.</param>
        /// <returns>False if input matrix is not 3x3.</returns>
        private bool Adjunct(Matrix<float> matrix, ref Matrix<float> adjunctMatrix)
        {
            if (matrix.Cols != 3 || matrix.Rows != 3)
                return false;

            adjunctMatrix[0, 0] = matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1];
            adjunctMatrix[0, 1] = matrix[1, 2] * matrix[2, 0] - matrix[1, 0] * matrix[2, 2];
            adjunctMatrix[0, 2] = matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0];

            adjunctMatrix[1, 0] = matrix[0, 2] * matrix[2, 1] - matrix[0, 1] * matrix[2, 2];
            adjunctMatrix[1, 1] = matrix[0, 0] * matrix[2, 2] - matrix[0, 2] * matrix[2, 0];
            adjunctMatrix[1, 2] = matrix[0, 1] * matrix[2, 0] - matrix[0, 0] * matrix[2, 1];

            adjunctMatrix[2, 0] = matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1];
            adjunctMatrix[2, 1] = matrix[0, 2] * matrix[1, 0] - matrix[0, 0] * matrix[1, 2];
            adjunctMatrix[2, 2] = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];

            return true;
        }

        /// <summary>
        /// Inverts the given matrix.
        /// </summary>
        /// <param name="matrix">Matrix to invert.</param>
        /// <param name="invertedMatrix">Reference to the inverted matrix memory.</param>
        /// <param name="adjunctMatrix">Reference to the adjunct matrix memory.</param>
        /// <returns>False if input matrix is not 3x3 or not invertible.</returns>
        private bool Invert(Matrix<float> matrix, ref Matrix<float> invertedMatrix, ref Matrix<float> adjunctMatrix)
        {
            if (matrix.Cols != 3 || matrix.Rows != 3)
                return false;

            float determinant = matrix[0, 0] * matrix[1, 1] * matrix[2, 2]
                + matrix[1, 0] * matrix[2, 1] * matrix[0, 2]
                + matrix[2, 0] * matrix[0, 1] * matrix[1, 2]
                - matrix[2, 0] * matrix[1, 1] * matrix[0, 2]
                - matrix[1, 0] * matrix[0, 1] * matrix[2, 2]
                - matrix[0, 0] * matrix[2, 1] * matrix[1, 2];

            if (determinant == 0 || !Adjunct(matrix, ref adjunctMatrix))
                return false;

            invertedMatrix = adjunctMatrix / determinant;

            return true;
        }
    }
}
