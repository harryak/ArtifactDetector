using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.DebugUtilities
{
    internal class Debuggable
    {
        /// <summary>
        /// Debuggable classes have a logger and a stopwatch.
        /// </summary>
        /// <param name="setup">The current execution's setup.</param>
        internal Debuggable()
        {
            Logger = Setup.GetInstance().GetLogger(GetType().Name);
            Stopwatch = Setup.GetInstance().Stopwatch;
        }

        protected ILogger Logger { get; set; }
        protected AADStopwatch Stopwatch { get; set; }

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
                Stopwatch.Stop(GetType().Name);
                Logger.LogDebug(message, Stopwatch.ElapsedMilliseconds);
            }
        }
    }
}