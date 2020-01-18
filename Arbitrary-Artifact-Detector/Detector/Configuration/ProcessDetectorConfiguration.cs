using System.IO;
using System.Linq;

namespace ArbitraryArtifactDetector.Detector.Configuration
{
    /// <summary>
    /// Configuration for the ProcessDetector to detect a running process.
    /// </summary>
    internal class ProcessDetectorConfiguration : BaseDetectorConfiguration, IDetectorConfiguration
    {
        /// <summary>
        /// Constructor for configuration for having a known process name.
        /// </summary>
        /// <param name="processName">Name of the process to detect.</param>
        public ProcessDetectorConfiguration(string processName)
        {
            if (processName == "")
            {
                throw new System.ArgumentException("Process name can not be an empty string.");
            }
            ProcessName = processName;
        }

        /// <summary>
        /// Constructor for configuration for having a known process executable.
        /// </summary>
        /// <param name="processExecutable">Executable of the process to detect.</param>
        public ProcessDetectorConfiguration(FileInfo processExecutable)
        {
            // Take the executable name without suffix as program name.
            string[] parts = processExecutable.Name.Split('.');
            ProcessName = string.Join(".", parts.Where(part => "." + part != processExecutable.Extension));
        }

        /// <summary>
        /// The name of the process.
        /// </summary>
        public string ProcessName { get; }
    }
}