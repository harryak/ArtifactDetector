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
    /// Filter for matching VectorOfKeyPoints using affine transformations.
    /// </summary>
    internal class AffineMatchFilter : BaseMatchFilter, IMatchFilter
    {
        public AffineMatchFilter()
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

            // Get list of masked matches for easier access.
            FilterMDMatchArrayOfArray(matches.ToArrayOfArray(), new Matrix<byte>(mask.GetRawData()), out var maskedMatchesList);

            // Setup test variables for return value.
            int bestMatchCount = 0;
            var bestTransformationMatrix = new Matrix<float>(3, 3);
            bestTransformationMatrix.SetIdentity();

            // Get random for Ran(SaC).
            var random = new Random();

            // Define variables needed for RanSaC runs.
            var modelTriangle = new Matrix<float>(3, 3);
            var invertedModelTriangle = new Matrix<float>(3, 3);
            var adjunctModelTriangle = new Matrix<float>(3, 3);
            var queryTriangle = new Matrix<float>(3, 3);
            int matchIndex1, matchIndex2, matchIndex3, matchCount;

            var transformationMatrix = new Matrix<float>(3, 3);
            transformationMatrix.SetIdentity();

            // Prepare handling of the matches' mask.
            var maskInitial = new Matrix<byte>(mask.GetRawData());
            var maskCurrent = new Matrix<byte>(maskInitial.Size);
            var bestMask    = new Matrix<byte>(maskInitial.Size);

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
                if (!IsNonemptyTriangle(ref modelTriangle) && !IsNonemptyTriangle(ref queryTriangle))
                    continue;

                // Invert model triangle for transformation matrix. If inversion does not exist, continue.
                if (!Invert(ref modelTriangle, ref invertedModelTriangle, ref adjunctModelTriangle))
                    continue;

                // Try to find applicable transformation matrix for these matches.
                transformationMatrix = queryTriangle * invertedModelTriangle;

                // Count how many matches fit to this model. This also counts the current points.
                foreach (var indexedMatch in maskedMatchesList)
                {
                    if (PointFitsModel(ref modelKeyPointsArray[indexedMatch.match[0].TrainIdx].Point, ref queryKeyPointsArray[indexedMatch.match[0].QueryIdx].Point, ref transformationMatrix, patchSize))
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
            return bestTransformationMatrix;
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
        private bool Invert([In] ref Matrix<float> matrix, ref Matrix<float> invertedMatrix, ref Matrix<float> adjunctMatrix)
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

        /// <summary>
        /// Check to determine whether three points form a triangle with an area > 0.
        /// </summary>
        /// <param name="matrix">3 x 2 matrix with coordinates of points.</param>
        /// <returns>Whether the area is larger than zero.</returns>
        private bool IsNonemptyTriangle([In] ref Matrix<float> matrix)
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
        /// Tell if the given srcPoint fits to the desPoint with respect to an error patch size.
        /// </summary>
        /// <param name="srcPoint">The point to transform.</param>
        /// <param name="desPoint">Goal point.</param>
        /// <param name="transformationMatrix">Speaks for itself.</param>
        /// <param name="patchSize">Half length of error square around desPoint.</param>
        /// <returns>True or false.</returns>
        private bool PointFitsModel([In] ref PointF srcPoint, [In] ref PointF desPoint, [In] ref Matrix<float> transformationMatrix, int patchSize)
        {
            var srcPointMat = new Matrix<float>(3, 1);
            srcPointMat.SetValue(1f);
            srcPointMat[0, 0] = srcPoint.X;
            srcPointMat[1, 0] = srcPoint.Y;

            var calculatedDesPoint = new Matrix<float>(3, 1);
            calculatedDesPoint = transformationMatrix * srcPointMat;

            return IsInTargetPatch(calculatedDesPoint[0, 0], calculatedDesPoint[1, 0], desPoint, patchSize);
        }
    }
}
