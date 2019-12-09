using ArbitraryArtifactDetector.EnvironmentalDetector;
using Microsoft.Extensions.Logging;
using System;

namespace ArbitraryArtifactDetector
{
    class Program
    {
        internal static Setup Setup { get; set; }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Setup = new Setup(args);
            }
            catch (SetupError)
            {
                return -1;
            }

            var logger = Setup.GetLogger("Main");

            var envDetector = new OpenWindowDetector(Setup.GetLogger("OpenWindowDetector"), Setup.Stopwatch);

            var openWindows = envDetector.GetOpenedWindows();

            if (openWindows.Count > 0)
            {
                foreach(var openWindow in openWindows)
                {
                    logger.LogDebug("Have open window {windowName}.", openWindow.ToString());
                }
                return 1;
            }
            // Unreachable for now.

            // Launch the actual program.
            logger.LogDebug("Call the actual comparison.");

            var artifactFound = Setup.ArtifactDetector.FindArtifact(Setup);

            return artifactFound.ArtifactFound ? 0 : 1;
        }
    }
}
