using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// A base class to provide common functions for all detectors.
    /// </summary>
    abstract class BaseDetector : IDetector
    {
        /// <summary>
        /// A logger instance.
        /// </summary>
        protected ILogger Logger { get; set; }
        /// <summary>
        /// A stopwatch for evaluation. Can be null.
        /// </summary>
        protected VADStopwatch Stopwatch { get; set; }

        /// <summary>
        /// Provide a logger, please.
        /// </summary>
        /// <param name="logger">Instance to use for logging. Should know the class it is logging for.</param>
        /// <param name="stopwatch">Optional. A VADStopwatch to stop the time for evaluation.</param>
        protected BaseDetector(ILogger logger, VADStopwatch stopwatch = null)
        {
            Logger = logger;
            Stopwatch = stopwatch;
        }

        /// <summary>
        /// Wrapper to start the stopwatch after checking it actually exists.
        /// </summary>
        protected void StartStopwatch()
        {
            if (Stopwatch != null)
            {
                Stopwatch.Restart();
            }
        }

        /// <summary>
        /// Wrapper to stop the stopwatch and log a message.
        /// </summary>
        /// <param name="message">Format string, gets Stopwatch.ElapsedMilliseconds as {0} parameter.</param>
        protected void StopStopwatch(string message)
        {
            if (Stopwatch != null)
            {
                Stopwatch.Stop(new StackFrame(1).GetMethod().Name);
                Logger.LogDebug(message, Stopwatch.ElapsedMilliseconds);
            }
        }

        abstract public DetectorResponse FindArtifact(Setup setup);
    }
}
