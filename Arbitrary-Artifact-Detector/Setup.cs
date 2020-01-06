using ArbitraryArtifactDetector.DebugUtilities;
using ArbitraryArtifactDetector.Helper;
using Microsoft.Extensions.Logging;
using Mono.Options;
using NLog.Extensions.Logging;
using System;
using System.IO;

namespace ArbitraryArtifactDetector
{
    [Serializable]
    class Setup
    {
        private bool shouldShowHelp = false;

        /// <summary>
        /// 
        /// </summary>
        private ILogger Logger { get; set; }
        public AADStopwatch Stopwatch { get; set; } = null;

        public string ArtifactGoal { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public bool ShouldEvaluate { get; set; } = false;
        public bool ShouldCache { get; set; } = false;
        public string LibraryFileName { get; } = "artifacts.bin";

        public ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>
        /// This class handles the parsing of the configuration files and command line arguments for this program, plus the setup of the working environment.
        /// </summary>
        /// <param name="commandLineArguments"></param>
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
            if (shouldShowHelp || string.IsNullOrEmpty(ArtifactGoal))
            {
                ShowHelp(options);
                throw new SetupError("Screenshot path or artifact goal not set.");
            }
            // We got the info.
            Logger.LogInformation("We got the artifact goal {artifactGoal}.", ArtifactGoal);

            // Is there an existing and accessable working directory?
            if (Directory.Exists(WorkingDirectory))
            {
                if (!FileHelper.DirectoryIsWritable(WorkingDirectory, out Exception exception))
                {
                    Logger.LogError("Can not write to working directory {path} with error: {0}.", WorkingDirectory, exception.Message);
                }
            }

            // Determine if we use a stopwatch in this run.
            if (ShouldEvaluate)
            {
                // Get stopwatch for evaluation.
                Stopwatch = AADStopwatch.GetInstance();
            }
        }

        /// <summary>
        /// Setup the loggerFactory.
        /// </summary>
        /// <returns></returns>
        private void SetupLogging()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new NLogLoggerProvider());
        }

        /// <summary>
        /// Get a new logger for the supplied category name.
        /// </summary>
        /// <param name="categoryName">Name for the logging.</param>
        /// <returns>A new ILogger instance for the category name.</returns>
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
                { "a|artifact=", "Name of the artifact to look for (required).", a => ArtifactGoal = a },
                { "f|filepath=", "Path to the working directory (default is current directory). The recipes must be in this folder!", f => WorkingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath(f)) },
                { "c|cache", "Cache the artifact types.", c => ShouldCache = c != null },
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
    }
}
