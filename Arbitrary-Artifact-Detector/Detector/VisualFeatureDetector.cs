using ArbitraryArtifactDetector.Detector.VisualFeatureExtractor;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Utility;
using ArbitraryArtifactDetector.Viewer;
using Emgu.CV;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Default constructor for detectors.
        /// </summary>
        /// <param name="setup">The setup of the application.</param>
        /// <exception cref="ArgumentException">If the visual feature extractor configuration is invalid.</exception>
        public VisualFeatureDetector(Setup setup) : base(setup)
        {
            SetupFeatureExtractor(setup);
        }

        /// <summary>
        /// Feature extractor used in this run.
        /// </summary>
        protected IVisualFeatureExtractor FeatureExtractor { get; set; }

        /// <summary>
        /// Main function of this detector: Find the artifact provided by the configuration.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        /// <exception cref="ArgumentNullException">If there are no images.</exception>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            ICollection<ProcessedImage> referenceImages = runtimeInformation.ReferenceImages.GetProcessedImages();

            if (referenceImages.Count < 1)
            {
                throw new ArgumentNullException("No images for the artifact type found.");
            }

            // Make screenshot of artifact window and extract the features.
            // TODO: Do this in loop for all matching windows.
            ProcessedImage observedImage = FeatureExtractor.ExtractFeatures(
                WindowCapturer.CaptureWindow(runtimeInformation.MatchingWindowsInformation.GetEnumerator().Current.Key)
                );

            bool artifactTypeFound = FeatureExtractor.ImageContainsArtifactType(observedImage, referenceImages, out Mat drawingResult, out int matchCount);

#if DEBUG
            // Show the results in a window.
            if (drawingResult != null)
                Application.Run(new ImageViewer(drawingResult));
#endif

            return new DetectorResponse() { ArtifactPresent = artifactTypeFound, Certainty = 100 };
        }

        /// <summary>
        /// Set instance of visual feature extractor to value in configuration file.
        /// </summary>
        /// <param name="setup">The setup of this application.</param>
        /// <returns>True on success.</returns>
        /// <exception cref="ArgumentException">If the visual feature extractor configuration is invalid.</exception>
        private void SetupFeatureExtractor(Setup setup)
        {
            // If the value in the configuration is invalid throw error.
            if (!visualFeatureExtractorSelectionMap.ContainsKey(AADConfig.FeatureExtractorSelection))
            {
                throw new ArgumentException("Could not instantiate feature extractor, wrong name given.");
            }

            Logger.LogInformation("Using feature extractor {extractorSelection}.", AADConfig.FeatureExtractorSelection);
            FeatureExtractor = visualFeatureExtractorSelectionMap[AADConfig.FeatureExtractorSelection](setup);
        }
    }
}