/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using ArtifactDetector.ArtifactDetector;
using ArtifactDetector.Helper;
using ArtifactDetector.Model;
using ArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using Mono.Options;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace ArtifactDetector
{
    class Program
    {
        // TODO: Move to config and add parameter.
        private readonly static string libraryFileName = "artifacts.bin";

        /// <summary>
        /// Map for selecting an artifact detector with its name.
        /// </summary>
        private readonly static Dictionary<string, Func<ILoggerFactory, IArtifactDetector>> detectorSelectionMap =
            new Dictionary<string, Func<ILoggerFactory, IArtifactDetector>>(){
                { "akaze", (ILoggerFactory loggerFactory) => { return new AkazeArtifactDetector(loggerFactory); } },
                { "brisk", (ILoggerFactory loggerFactory) => { return new BriskArtifactDetector(loggerFactory); } },
                { "kaze", (ILoggerFactory loggerFactory) => { return new KazeArtifactDetector(loggerFactory); } },
                { "orb", (ILoggerFactory loggerFactory) => { return new OrbArtifactDetector(loggerFactory); } }
            };

        /// <summary>
        /// Prints the command line options/help.
        /// </summary>
        /// <param name="options"></param>
        private static void ShowHelp(OptionSet options)
        {
            // show some app description message
            Console.WriteLine("Usage: Artifact-Detector.exe [OPTIONS]+");
            Console.WriteLine("Takes the supplied screenshot and looks in it for the artifact specified.");
            Console.WriteLine();

            // output the options
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            // Setup logging.
            ILoggerProvider loggerProvider = new NLogLoggerProvider();
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(loggerProvider);
            var logger = loggerFactory.CreateLogger("Main");

            // Setup command line arguments.
            var screenshotPath = "";
            var artifactGoal = "";
            var workingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath("."));
            var detectorSelection = "orb";  // Initialize with standard detector
            var shouldShowHelp = false;
            var shouldEvaluate = false;
            var shouldCache = false;

            var options = new OptionSet
            {
                { "s|screenshot=", "the path to the screenshot to search in.", p => screenshotPath = p },
                { "a|artifact=", "name of the artifact to look for.", a => artifactGoal = a },
                { "f|filepath=", "path to the working directory (default is current directory).", f => workingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath(f)) },
                { "d|detector=", "detector to use (default: orb). [akaze, brisk, kaze, orb]", d => detectorSelection = d},
                { "h|help", "show this message and exit.", h => shouldShowHelp = h != null },
                { "e|evaluate", "include stopwatch.", e => shouldEvaluate = e != null },
                { "c|cache", "cache the artifact types.", c => shouldCache = c != null }
            };

            // Parse the command line.
            logger.LogDebug("Parsing the command line.");
            try
            {
                options.Parse(args);
            } catch (OptionException e)
            {
                logger.LogError("Error ocurred while parsing the options: {0} with option {1}.", e.Message, e.OptionName);
                shouldShowHelp = true;
            }

            // Should we show the help?
            if (shouldShowHelp || string.IsNullOrEmpty(screenshotPath) || string.IsNullOrEmpty(artifactGoal))
            {
                ShowHelp(options);
                return;
            }
            // We got the info.
            logger.LogInformation("We got the path {screenshotPath} and the artifact goal {artifactGoal}.", screenshotPath, artifactGoal);

            // Is there an existing and accessable working directory?
            if (Directory.Exists(workingDirectory))
            {
                try
                {
                    using (var lockFile = File.OpenWrite(workingDirectory + "lock"))
                    {
                        ;
                    }

                    File.Delete(workingDirectory + "lock");
                } catch (AccessViolationException e)
                {
                    logger.LogError("Can not write to working directory {workingDirectory} with error: {0}.", workingDirectory, e.Message);
                    return;
                }
            }

            // Which artifact detector should we use?
            IArtifactDetector detector = null;
            if (detectorSelectionMap.ContainsKey(detectorSelection))
            {
                logger.LogInformation("Using detector {detectorSelection}.", detectorSelection);
                detector = detectorSelectionMap[detectorSelection](loggerFactory);
            } else
            {
                ShowHelp(options);
                return;
            }

            // Determine if we use a stopwatch in this run.
            Stopwatch stopwatch = null;
            if (shouldEvaluate)
            {
                // Get stopwatch for evaluation.
                stopwatch = new Stopwatch();
            }

            ArtifactLibrary artifactLibrary = null;

            // Should we use a cache for the artifact library?
            if (shouldCache)
            {
                // Get the artifact library from a file.
                if (File.Exists(workingDirectory + libraryFileName))
                {
                    try
                    {
                        artifactLibrary = ArtifactLibrary.FromFile(workingDirectory + libraryFileName, detector, stopwatch, loggerFactory);
                        logger.LogDebug("Loaded artifact library from file {0}.", workingDirectory + libraryFileName);
                    }
                    catch (SerializationException)
                    {
                        logger.LogWarning("Deserialization of artifact library failed.");
                    }
                }
                else
                {
                    logger.LogDebug("Artifact library file not found at {0}.", workingDirectory + libraryFileName);
                }
            }

            // Some error occurred, get an empty library.
            if (artifactLibrary == null)
            {
                logger.LogDebug("Creating new artifact library instance.");
                artifactLibrary = new ArtifactLibrary(workingDirectory, detector, loggerFactory);
            }

            // Launch the actual program.
            logger.LogDebug("Call the actual comparison.");
            ArtifactType artifactType = null;
            try
            {
                artifactType = artifactLibrary.GetArtifactType(artifactGoal, stopwatch);
            }
            catch (NotImplementedException e)
            {
                logger.LogError(e.Message);
                return;
            }

            ObservedImage observedImage = detector.ExtractFeatures(screenshotPath, stopwatch);

            Mat result = detector.AnalyzeImage(observedImage, artifactType, stopwatch);

#if DEBUG
            Application.Run(new ImageViewer(result));
#endif

            // Chache the artifact library.
            if (shouldCache)
            {
                artifactLibrary.ExportToFile(libraryFileName, workingDirectory);
                logger.LogInformation("Exported artifact library to {libraryFileName}", workingDirectory + libraryFileName);
            }

            return;
        }
    }
}
