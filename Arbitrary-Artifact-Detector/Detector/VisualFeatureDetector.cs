using ArbitraryArtifactDetector.Detector.VisualFeatureExtractor;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Utility;
using ArbitraryArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Base class for all artifact detectors.
    /// </summary>
    internal class VisualFeatureDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Map for selecting a feature extractor by its name.
        /// </summary>
        private readonly Dictionary<string, Func<Setup, IVisualFeatureExtractor>> visualFeatureExtractorSelectionMap =
            new Dictionary<string, Func<Setup, IVisualFeatureExtractor>>(){
                { "akaze", (Setup setup) => { return new AkazeDetector(setup); } },
                { "brisk", (Setup setup) => { return new BriskDetector(setup); } },
                { "kaze",  (Setup setup) => { return new KazeDetector(setup); } },
                { "orb",   (Setup setup) => { return new OrbDetector(setup); } }
            };

        public VisualFeatureDetector(Setup setup) : base(setup)
        {
        }

        public ArtifactReferenceImageCache ArtifactLibrary { get; set; } = null;
        protected IVisualFeatureExtractor FeatureExtractor { get; set; }

        /// <summary>
        /// Main function of this detector: Find the artifact provided by the configuration.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        ///
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Should we use a cache for the artifact library?
            if (Setup.ShouldCache)
            {
                FetchArtifactLibrary(Setup);
            }

            // Some error occurred, get an empty library.
            if (ArtifactLibrary == null)
            {
                Logger.LogDebug("Creating new artifact library instance.");
                try
                {
                    ArtifactLibrary = ArtifactReferenceImageCache.GetInstance(Setup, FeatureExtractor, Setup.GetLogger("ArtifactLibrary"), Stopwatch);
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not instantiate artifact library: {0}", e.Message);
                    throw new SetupError("Could not instantiate artifact library.");
                }
            }

            ICollection<ProcessedImage> referenceImages = ArtifactLibrary.GetProcessedImages();

            if (referenceImages.Count < 1)
            {
                Logger.LogError("Could not get any images for the artifact type.");
                throw new ArgumentNullException("No images for the artifact type found.");
            }

            // Make screenshot of artifact window and extract the features.
            ProcessedImage observedImage = FeatureExtractor.ExtractFeatures(
                WindowCapturer.CaptureWindow(runtimeInformation.MatchingWindowsInformation.GetEnumerator().Current.Key)
                );

            bool artifactTypeFound = FeatureExtractor.ImageContainsArtifactType(observedImage, referenceImages, out Mat drawingResult, out int matchCount);

#if DEBUG
            // Prepare debug window output.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Show the results in a window.
            if (drawingResult != null)
                Application.Run(new ImageViewer(drawingResult));
#endif

            // Cache the artifact library.
            if (Setup.ShouldCache)
            {
                ArtifactLibrary.Save();
                Logger.LogInformation("Exported artifact library to {libraryFileName}.", Setup.WorkingDirectory + Setup.LibraryFileName);
            }

            Logger.LogInformation("The comparison yielded {0}.", artifactTypeFound);

            if (Setup.ShouldEvaluate)
            {
                try
                {
                    bool printHeader = false;
                    if (!File.Exists("output.csv")) printHeader = true;
                    using (StreamWriter file = new StreamWriter("output.csv", true))
                    {
                        if (printHeader) file.WriteLine("artifactDetector;screenshot_path;artifact_goal;" + Setup.Stopwatch.LabelsToCSV() + ";found;match_count");

                        file.WriteLine(AADConfig.FeatureExtractorSelection + ";" + Setup.ArtifactTarget + ";" + Setup.Stopwatch.TimesToCSVinNSprecision() + ";" + artifactTypeFound + ";" + matchCount);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not write to output.csv: {0}", e.Message);
                }
            }

            return new DetectorResponse() { ArtifactPresent = artifactTypeFound, Certainty = 100 };
        }

        /// <summary>
        /// Get the artifact library from a file.
        /// </summary>
        /// <param name="logger"></param>
        private void FetchArtifactLibrary(Setup setup)
        {
            // Get the artifact library from a file.
            if (File.Exists(setup.WorkingDirectory.FullName + setup.LibraryFileName))
            {
                try
                {
                    ArtifactLibrary = ArtifactReferenceImageCache.GetInstance(setup, FeatureExtractor, setup.GetLogger("ArtifactReferenceImageCache"), setup.Stopwatch);
                    Logger.LogDebug("Loaded artifact library from file {0}.", setup.WorkingDirectory.FullName + setup.LibraryFileName);
                }
                catch (SerializationException)
                {
                    Logger.LogWarning("Deserialization of artifact library failed.");
                }
            }
            else
            {
                Logger.LogDebug("Artifact library file not found at {0}.", setup.WorkingDirectory.FullName + setup.LibraryFileName);
            }
        }

        private bool SetupFeatureExtractor(Setup setup)
        {
            if (visualFeatureExtractorSelectionMap.ContainsKey(AADConfig.FeatureExtractorSelection))
            {
                Logger.LogInformation("Using feature extractor {extractorSelection}.", AADConfig.FeatureExtractorSelection);
                FeatureExtractor = visualFeatureExtractorSelectionMap[AADConfig.FeatureExtractorSelection](setup);
                return true;
            }

            return false;
        }
    }
}