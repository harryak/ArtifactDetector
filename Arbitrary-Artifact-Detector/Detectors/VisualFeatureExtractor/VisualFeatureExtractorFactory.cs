using System;
using System.Collections.Generic;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    internal class VisualFeatureExtractorFactory
    {
        /// <summary>
        /// Map for selecting a feature extractor by its name.
        /// </summary>
        private static readonly Dictionary<string, Func<IVisualFeatureExtractor>> visualFeatureExtractorSelectionMap =
            new Dictionary<string, Func<IVisualFeatureExtractor>>(){
                { "akaze", () => { return new AkazeDetector(); } },
                { "brisk", () => { return new BriskDetector(); } },
                { "kaze",  () => { return new KazeDetector(); } },
                { "orb",   () => { return new OrbDetector(); } }
            };

        /// <summary>
        /// Set instance of visual feature extractor to value in configuration file.
        /// </summary>
        /// <param name="setup">The setup of this application.</param>
        /// <returns>True on success.</returns>
        /// <exception cref="ArgumentException">If the visual feature extractor configuration is invalid.</exception>
        public static IVisualFeatureExtractor GetFeatureExtractor()
        {
            // If the value in the configuration is invalid throw error.
            if (!visualFeatureExtractorSelectionMap.ContainsKey(AADConfig.FeatureExtractorSelection))
            {
                throw new ArgumentException("Could not instantiate feature extractor, wrong name given.");
            }

            return visualFeatureExtractorSelectionMap[AADConfig.FeatureExtractorSelection]();
        }
    }
}