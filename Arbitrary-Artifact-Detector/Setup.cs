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
        /// Map for selecting an artifact artifactDetector by its name.
        /// </summary>
        private readonly Dictionary<string, Func<ILoggerFactory, IArtifactDetector>> detectorSelectionMap =
            new Dictionary<string, Func<ILoggerFactory, IArtifactDetector>>(){
                { "akaze", (ILoggerFactory loggerFactory) => { return new AkazeArtifactDetector(loggerFactory); } },
                { "brisk", (ILoggerFactory loggerFactory) => { return new BriskArtifactDetector(loggerFactory); } },
                { "kaze", (ILoggerFactory loggerFactory) => { return new KazeArtifactDetector(loggerFactory); } },
                { "orb", (ILoggerFactory loggerFactory) => { return new OrbArtifactDetector(loggerFactory); } }
            };

        private readonly Dictionary<string, Func<IMatchFilter>> filterSelectionMap =
            new Dictionary<string, Func<IMatchFilter>>()
            {
                { "simple", () => { return new SimpleMatchFilter(); } },
                { "affine", () => { return new AffineMatchFilter(); } },
            };

        private bool shouldShowHelp = false;

        public ILogger Logger { get; set; }
        public VADStopwatch Stopwatch { get; set; } = null;
        public ArtifactLibrary ArtifactLibrary { get; set; } = null;
        public string ArtifactGoal { get; set; } = "";
        public string ScreenshotPath { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public IArtifactDetector ArtifactDetector { get; set; } = null;
        public IMatchFilter MatchFilter { get; set; } = null;
        public bool ShouldEvaluate { get; set; } = false;
        public bool ShouldCache { get; set; } = false;
        public string LibraryFileName { get; } = "artifacts.bin";

        public ILoggerFactory LoggerFactory { get; set; }
        public string DetectorSelection { get; set; } = "orb";
        public string FilterSelection { get; set; } = "simple";

        /// <summary>
        /// Setup the loggerFactory and return a new logger with the given categoryName.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        private ILogger SetupLogging(string categoryName)
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new NLogLoggerProvider());
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
        private bool TestLockFile(string path, ILogger logger)
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
                logger.LogError("Can not write to working directory {path} with error: {0}.", path, e.Message);
                return false;
            }
            catch (IOException e)
            {
                logger.LogError("Can not write to working directory {path} with error: {0}.", path, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Try to resolve the artifact artifactDetector by a given selection.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        private bool SetupArtifactDetector(ILogger logger)
        {
            if (detectorSelectionMap.ContainsKey(DetectorSelection))
            {
                logger.LogInformation("Using artifactDetector {detectorSelection}.", DetectorSelection);
                ArtifactDetector = detectorSelectionMap[DetectorSelection](LoggerFactory);
                return true;
            }

            return false;
        }

        private bool SetupMatchFilter(ILogger logger)
        {
            if (filterSelectionMap.ContainsKey(FilterSelection))
            {
                logger.LogInformation("Using match filter {filterSelection}.", FilterSelection);
                MatchFilter = filterSelectionMap[FilterSelection]();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the artifact library from a file.
        /// </summary>
        /// <param name="logger"></param>
        private void FetchArtifactLibrary(ILogger logger, VADStopwatch stopwatch = null)
        {
            // Get the artifact library from a file.
            if (File.Exists(WorkingDirectory + LibraryFileName))
            {
                try
                {
                    ArtifactLibrary = ArtifactLibrary.FromFile(WorkingDirectory + LibraryFileName, ArtifactDetector, stopwatch, LoggerFactory);
                    logger.LogDebug("Loaded artifact library from file {0}.", WorkingDirectory + LibraryFileName);
                }
                catch (SerializationException)
                {
                    logger.LogWarning("Deserialization of artifact library failed.");
                }
            }
            else
            {
                logger.LogDebug("Artifact library file not found at {0}.", WorkingDirectory + LibraryFileName);
            }
        }

        public Setup(string[] commandLineArguments)
        {
            Logger = SetupLogging("Setup");

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
                TestLockFile(WorkingDirectory, Logger);
            }

            // Which artifact detector should we use?
            if (!SetupArtifactDetector(Logger))
            {
                ShowHelp(options);
                throw new SetupError("Could not set up artifact detector.");
            }

            // Which match filter should we use?
            if (!SetupMatchFilter(Logger))
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
                FetchArtifactLibrary(Logger, Stopwatch);
            }

            // Some error occurred, get an empty library.
            if (ArtifactLibrary == null)
            {
                Logger.LogDebug("Creating new artifact library instance.");
                try
                {
                    ArtifactLibrary = new ArtifactLibrary(WorkingDirectory, ArtifactDetector, LoggerFactory);
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
