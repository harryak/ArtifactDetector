/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Drawing;

namespace ArbitraryArtifactDetector.VisualMatchFilter
{
    public abstract class BaseMatchFilter : IMatchFilter
    {
        private ILogger Logger { get; set; }
        private VADStopwatch Stopwatch { get; set; }

        protected BaseMatchFilter(ILogger logger, VADStopwatch stopwatch)
        {
            Logger = logger;
            Stopwatch = stopwatch;
        }

        public abstract Matrix<float> GetRanSaCTransformationMatrix(VectorOfKeyPoint modelKeyPoints, VectorOfKeyPoint queryKeyPoints, VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize);

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
        
        /// <summary>
        /// Extract list of indexed matches from inputArray using the given mask.
        /// </summary>
        /// <param name="inputArray">The 2D input array.</param>
        /// <param name="mask">Mask for the input array.</param>
        /// <returns>List of indexed matches where mask is 1.</returns>
        protected List<IndexedMDMatch> FilterMDMatchArrayOfArray(MDMatch[][] inputArray, Matrix<byte> mask)
        {
            List<IndexedMDMatch> filteredMatches = new List<IndexedMDMatch>();

            for (int i = 0; i < inputArray.Length && i < mask.Size.Height; i++)
            {
                if (mask[i, 0] > 0) filteredMatches.Add(new IndexedMDMatch(i, inputArray[i]));
            }

            return filteredMatches;
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
    }
}
