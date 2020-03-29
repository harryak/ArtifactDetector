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
            Stopwatch = ApplicationSetup.GetInstance().Stopwatch;
        }

        /// <summary>
        /// Instance of an ILogger to write to.
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Instance of an AADStopwatch to stop the time with.
        /// </summary>
        protected DetectorStopwatch Stopwatch { get; set; }

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
