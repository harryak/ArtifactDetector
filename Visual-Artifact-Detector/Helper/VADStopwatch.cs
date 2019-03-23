/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VisualArtifactDetector.Helper
{
    /// <summary>
    /// A simple extension to System.Diagnostics.Stopwatch:
    /// Singleton for measuring multiple times, storing their elapsed ticks and
    /// outputting the resulting list as CSV strings.
    /// 
    /// This is not thread safe!
    /// </summary>
    public sealed class VADStopwatch : Stopwatch
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static VADStopwatch instance = null;

        // List of the elapsed times.
        private List<TimeSpan> MeasuredTimes;

        /// <summary>
        /// Do not allow direct instantiations.
        /// </summary>
        private VADStopwatch()
        {
            MeasuredTimes = new List<TimeSpan>();
        }

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns>The instance.</returns>
        public static VADStopwatch GetInstance()
        {
            if (instance == null)
            {
                instance = new VADStopwatch();
            }
            return instance;
        }

        /// <summary>
        /// Overrides the parent's Stop just to add the elapsed time to the internal list.
        /// </summary>
        public new void Stop()
        {
            // Stop the Stopwatch first.
            base.Stop();

            // Then add the elapsed time for later processing.
            MeasuredTimes.Add(Elapsed);
        }

        /// <summary>
        /// Converts the measured times (in ms) to a CSV-string (comma-separated), ending with their sum.
        /// </summary>
        /// <returns>The resulting string.</returns>
        public string TimesToCSVinMS()
        {
            string output = "";
            MeasuredTimes.ForEach(TimeSpan => output += TimeSpan.Milliseconds + ",");

            long TotalTime = 0;
            MeasuredTimes.ForEach(TimeSpan => TotalTime += TimeSpan.Milliseconds);

            return output + TotalTime;
        }

        /// <summary>
        /// Converts the measured times (in ns) to a CSV-string (comma-separated), ending with their sum.
        /// </summary>
        /// <returns>The resulting string.</returns>
        public string TimesToCSVinNS()
        {
            string output = "";
            MeasuredTimes.ForEach(TimeSpan => output += (long) (TimeSpan.TotalMilliseconds * 1000000) + ",");

            long TotalTime = 0;
            MeasuredTimes.ForEach(TimeSpan => TotalTime += (long) (TimeSpan.TotalMilliseconds * 1000000));

            return output + TotalTime;
        }
    }
}
