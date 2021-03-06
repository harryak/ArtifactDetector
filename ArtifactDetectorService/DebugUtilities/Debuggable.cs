using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.DebugUtilities
{
    /// <summary>
    /// Parent class to all debuggable classes of the ArtifactDetector.
    /// </summary>
    internal abstract class HasLogger
    {
        /// <summary>
        /// Debuggable classes have a logger.
        /// </summary>
        /// <param name="setup">The current execution's setup.</param>
        internal HasLogger()
        {
            Logger = ApplicationSetup.GetInstance().GetLogger(GetType().Name);
        }

        /// <summary>
        /// Instance of an ILogger to write to.
        /// </summary>
        protected ILogger Logger { get; set; }
    }
}
