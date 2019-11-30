/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.VisualDetector;
using ArbitraryArtifactDetector.VisualMatchFilter;
using Microsoft.Extensions.Logging;
using Mono.Options;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ArbitraryArtifactDetector
{
    [Serializable]
    internal class Setup
    {
        /// <summary>
        /// Map for selecting a visual artifact detector by its name.
        /// </summary>
        private readonly Dictionary<string, Func<ILogger, VADStopwatch, IVisualArtifactDetector>> detectorSelectionMap =
            new Dictionary<string, Func<ILogger, VADStopwatch, IVisualArtifactDetector>>(){
                { "akaze", (ILogger logger, VADStopwatch stopwatch) => { return new AkazeArtifactDetector(logger, stopwatch); } },
                { "brisk", (ILogger logger, VADStopwatch stopwatch) => { return new BriskArtifactDetector(logger, stopwatch); } },
                { "kaze", (ILogger logger, VADStopwatch stopwatch) => { return new KazeArtifactDetector(logger, stopwatch); } },
                { "orb", (ILogger logger, VADStopwatch stopwatch) => { return new OrbArtifactDetector(logger, stopwatch); } }
            };

        /// <summary>
        /// Map for selecting a feature match filter by its name.
        /// </summary>
        private readonly Dictionary<string, Func<ILogger, VADStopwatch, IMatchFilter>> filterSelectionMap =
            new Dictionary<string, Func<ILogger, VADStopwatch, IMatchFilter>>()
            {
                { "simple", (ILogger logger, VADStopwatch stopwatch) => { return new SimpleMatchFilter(logger, stopwatch); } },
                { "affine", (ILogger logger, VADStopwatch stopwatch) => { return new AffineMatchFilter(logger, stopwatch); } },
            };

        private bool shouldShowHelp = false;

        /// <summary>
        /// 
        /// </summary>
        private ILogger Logger { get; set; }
        public VADStopwatch Stopwatch { get; set; } = null;

        public ArtifactLibrary ArtifactLibrary { get; set; } = null;
        public string ArtifactGoal { get; set; } = "";
        public string ScreenshotPath { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public IVisualArtifactDetector ArtifactDetector { get; set; } = null;
        public IMatchFilter MatchFilter { get; set; } = null;
        public bool ShouldEvaluate { get; set; } = false;
        public bool ShouldCache { get; set; } = false;
        public string LibraryFileName { get; } = "artifacts.bin";

        private ILoggerFactory LoggerFactory { get; set; }
        public string DetectorSelection { get; set; } = "orb";
        public string FilterSelection { get; set; } = "simple";

        /// <summary>
        /// Setup the loggerFactory.
        /// </summary>
        /// <returns></returns>
        private void SetupLogging()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new NLogLoggerProvider());
        }

        public ILogger GetLogger(string categoryName)
        {
            if (LoggerFactory == null) { SetupLogging(); }
            return LoggerFactory.CreateLogger(categoryName);
        }

        /// <summary>
        /// Prepares an option set for the command line.
        /// </summary>
        /// <returns></returns>
        private OptionSet SetupCmdOptions()
        {
            // Set default value to current directory for working directory.
            WorkingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath("."));

            return new OptionSet
            {
                { "h|help", "Show this message and exit.", h => shouldShowHelp = h != null },
                { "s|screenshot=", "The path to the screenshot to search in (required).", p => ScreenshotPath = p },
                { "a|artifact=", "Name of the artifact to look for (required).", a => ArtifactGoal = a },
                { "f|filepath=", "Path to the working directory (default is current directory). The recipes must be in this folder!", f => WorkingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath(f)) },
                { "c|cache", "Cache the artifact types.", c => ShouldCache = c != null },
                { "d|detector=", "Detector to use (default: orb). [akaze, brisk, kaze, orb]", d => DetectorSelection = d},
                { "m|filter=", "Match filter to use (default: simple). [simple, affine]", m => FilterSelection = m},
                { "e|evaluate", "Include stopwatch output.", e => ShouldEvaluate = e != null },
            };
        }

        /// <summary>
        /// Prints the command line options/help.
        /// </summary>
        /// <param name="options"></param>
        private void ShowHelp(OptionSet options)
        {
            // show some app description message
            Console.WriteLine("Usage: ArbitraryArtifact-Detector.exe [OPTIONS]+");
            Console.WriteLine("Takes the supplied screenshot and looks in it for the artifact specified.");
            Console.WriteLine();

            // output the options
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        /// <summary>
        /// Test if a directory is writable by attempting to write and delete a lock file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private bool TestLockFile(string path)
        {
            try
            {
                using (var lockFile = File.OpenWrite(path + "lock"))
                {
                    ;
                }

                File.Delete(path + "lock");
            }
            catch (AccessViolationException e)
            {
                Logger.LogError("Can not write to working directory {path} with error: {0}.", path, e.Message);
                return false;
            }
            catch (IOException e)
            {
                Logger.LogError("Can not write to working directory {path} with error: {0}.", path, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Try to resolve the artifact artifactDetector by a given selection.
        /// </summary>
        /// <returns></returns>
        private bool SetupArtifactDetector()
        {
            if (detectorSelectionMap.ContainsKey(DetectorSelection))
            {
                Logger.LogInformation("Using artifactDetector {detectorSelection}.", DetectorSelection);
                ArtifactDetector = detectorSelectionMap[DetectorSelection](GetLogger(DetectorSelection + "ArtifactDetector"), Stopwatch);
                return true;
            }

            return false;
        }

        private bool SetupMatchFilter()
        {
            if (filterSelectionMap.ContainsKey(FilterSelection))
            {
                Logger.LogInformation("Using match filter {filterSelection}.", FilterSelection);
                MatchFilter = filterSelectionMap[FilterSelection](GetLogger(FilterSelection + "MatchFilter"), Stopwatch);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the artifact library from a file.
        /// </summary>
        /// <param name="logger"></param>
        private void FetchArtifactLibrary()
        {
            // Get the artifact library from a file.
            if (File.Exists(WorkingDirectory + LibraryFileName))
            {
                try
                {
                    ArtifactLibrary = ArtifactLibrary.FromFile(WorkingDirectory + LibraryFileName, ArtifactDetector, Stopwatch, LoggerFactory);
                    Logger.LogDebug("Loaded artifact library from file {0}.", WorkingDirectory + LibraryFileName);
                }
                catch (SerializationException)
                {
                    Logger.LogWarning("Deserialization of artifact library failed.");
                }
            }
            else
            {
                Logger.LogDebug("Artifact library file not found at {0}.", WorkingDirectory + LibraryFileName);
            }
        }

        public Setup(string[] commandLineArguments)
        {
            Logger = GetLogger("Setup");

            // Parse the command line.
            Logger.LogDebug("Parsing the command line.");
            var options = SetupCmdOptions();
            try
            {
                options.Parse(commandLineArguments);
            }
            catch (OptionException e)
            {
                Logger.LogError("Error ocurred while parsing the options: {0} with option {1}.", e.Message, e.OptionName);
                shouldShowHelp = true;
            }

            // Should we show the help?
            if (shouldShowHelp || string.IsNullOrEmpty(ScreenshotPath) || string.IsNullOrEmpty(ArtifactGoal))
            {
                ShowHelp(options);
                throw new SetupError("Screenshot path or artifact goal not set.");
            }
            // We got the info.
            Logger.LogInformation("We got the path {screenshotPath} and the artifact goal {artifactGoal}.", ScreenshotPath, ArtifactGoal);

            // Is there an existing and accessable working directory?
            if (Directory.Exists(WorkingDirectory))
            {
                TestLockFile(WorkingDirectory);
            }

            // Which artifact detector should we use?
            if (!SetupArtifactDetector())
            {
                ShowHelp(options);
                throw new SetupError("Could not set up artifact detector.");
            }

            // Which match filter should we use?
            if (!SetupMatchFilter())
            {
                ShowHelp(options);
                throw new SetupError("Could not set up match filter.");
            }

            // Determine if we use a stopwatch in this run.
            if (ShouldEvaluate)
            {
                // Get stopwatch for evaluation.
                Stopwatch = VADStopwatch.GetInstance();
            }

            // Should we use a cache for the artifact library?
            if (ShouldCache)
            {
                FetchArtifactLibrary();
            }

            // Some error occurred, get an empty library.
            if (ArtifactLibrary == null)
            {
                Logger.LogDebug("Creating new artifact library instance.");
                try
                {
                    ArtifactLibrary = new ArtifactLibrary(WorkingDirectory, ArtifactDetector, GetLogger("ArtifactLibrary"));
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not instantiate artifact library: {0}", e.Message);
                    throw new SetupError("Could not instantiate artifact library.");
                }
            }
        }
    }
}
