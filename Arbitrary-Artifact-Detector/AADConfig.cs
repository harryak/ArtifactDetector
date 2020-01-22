using System;
using System.Configuration;
using System.IO;

namespace ArbitraryArtifactDetector
{
    /// <summary>
    /// Class for configuration of the AAD.
    /// </summary>
    internal class AADConfig
    {
        /// <summary>
        /// Flag to tell whether the processed images should be cached for the next run.
        /// </summary>
        public static bool Cache
        {
            get
            {
                return Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Cache"));
            }
        }

        /// <summary>
        /// Flag to tell whether this run should be evaluated.
        /// </summary>
        public static bool Evaluate
        {
            get
            {
                return Convert.ToBoolean(ConfigurationManager.AppSettings.Get("Evaluate"));
            }
        }

        /// <summary>
        /// Name of the feature extractor to use.
        /// </summary>
        public static string FeatureExtractorSelection
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("FeatureExtractor");
            }
        }

        /// <summary>
        /// Feature extraction setting: Threshold to sort out possible matches by their distance.
        /// </summary>
        public static double MatchDistanceThreshold
        {
            get
            {
                return Convert.ToDouble(ConfigurationManager.AppSettings.Get("MatchDistanceThreshold"));
            }
        }

        /// <summary>
        /// Feature extraction setting: Name of the match filter to use.
        /// </summary>
        public static string MatchFilterSelection
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("MatchFilter");
            }
        }
        
        /// <summary>
        /// Feature extraction setting: Uniqueness threshold to sort out possible matches.
        /// </summary>
        public static double MatchUniquenessThreshold
        {
            get
            {
                return Convert.ToDouble(ConfigurationManager.AppSettings.Get("MatchUniquenessThreshold"));
            }
        }

        /// <summary>
        /// Feature extraction setting: Absolute count of matches needed for a match.
        /// </summary>
        public static int MinimumMatchesRequired
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("MinimumMatchesRequired"));
            }
        }
    }
}