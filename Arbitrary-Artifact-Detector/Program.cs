/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector
{
    class Program
    {
        private static Setup setup;

        internal static Setup Setup { get => setup; set => setup = value; }

        [STAThread]
        static int Main(string[] args)
        {
#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            try
            {
                Setup = new Setup(args);
            }
            catch (SetupError)
            {
                return -1;
            }

            var logger = Setup.Logger;

            // Launch the actual program.
            logger.LogDebug("Call the actual comparison.");
            ArtifactType artifactType = null;
            try
            {
                artifactType = Setup.ArtifactLibrary.GetArtifactType(Setup.ArtifactGoal, Setup.Stopwatch);
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
            ProcessedImage observedImage = Setup.ArtifactDetector.ExtractFeatures(Setup.ScreenshotPath, Setup.Stopwatch);
            if (observedImage == null)
            {
                logger.LogError("Could not get the screenshot.");
                return -1;
            }

            bool artifactFound = Setup.ArtifactDetector.ImageContainsArtifactType(observedImage, artifactType, Setup.MatchFilter, out Mat drawingResult, out int matchCount, Setup.Stopwatch);

#if DEBUG
            // Show the results in a window.
            if (drawingResult != null)
                Application.Run(new ImageViewer(drawingResult));
#endif

            // Chache the artifact library.
            if (Setup.ShouldCache)
            {
                Setup.ArtifactLibrary.ExportToFile(Setup.LibraryFileName, Setup.WorkingDirectory);
                logger.LogInformation("Exported artifact library to {libraryFileName}.", Setup.WorkingDirectory + Setup.LibraryFileName);
            }

            logger.LogInformation("The comparison yielded {0}.", artifactFound);

            if (Setup.ShouldEvaluate)
            {
                try
                {
                    bool printHeader = false;
                    if (!File.Exists("output.csv")) printHeader = true;
                    using (StreamWriter file = new StreamWriter("output.csv", true))
                    {
                        if (printHeader) file.WriteLine("artifactDetector;screenshot_path;artifact_goal;" + Setup.Stopwatch.LabelsToCSV() + ";found;match_count");

                        file.WriteLine(Setup.DetectorSelection + ";" + Setup.ScreenshotPath + ";" + Setup.ArtifactGoal + ";" + Setup.Stopwatch.TimesToCSVinNSprecision() + ";" + artifactFound + ";" + matchCount);
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
