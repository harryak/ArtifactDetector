using Emgu.CV.Features2D;
using Emgu.CV.Flann;

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
        public KazeDetector()
        {
            FeatureDetector = new KAZE();

            var ip = new LinearIndexParams();
            var sp = new SearchParams();
            DescriptorMatcher = new FlannBasedMatcher(ip, sp);
        }
    }
}
