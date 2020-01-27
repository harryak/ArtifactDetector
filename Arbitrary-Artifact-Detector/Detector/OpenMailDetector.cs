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
        public MailDetector()
        {
            var processDetector = new RunningProcessDetector();
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(processDetector);

            var openWindowDetector = new OpenWindowDetector();
            processDetector.SetTargetConditions(new EqualityDetectorCondition<DetectorResponse>("ArtifactPresent", true));
            AddDetector(openWindowDetector);
        }
    }
}