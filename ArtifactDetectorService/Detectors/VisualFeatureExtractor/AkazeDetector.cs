using Emgu.CV.Features2D;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Visual feature detector using AKAZE and a brute-force feature matcher internally.
    /// </summary>
    internal class AkazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        /// <summary>
        /// Visual feature detector using AKAZE and a brute-force feature matcher internally.
        /// </summary>
        public AkazeDetector()
        {
            FeatureDetector = new AKAZE();
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
