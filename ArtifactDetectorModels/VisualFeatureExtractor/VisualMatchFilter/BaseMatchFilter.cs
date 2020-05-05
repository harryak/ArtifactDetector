using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor.VisualMatchFilter
{
    /// <summary>
    /// Base class for match filters implementing most of the ground features.
    /// </summary>
    internal abstract class BaseMatchFilter : IMatchFilter
    {
        /// <summary>
        /// Core function: Has to be implemented by the deriving classes.
        /// </summary>
        /// <param name="modelKeyPoints">Starting set of key points.</param>
        /// <param name="queryKeyPoints">Goal set of key points.</param>
        /// <param name="matches">Previously found matches between modelKeyPoints and queryKeyPoints masked by mask.</param>
        /// <param name="mask">Mask for previously found matches.</param>
        /// <param name="iterations">RanSaC maximum iterations.</param>
        /// <param name="inlierRatio">How many previously found matches should support the hypothesis.</param>
        /// <param name="patchSize">Error threshold for applying the hypothesis on the starting set to get to the goal set.</param>
        /// <returns>A transformation matrix from model to query or null.</returns>
        public abstract Matrix<float> GetRanSaCTransformationMatrix(ProcessedImage modelKeyPoints, [In] ref ProcessedImage queryKeyPoints, [In] ref VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize);

        /// <summary>
        /// Extract list of indexed matches from inputArray using the given mask.
        /// </summary>
        /// <param name="inputArray">The 2D input array.</param>
        /// <param name="mask">Mask for the input array.</param>
        /// <returns>List of indexed matches where mask is 1.</returns>
        protected void FilterMDMatchArrayOfArray(MDMatch[][] inputArray, Matrix<byte> mask, out List<IndexedMDMatch> filteredMatches)
        {
            filteredMatches = new List<IndexedMDMatch>();

            for (int i = 0; i < inputArray.Length && i < mask.Size.Height; i++)
            {
                if (mask[i, 0] > 0) filteredMatches.Add(new IndexedMDMatch(i, inputArray[i]));
            }
        }

        /// <summary>
        /// Test if point is equal to patchCenter with respect to error patch size patchSize.
        /// </summary>
        /// <param name="point">Test value.</param>
        /// <param name="rangeCenter">Goal value center.</param>
        /// <param name="errorRangeRadius">Error range radius.</param>
        /// <returns>If point is within range.</returns>
        protected bool IsInRange(float point, float rangeCenter, float errorRangeRadius)
            => point >= point - errorRangeRadius
            && point <= point + errorRangeRadius;

        /// <summary>
        /// Test if the 2D point is within a square of patchSize around patchCenter.
        /// </summary>
        /// <param name="pointX">Test value x coordinate.</param>
        /// <param name="pointY">Test value y coordinate.</param>
        /// <param name="patchCenter">Goal value center.</param>
        /// <param name="patchSize">Half length of error square.</param>
        /// <returns>If point is within square patch.</returns>
        protected bool IsInTargetPatch(float pointX, float pointY, PointF patchCenter, float patchSize)
            => IsInRange(pointX, patchCenter.X, patchSize)
            && IsInRange(pointY, patchCenter.Y, patchSize);

        /// <summary>
        /// Adds an index to a list of MDMatches.
        /// </summary>
        public struct IndexedMDMatch
        {
            public int index;
            public MDMatch[] match;

            public IndexedMDMatch(int i, MDMatch[] mDMatch) : this()
            {
                index = i;
                match = mDMatch;
            }
        }
    }
}
