using ArbitraryArtifactDetector.Detector.Configuration;
using ArbitraryArtifactDetector.DetectorCondition.Model;
using ArbitraryArtifactDetector.Model;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

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

            PersistentRuntimeInformation = new ArtifactRuntimeInformation();
            // TODO: Take names from config/job.
            PersistentRuntimeInformation.PossibleProcessNames.Add(GetProcessName(configuration.Executable));

            var processDetector = new RunningProcessDetector(setup);
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(processDetector);

            var openWindowDetector = new OpenWindowDetector(setup);
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(openWindowDetector);
        }

        /// <summary>
        /// This instances configuration.
        /// </summary>
        private MailDetectorConfiguration Configuration { get; }

        private string GetProcessName(FileInfo processExecutable)
        {
            // Take the executable name without suffix as program name.
            string[] parts = processExecutable.Name.Split('.');
            return string.Join(".", parts.Where(part => "." + part != processExecutable.Extension));
        }
    }
}