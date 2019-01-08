/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using ArtifactDetector.ArtifactDetector;
using ArtifactDetector.Model;
using Microsoft.Extensions.Logging;
using Mono.Options;
using NLog.Extensions.Logging;
using System;
using System.Diagnostics;

namespace ArtifactDetector
{
    class Program
    {
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

        static void Main(string[] args)
        {
            // Setup logging.
            ILoggerProvider loggerProvider = new NLogLoggerProvider();
            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(loggerProvider);
            var logger = loggerFactory.CreateLogger("Main");

            // Setup command line arguments.
            var logVerbosity = 0;
            var screenshotPath = "";
            var artifactGoal = "";
            var shouldShowHelp = false;

            var options = new OptionSet
            {
                { "s|screenshot=", "the path to the screenshot to search in.", p => screenshotPath = p },
                { "a|artifact=", "name of the artifact to look for.", a => artifactGoal = a },
                { "v", "increase log message verbosity", v => {
                    if (v != null)
                        ++logVerbosity;
                } },
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null }
            };

            // Parse the command line.
            logger.LogDebug("Try to parse the command line");
            try
            {
                options.Parse(args);
            } catch (OptionException e)
            {
                logger.LogError("Error ocurred while parsing the options: {0} with option {1}.", e.Message, e.OptionName);
                Console.Error.Write("Error: " + e.Message);
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

            // Launch actual program.
            logger.LogDebug("Call the actual comparison.");
            IArtifactDetector detector = new FastArtifactDetector(loggerFactory);

            Stopwatch stopwatch = new Stopwatch();
            ArtifactType artifactType = new ArtifactType(detector.ExtractFeatures(artifactGoal, stopwatch));

            detector.AnalyzeScreenshot(detector.ExtractFeatures(screenshotPath, stopwatch), artifactType);
        }
    }
}
