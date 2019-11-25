/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

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
    }
}
