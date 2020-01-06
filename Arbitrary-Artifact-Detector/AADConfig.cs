using System;
using System.Configuration;

namespace ArbitraryArtifactDetector
{
    class AADConfig
    {
        public static int MinimumMatchesRequired
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings.Get("MinimumMatchesRequired"));
            }
        }
        public static double MatchUniquenessThreshold
        {
            get
            {
                return Convert.ToDouble(ConfigurationManager.AppSettings.Get("MatchUniquenessThreshold"));
            }
        }
        public static double MatchDistanceThreshold
        {
            get
            {
                return Convert.ToDouble(ConfigurationManager.AppSettings.Get("MatchDistanceThreshold"));
            }
        }
        public static string FeatureExtractorSelection
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("FeatureExtractor");
            }
        }
        public static string MatchFilterSelection
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("MatchFilter");
            }
        }
    }
}
