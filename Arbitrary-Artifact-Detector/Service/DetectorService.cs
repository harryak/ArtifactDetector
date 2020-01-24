using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Parser;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Timers;

namespace ArbitraryArtifactDetector.Service
{
    /// <summary>
    /// Detector service class that waits for another process to call "StartWatch", then tries to detect
    /// the configured artifact until "StopWatch" is called.
    /// </summary>
    partial class DetectorService : ServiceBase, IDetectorService
    {
        /// <summary>
        /// The current configuration of artifact to be looked for.
        /// </summary>
        private ArtifactConfiguration artifactConfiguration = null;

        /// <summary>
        /// Timer to call detection in interval.
        /// </summary>
        private Timer detectionTimer = null;

        /// <summary>
        /// Host for this service to be callable.
        /// </summary>
        private ServiceHost serviceHost = null;

        /// <summary>
        /// Instantiate service with setup.
        /// </summary>
        /// <param name="setup">Setup of this application.</param>
        public DetectorService(Setup setup)
        {
            InitializeComponent();

            Setup = setup;
            Logger = Setup.GetLogger("DetectorService");
            ArtifactConfigurationParser = new ArtifactConfigurationParser(Setup);
        }

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static ILogger Logger { get; set; }

        /// <summary>
        /// Setup for this run, holding arguments and other necessary objects.
        /// </summary>
        private static Setup Setup { get; set; }

        /// <summary>
        /// Instance of the artifact configuration parser.
        /// </summary>
        private ArtifactConfigurationParser ArtifactConfigurationParser { get; set; }

        /// <summary>
        /// Start watching an artifact in an interval of configured length.
        /// </summary>
        public void StartWatch(string artifactType, string artifactConfigurationString, string referenceImagePath, int intervalLength)
        {
            // Check parameters for validity.
            if (artifactType == "" || artifactConfigurationString == "" || referenceImagePath == "" || intervalLength < 1)
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");

                // Stop execution.
                return;
            }

            try
            {
                DirectoryInfo referenceImageFile = new DirectoryInfo(referenceImagePath);
            }
            catch (Exception exception)
            {
                Logger.LogError("Can not access the supplied reference image path with error: {0}. Not going to execute watch task.", exception.Message);

                // Stop execution.
                return;
            }

            // Determine appropriate artifact detector(s) and runtime information by getting the artifact's configuration from arguments.
            try
            {
                artifactConfiguration = ArtifactConfigurationParser.ParseConfigurationString(artifactConfigurationString);
            }
            catch (IOException exception)
            {
                Logger.LogError("Can not read the artifact's recipe with error: {0}. Not going to execute watch task.", exception.Message);

                // Stop execution.
                return;
            }

            // Start detection loop.
            detectionTimer.Interval = intervalLength;
            detectionTimer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopWatch()
        {
            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            // Set configuration to null to be empty on next run.
            artifactConfiguration = null;
        }

        /// <summary>
        /// Starts this service by opening a service host.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();

            // Setup detection timer to call detection in loop.
            detectionTimer = new Timer(1000);
            detectionTimer.Elapsed += DetectionEventHandler;
        }

        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }

            // Dispose detection timer just in case.
            detectionTimer.Dispose();
        }

        private DetectorResponse Detect(ArtifactConfiguration configuration)
        {
            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = (ArtifactRuntimeInformation) configuration.RuntimeInformation.Clone();
            return configuration.Detector.FindArtifact(ref artifactRuntimeInformation);
        }

        /// <summary>
        /// Method that gets continuously called by timer (in intervals) to detect the artifact.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        private void DetectionEventHandler(object source, ElapsedEventArgs eventArgs)
        {
        }
    }
}