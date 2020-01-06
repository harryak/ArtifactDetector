using ArbitraryArtifactDetector.DetectorCondition.Model;
using ArbitraryArtifactDetector.Detectors;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.RecipeParser;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ArbitraryArtifactDetector
{
    class Program
    {
        /// <summary>
        /// Setup for this run, holding arguments and other necessary objects.
        /// </summary>
        internal static Setup Setup { get; set; }

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static ILogger Logger { get; set; }

        [STAThread]
        static int Main(string[] args)
        {
            // 1. Initalize program setup with command line arguments.
            try
            {
                Setup = new Setup(args);
            }
            catch (SetupError)
            {
                return -1;
            }

            Logger = Setup.GetLogger("Main");

            // 2. Determine appropriate artifact detector(s) by getting the artifact's recipe.
            ArtifactConfiguration artifactConfiguration;
            try
            {
                artifactConfiguration = new ArtifactConfigurationParser().ParseRecipe(Setup.WorkingDirectory + Setup.ArtifactGoal);
            }
            catch (IOException exception)
            {
                Logger.LogError("Can not read the artifact's recipe with error: {0}", exception.Message);
                return -1;
            }

            // 3. Get artifact detector (may be a compound detector) from artifact configuration.
            IDetector artifactDetector;
            try
            {
                artifactDetector = null;// SetupArtifactDetector(artifactConfiguration);
            }
            catch (SetupError error)
            {
                Logger.LogError(error.Message);
                return -1;
            }
            
            DetectorResponse artifactFound = artifactDetector.FindArtifact(null, Setup);

            return artifactFound.ArtifactPresent ? 0 : 1;
        }
    }
}
