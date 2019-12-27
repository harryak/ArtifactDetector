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
                //ArtifactDetector = visualDetectorSelectionMap[DetectorSelection](GetLogger(DetectorSelection + "ArtifactDetector"), Stopwatch);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the artifact library from a file.
        /// </summary>
        /// <param name="logger"></param>
        private static void FetchArtifactLibrary()
        {
            // Get the artifact library from a file.
            if (File.Exists(Setup.WorkingDirectory + Setup.LibraryFileName))
            {
                try
                {
                    //Setup.ArtifactLibrary = ArtifactLibrary.FromFile(Setup.WorkingDirectory + Setup.LibraryFileName, ArtifactDetector, Setup.Stopwatch, Setup.LoggerFactory);
                    Logger.LogDebug("Loaded artifact library from file {0}.", Setup.WorkingDirectory + Setup.LibraryFileName);
                }
                catch (SerializationException)
                {
                    Logger.LogWarning("Deserialization of artifact library failed.");
                }
            }
            else
            {
                Logger.LogDebug("Artifact library file not found at {0}.", Setup.WorkingDirectory + Setup.LibraryFileName);
            }
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

            // Should we use a cache for the artifact library?
            if (Setup.ShouldCache)
            {
                FetchArtifactLibrary();
            }

            // Some error occurred, get an empty library.
            if (Setup.ArtifactLibrary == null)
            {
                Logger.LogDebug("Creating new artifact library instance.");
                try
                {
                    //Setup.ArtifactLibrary = new ArtifactLibrary(Setup.WorkingDirectory, ArtifactDetector, GetLogger("ArtifactLibrary"), Stopwatch);
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not instantiate artifact library: {0}", e.Message);
                    throw new SetupError("Could not instantiate artifact library.");
                }
            }

            //var artifactFound = ArtifactDetector.FindArtifact(Setup);

            //return artifactFound.ArtifactPresent ? 0 : 1;
            return 1;
        }
    }
}
