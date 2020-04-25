using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Visual feature detector using Kaze and a Flann based feature matcher internally.
    /// </summary>
    internal class KazeDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        /// <summary>
        /// Visual feature detector using Kaze and a Flann based feature matcher internally.
        /// </summary>
        public KazeDetector(double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
            : base(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger)
        {
            FeatureDetector = new KAZE();

            var ip = new LinearIndexParams();
            var sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}
