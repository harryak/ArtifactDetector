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
        }

        /// <summary>
        /// Instance of an ILogger to write to.
        /// </summary>
        protected ILogger Logger { get; set; }
    }
}
