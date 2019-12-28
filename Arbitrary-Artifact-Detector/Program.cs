using ArbitraryArtifactDetector.Detector;
using ArbitraryArtifactDetector.Detector.VisualDetector;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ArbitraryArtifactDetector
{
    class Program
    {
        /// <summary>
        /// Map for selecting a visual artifact detector by its name.
        /// </summary>
        private static readonly Dictionary<string, Func<ILogger, VADStopwatch, IVisualDetector>> visualDetectorSelectionMap =
            new Dictionary<string, Func<ILogger, VADStopwatch, IVisualDetector>>(){
                { "akaze", (ILogger logger, VADStopwatch stopwatch) => { return new AkazeDetector(logger, stopwatch); } },
                { "brisk", (ILogger logger, VADStopwatch stopwatch) => { return new BriskDetector(logger, stopwatch); } },
                { "kaze", (ILogger logger, VADStopwatch stopwatch) => { return new KazeDetector(logger, stopwatch); } },
                { "orb", (ILogger logger, VADStopwatch stopwatch) => { return new OrbDetector(logger, stopwatch); } }
            };

        /// <summary>
        /// Setup for this run, holding arguments and other necessary objects.
        /// </summary>
        internal static Setup Setup { get; set; }

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static ILogger Logger { get; set; }
        public static IDetector ArtifactDetector { get; private set; }

        /// <summary>
        /// Try to resolve the artifact artifactDetector by a given selection.
        /// </summary>
        /// <returns></returns>
        private static bool SetupArtifactDetector()
        {
            Type detectorType = typeof(IDetector);
            string Namespace = detectorType.Namespace;

            if (visualDetectorSelectionMap.ContainsKey(Setup.DetectorSelection))
            {
                Logger.LogInformation("Using artifactDetector {detectorSelection}.", Setup.DetectorSelection);
                ArtifactDetector = visualDetectorSelectionMap[Setup.DetectorSelection](Setup.GetLogger(Setup.DetectorSelection + "ArtifactDetector"), Setup.Stopwatch);
                return true;
            }

            return false;
        }

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

            Logger = Setup.GetLogger("Main");

            // Launch the actual program.
            Logger.LogDebug("Call the actual comparison.");

            // Which artifact detector should we use?
            if (!SetupArtifactDetector())
            {
                throw new SetupError("Could not set up artifact detector.");
            }

            var artifactFound = ArtifactDetector.FindArtifact(Setup);

            return artifactFound.ArtifactPresent ? 0 : 1;
        }
    }
}
