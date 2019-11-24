/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using VisualArtifactDetector.VisualArtifactDetector.Detectors;
using VisualArtifactDetector.Helper;
using VisualArtifactDetector.Model;
using VisualArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using Mono.Options;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace VisualArtifactDetector
{
    class Program
    {
        // TODO: Move to config and add parameter.
        private readonly static string libraryFileName = "artifacts.bin";
        private static ILoggerFactory loggerFactory;
        private static IArtifactDetector detector = null;
        private static ArtifactLibrary artifactLibrary = null;

        /// <summary>
        /// Map for selecting an artifact detector by its name.
        /// </summary>
        private readonly static Dictionary<string, Func<ILoggerFactory, IArtifactDetector>> detectorSelectionMap =
            new Dictionary<string, Func<ILoggerFactory, IArtifactDetector>>(){
                { "akaze", (ILoggerFactory loggerFactory) => { return new AkazeArtifactDetector(loggerFactory); } },
                { "brisk", (ILoggerFactory loggerFactory) => { return new BriskArtifactDetector(loggerFactory); } },
                { "kaze", (ILoggerFactory loggerFactory) => { return new KazeArtifactDetector(loggerFactory); } },
                { "orb", (ILoggerFactory loggerFactory) => { return new OrbArtifactDetector(loggerFactory); } }
            };

        /// <summary>
        /// Setup the loggerFactory and return a new logger with the given categoryName.
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        private static ILogger SetupLogging(string categoryName)
        {
            loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new NLogLoggerProvider());
            return loggerFactory.CreateLogger(categoryName);
        }

        // Setup command line arguments.
        private static string screenshotPath = "";
        private static string artifactGoal = "";
        private static string workingDirectory = "";
        private static string detectorSelection = "orb";  // Initialize with standard detector
        private static bool shouldShowHelp = false;
        private static bool shouldEvaluate = false;
        private static bool shouldCache = false;

        /// <summary>
        /// Prepares an option set for the command line.
        /// </summary>
        /// <returns></returns>
        private static OptionSet SetupCmdOptions()
        {
            // Set default value to current directory for working directory.
            workingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath("."));

            return new OptionSet
            {
                { "h|help", "Show this message and exit.", h => shouldShowHelp = h != null },
                { "s|screenshot=", "The path to the screenshot to search in (required).", p => screenshotPath = p },
                { "a|artifact=", "Name of the artifact to look for (required).", a => artifactGoal = a },
                { "f|filepath=", "Path to the working directory (default is current directory). The recipes must be in this folder!", f => workingDirectory = FileHelper.AddDirectorySeparator(Path.GetFullPath(f)) },
                { "c|cache", "Cache the artifact types.", c => shouldCache = c != null },
                { "d|detector=", "Detector to use (default: orb). [akaze, brisk, kaze, orb]", d => detectorSelection = d},
                { "e|evaluate", "Include stopwatch output.", e => shouldEvaluate = e != null },
            };
        }

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

        /// <summary>
        /// Test if a directory is writable by attempting to write and delete a lock file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static bool TestLockFile(string path, ILogger logger)
        {
            try
            {
                using (var lockFile = File.OpenWrite(workingDirectory + "lock"))
                {
                    ;
                }

                File.Delete(workingDirectory + "lock");
            }
            catch (AccessViolationException e)
            {
                logger.LogError("Can not write to working directory {workingDirectory} with error: {0}.", workingDirectory, e.Message);
                return false;
            }
            catch (IOException e)
            {
                logger.LogError("Can not write to working directory {workingDirectory} with error: {0}.", workingDirectory, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Try to resolve the artifact detector by a given selection.
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        private static bool SetupArtifactDetector(ILogger logger)
        {
            if (detectorSelectionMap.ContainsKey(detectorSelection))
            {
                logger.LogInformation("Using detector {detectorSelection}.", detectorSelection);
                detector = detectorSelectionMap[detectorSelection](loggerFactory);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the artifact library from a file.
        /// </summary>
        /// <param name="logger"></param>
        private static void FetchArtifactLibrary(ILogger logger, VADStopwatch stopwatch = null)
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

        [STAThread]
        static int Main(string[] args)
        {
#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            var logger = SetupLogging("Main");

            // Parse the command line.
            logger.LogDebug("Parsing the command line.");
            var options = SetupCmdOptions();
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
                return -1;
            }
            // We got the info.
            logger.LogInformation("We got the path {screenshotPath} and the artifact goal {artifactGoal}.", screenshotPath, artifactGoal);

            // Is there an existing and accessable working directory?
            if (Directory.Exists(workingDirectory))
            {
                TestLockFile(workingDirectory, logger);
            }

            // Which artifact detector should we use?
            if (!SetupArtifactDetector(logger))
            {
                ShowHelp(options);
                return -1;
            }

            // Determine if we use a stopwatch in this run.
            VADStopwatch stopwatch = null;
            if (shouldEvaluate)
            {
                // Get stopwatch for evaluation.
                stopwatch = VADStopwatch.GetInstance();
            }

            // Should we use a cache for the artifact library?
            if (shouldCache)
            {
                FetchArtifactLibrary(logger, stopwatch);
            }

            // Some error occurred, get an empty library.
            if (artifactLibrary == null)
            {
                logger.LogDebug("Creating new artifact library instance.");
                try
                {
                    artifactLibrary = new ArtifactLibrary(workingDirectory, detector, loggerFactory);
                }
                catch (Exception e)
                {
                    logger.LogError("Could not instantiate artifact library: {0}", e.Message);
                    return -1;
                }
            }

            // Launch the actual program.
            logger.LogDebug("Call the actual comparison.");
            ArtifactType artifactType = null;
            try
            {
                artifactType = artifactLibrary.GetArtifactType(artifactGoal, stopwatch);
            }
            catch (Exception e)
            {
                logger.LogError("Could not get artifact type: {0}", e.Message);
                return -1;
            }

            if (artifactType.Images.Count < 1)
            {
                logger.LogError("Could not get any images for the artifact type.");
                return -1;
            }

            // Extract the features of the given image for comparison.
            ProcessedImage observedImage = detector.ExtractFeatures(screenshotPath, stopwatch);
            if (observedImage == null)
            {
                logger.LogError("Could not get the screenshot.");
                return -1;
            }

            bool artifactFound = detector.ImageContainsArtifactType(observedImage, artifactType, out Mat drawingResult, out int matchCount, stopwatch);

#if DEBUG
            // Show the results in a window.
            if (drawingResult != null)
                Application.Run(new ImageViewer(drawingResult));
#endif

            // Chache the artifact library.
            if (shouldCache)
            {
                artifactLibrary.ExportToFile(libraryFileName, workingDirectory);
                logger.LogInformation("Exported artifact library to {libraryFileName}.", workingDirectory + libraryFileName);
            }

            logger.LogInformation("The comparison yielded {0}.", artifactFound);

            if (shouldEvaluate)
            {
                try
                {
                    bool printHeader = false;
                    if (!File.Exists("output.csv")) printHeader = true;
                    using (StreamWriter file = new StreamWriter("output.csv", true))
                    {
                        if (printHeader) file.WriteLine("detector;screenshot_path;artifact_goal;" + stopwatch.LabelsToCSV() + ";found;match_count");

                        file.WriteLine(detectorSelection + ";" + screenshotPath + ";" + artifactGoal + ";" + stopwatch.TimesToCSVinNSprecision() + ";" + artifactFound + ";" + matchCount);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Could not write to output.csv: {0}", e.Message);
                }
            }

            return artifactFound ? 0 : 1;
        }
    }
}
