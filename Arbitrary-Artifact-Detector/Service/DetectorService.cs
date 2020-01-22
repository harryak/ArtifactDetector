using ArbitraryArtifactDetector.Detector;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Parser;
using Microsoft.Extensions.Logging;
using System.IO;
using System.ServiceProcess;

namespace ArbitraryArtifactDetector.Service
{
    partial class DetectorService : ServiceBase, IDetectorService
    {
        public DetectorService(Setup setup)
        {
            InitializeComponent();

            Setup = setup;
            Logger = setup.GetLogger("DetectorService");
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
        /// Start watching an artifact.
        /// </summary>
        public void StartWatch(string artifactConfigurationString)
        {
            // TODO: Setup watch task.
            // Determine appropriate artifact detector(s) by getting the artifact's recipe.
            ArtifactRuntimeInformation artifactConfiguration;
            try
            {
                artifactConfiguration = ArtifactConfigurationParser.ParseConfigurationString(artifactConfigurationString);
            }
            catch (IOException exception)
            {
                Logger.LogError("Can not read the artifact's recipe with error: {0}", exception.Message);
            }

            // TODO: Call detect in loop.
        }

        public void StopWatch()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnStart(string[] args)
        {
            ArtifactConfigurationParser = new ArtifactConfigurationParser(Setup);
            // TODO: Add code here to start your service.
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        private DetectorResponse Detect(IDetector detector)
        {
            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = new ArtifactRuntimeInformation();
            return detector.FindArtifact(ref artifactRuntimeInformation);
        }
    }
}