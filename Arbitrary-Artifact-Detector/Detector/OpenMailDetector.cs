using ArbitraryArtifactDetector.DetectorCondition.Model;
using ArbitraryArtifactDetector.Model;

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
        public MailDetector(Setup setup) : base(setup)
        {
            var processDetector = new RunningProcessDetector(setup);
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(processDetector);

            var openWindowDetector = new OpenWindowDetector(setup);
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(openWindowDetector);
        }
    }
}