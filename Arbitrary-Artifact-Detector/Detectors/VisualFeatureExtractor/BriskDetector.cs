using Emgu.CV.Features2D;
using Emgu.CV.Flann;

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
        public BriskDetector()
        {
            FeatureDetector = new Brisk();

            var indexParams = new LshIndexParams(6, 12, 1);
            var searchParams = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
        }
    }
}
