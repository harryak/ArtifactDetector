using ArbitraryArtifactDetector.Utility;
using System.IO;

namespace ArbitraryArtifactDetector.Detector.Configuration
{
    /// <summary>
    /// Configuration for the MailDetector to detect an opened mail.
    /// </summary>
    internal class MailDetectorConfiguration : BaseDetectorConfiguration, IDetectorConfiguration
    {
        /// <summary>
        /// Constructor: Expects a sender and a subject for the opened mail to detect.
        /// </summary>
        /// <param name="sender">Non-empty string containing the sender or parts of it.</param>
        /// <param name="subject">Optional: String containing the subject or parts of it.</param>
        /// <param name="executable">Optional: String containing the path to the mail client executable.</param>
        public MailDetectorConfiguration(string sender, string subject = "", string executable = "")
        {
            // Make sure the sender is set as detection without is impossible and it is always set.
            if (sender == "")
            {
                throw new System.ArgumentException("Sender can not be empty.");
            }

            Sender = sender;
            Subject = subject;

            if (executable == "")
            {
                Executable = DefaultApplicationFinder.GetDefaultMailReaderExecutable();
            }
            else
            {
                Executable = new FileInfo(executable);
            }
        }

        /// <summary>
        /// Path to the executable of the mail client.
        /// </summary>
        public FileInfo Executable { get; set; }

        /// <summary>
        /// Non-empty string containing the sender or parts of it.
        /// </summary>
        public string Sender { get; }

        /// <summary>
        /// Possibly empty string containing the subject or parts of it.
        /// </summary>
        public string Subject { get; }
    }
}