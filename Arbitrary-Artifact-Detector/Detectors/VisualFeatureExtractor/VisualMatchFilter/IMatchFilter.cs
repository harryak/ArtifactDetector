using Emgu.CV;
using Emgu.CV.Util;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor.VisualMatchFilter
{
    /// <summary>
    /// Interface of all match filters using RanSaC for abstraction from different implementations.
    /// </summary>
    interface IMatchFilter
    {
        /// <summary>
        /// Tries to get a transformation matrix via RanSaC from modelKeyPoints to queryKeyPoints matching the inlierRatio while allowing errors of patchSize.
        /// </summary>
        /// <param name="modelKeyPoints">Starting set of key points.</param>
        /// <param name="queryKeyPoints">Goal set of key points.</param>
        /// <param name="matches">Previously found matches between modelKeyPoints and queryKeyPoints masked by mask.</param>
        /// <param name="mask">Mask for previously found matches.</param>
        /// <param name="iterations">RanSaC maximum iterations.</param>
        /// <param name="inlierRatio">How many previously found matches should support the hypothesis.</param>
        /// <param name="patchSize">Error threshold for applying the hypothesis on the starting set to get to the goal set.</param>
        /// <returns>A transformation matrix from model to query or null.</returns>
        Matrix<float> GetRanSaCTransformationMatrix(VectorOfKeyPoint modelKeyPoints, VectorOfKeyPoint queryKeyPoints, VectorOfVectorOfDMatch matches, ref Mat mask, int iterations, double inlierRatio, int patchSize);
    }
}
