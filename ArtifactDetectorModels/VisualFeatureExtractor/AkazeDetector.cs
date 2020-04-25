using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

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
        public AkazeDetector(double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
            : base(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger)
        {
            FeatureDetector = new AKAZE();
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
