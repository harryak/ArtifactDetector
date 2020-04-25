using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Visual feature detector using Brisk and a Flann based feature matcher internally.
    /// </summary>
    internal class BriskDetector : BaseVisualFeatureExtractor, IVisualFeatureExtractor
    {
        /// <summary>
        /// Visual feature detector using Brisk and a Flann based feature matcher internally.
        /// </summary>
        public BriskDetector(double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
            : base(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger)
        {
            FeatureDetector = new Brisk();

            var indexParams = new LshIndexParams(6, 12, 1);
            var searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
        }
    }
}
