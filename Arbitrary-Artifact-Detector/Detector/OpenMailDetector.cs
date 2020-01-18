using ArbitraryArtifactDetector.Detector.Configuration;
using ArbitraryArtifactDetector.DetectorCondition.Model;
using ArbitraryArtifactDetector.Model;
using Microsoft.Extensions.Logging;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Detector to detect if an email is opened.
    ///
    /// Needs:  Sender, (subject), (mailClientExecutable)
    /// Yields: WindowHandle
    /// </summary>
    internal class MailDetector : CompoundDetector, ICompoundDetector
    {
        /// <summary>
        /// Constructor for this detector, taking the setup and its configuration.
        /// </summary>
        /// <param name="setup">Global setup object for the application.</param>
        /// <param name="configuration">Configuration for this detector instance.</param>
        public MailDetector(Setup setup, MailDetectorConfiguration configuration) : base(setup)
        {
            Logger.Log(LogLevel.Debug, "Setting up MailDetector now.");
            Configuration = configuration;

            var processDetector = new ProcessDetector(setup, new ProcessDetectorConfiguration(configuration.Executable));
            AddDetector(processDetector);

            var openWindowDetector = new OpenWindowDetector(setup);
            AddDetector(openWindowDetector);
        }

        /// <summary>
        /// This instances configuration.
        /// </summary>
        private MailDetectorConfiguration Configuration { get; }
    }
}