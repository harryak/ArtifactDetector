using System;
using System.Configuration;

namespace ItsApe.ArtifactDetector
{
    /// <summary>
    /// Class for configuration of the AAD.
    /// </summary>
    internal class ApplicationConfiguration
    {
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

        /// <summary>
        /// The name of the application runnign in the user session.
        /// </summary>
        public static string UserSessionApplicationName
        {
            get
            {
                return Convert.ToString(ConfigurationManager.AppSettings.Get("UserSessionApplicationName"));
            }
        }
    }
}
