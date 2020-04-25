using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Factory class to get the currently configured feature matcher.
    /// </summary>
    public class VisualFeatureExtractorFactory
    {
        /// <summary>
        /// Map for selecting a feature extractor by its name.
        /// </summary>
        private static readonly Dictionary<string, Func<double, double, int, string, ILogger, IVisualFeatureExtractor>> visualFeatureExtractorSelectionMap =
            new Dictionary<string, Func<double, double, int, string, ILogger, IVisualFeatureExtractor>>(){
                { "akaze", (double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
                    => { return new AkazeDetector(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger); } },
                { "brisk", (double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
                    => { return new BriskDetector(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger); } },
                { "kaze",  (double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
                    => { return new KazeDetector(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger); } },
                { "orb",   (double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
                    => { return new OrbDetector(matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger); } }
            };

        /// <summary>
        /// Set instance of visual feature extractor to value in configuration file.
        /// </summary>
        /// <param name="setup">The setup of this application.</param>
        /// <returns>True on success.</returns>
        /// <exception cref="ArgumentException">If the visual feature extractor configuration is invalid.</exception>
        public static IVisualFeatureExtractor GetFeatureExtractor(string featureExtractorSelection, double matchDistanceThreshold, double matchUniquenessThreshold, int minimumMatchesRequired, string matchFilterSelection, ILogger logger)
        {
            // If the value in the configuration is invalid throw error.
            if (!visualFeatureExtractorSelectionMap.ContainsKey(featureExtractorSelection))
            {
                throw new ArgumentException("Could not instantiate feature extractor, wrong name given.");
            }

            return visualFeatureExtractorSelectionMap[featureExtractorSelection](matchDistanceThreshold, matchUniquenessThreshold, minimumMatchesRequired, matchFilterSelection, logger);
        }
    }
}
