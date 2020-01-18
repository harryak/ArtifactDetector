using ArbitraryArtifactDetector.Detector.Configuration;
using ArbitraryArtifactDetector.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Detector to detect runnign processes.
    ///
    /// Needs:  ProcessName
    /// Yields: WindowHandle
    /// </summary>
    internal class ProcessDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Constructor for this detector, taking the setup and its configuration.
        /// </summary>
        /// <param name="setup">Global setup object for the application.</param>
        /// <param name="configuration">Configuration for this detector instance.</param>
        public ProcessDetector(Setup setup, ProcessDetectorConfiguration configuration) : base(setup)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// This instances configuration.
        /// </summary>
        private ProcessDetectorConfiguration Configuration { get; }

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            Process[] processes;

            StartStopwatch();
            processes = Process.GetProcessesByName(Configuration.ProcessName);

            if (processes.Length < 1)
            {
                StopStopwatch("FindArtifact finished in {0}ms.");
                return new DetectorResponse() { ArtifactPresent = false, Certainty = 100 };
            }

            runtimeInformation.WindowHandle = processes[0].MainWindowHandle;

            StopStopwatch("FindArtifact finished in {0}ms.");

            int certainty = 100 / processes.Length;
            return new DetectorResponse() { ArtifactPresent = true, Certainty = certainty };
        }
    }
}