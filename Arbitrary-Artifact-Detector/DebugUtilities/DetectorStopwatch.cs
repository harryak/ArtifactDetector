using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ItsApe.ArtifactDetector.DebugUtilities
{
    /// <summary>
    /// A simple extension to System.Diagnostics.Stopwatch:
    /// Singleton for measuring multiple times, storing their elapsed ticks and
    /// outputting the resulting list as CSV strings.
    ///
    /// This is not thread safe!
    /// </summary>
    internal sealed class DetectorStopwatch : Stopwatch
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        private static DetectorStopwatch instance = null;

        private int ExtraValue = -1;

        private List<string> Labels;

        // List of the elapsed times.
        private List<TimeSpan> MeasuredTimes;

        /// <summary>
        /// Do not allow direct instantiations.
        /// </summary>
        private DetectorStopwatch()
        {
            MeasuredTimes = new List<TimeSpan>();
            Labels = new List<string>();
        }

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns>The instance.</returns>
        public static DetectorStopwatch GetInstance()
        {
            if (instance == null)
            {
                instance = new DetectorStopwatch();
            }
            return instance;
        }

        /// <summary>
        /// Return all labels as a CSV string.
        /// </summary>
        /// <returns>CSV-string.</returns>
        public string LabelsToCSV()
        {
            string output = "";
            Labels.ForEach(Label => output += Label + ";");

            return output + "total";
        }

        /// <summary>
        /// Overrides the parent's Stop just to add the elapsed time to the internal list.
        /// </summary>
        public void Stop(string label, int extraValue = -1)
        {
            // Stop the Stopwatch first.
            Stop();

            Labels.Add(label);

            if (extraValue > 0)
            {
                Labels.Add("extra_value");
                ExtraValue = extraValue;
            }

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
        public string TimesToCSVinNSprecision()
        {
            string output = "";
            MeasuredTimes.ForEach(TimeSpan => output += TimeSpan.TotalMilliseconds + ";");

            double TotalTime = 0;
            MeasuredTimes.ForEach(TimeSpan => TotalTime += TimeSpan.TotalMilliseconds);

            output += TotalTime;

            if (ExtraValue > 0) output += ";" + ExtraValue;

            return output;
        }
    }
}
