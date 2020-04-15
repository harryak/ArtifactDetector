using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.DebugUtilities
{
    /// <summary>
    /// Parent class to all debuggable classes of the ArtifactDetector.
    /// </summary>
    internal abstract class Debuggable
    {
        /// <summary>
        /// Debuggable classes have a logger and a stopwatch.
        /// </summary>
        /// <param name="setup">The current execution's setup.</param>
        internal Debuggable()
        {
            Logger = ApplicationSetup.GetInstance().GetLogger(GetType().Name);
            Stopwatches = new Stack<DetectorStopwatch>();
        }

        /// <summary>
        /// Instance of an ILogger to write to.
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Instance of an AADStopwatch to stop the time with.
        /// </summary>
        protected Stack<DetectorStopwatch> Stopwatches { get; set; }

        /// <summary>
        /// Wrapper to start a new stopwatch.
        /// Note: You are responsible for nesting stopwatches correctly!
        ///       Stop the inner stopwatch first, always.
        /// </summary>
        protected void StartStopwatch()
        {
            var stopwatch = new DetectorStopwatch();
            stopwatch.Start();
            Stopwatches.Push(stopwatch);
        }

        /// <summary>
        /// Wrapper to stop the newest stopwatch and log its time.
        /// </summary>
        /// <param name="message">Format string, gets Stopwatch.ElapsedMilliseconds as {0} parameter.</param>
        protected void StopStopwatch(string message)
        {
            if (Stopwatches.Count > 0)
            {
                var stopwatch = Stopwatches.Pop();
                stopwatch.Stop(GetType().Name);
                Logger.LogDebug(message, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
