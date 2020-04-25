using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Visual feature detector using Orb and a brute-force feature matcher internally.
    /// </summary>
    internal class OrbDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        /// <summary>
        /// Visual feature detector using Orb and a brute-force feature matcher internally.
        /// </summary>
        public OrbDetector(double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
            : base(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger)
        {
            FeatureDetector = new ORBDetector(1000);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }
    }
}
