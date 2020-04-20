using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Emgu.CV.UI;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Image recognizing detector.
    /// </summary>
    internal class VisualFeatureDetector : BaseDetector, IDetector
    {
        private bool ArtifactFound;

        private ICollection<ProcessedImage> ReferenceImages;

        private IList<WindowInformation> WindowInformation;

        /// <summary>
        /// Instantiate via setting the feature extractor from the configurations.
        /// </summary>
        public VisualFeatureDetector()
        {
            FeatureExtractor = VisualFeatureExtractorFactory.GetFeatureExtractor();
        }

        /// <summary>
        /// Feature extractor used in this run.
        /// </summary>
        private IVisualFeatureExtractor FeatureExtractor { get; set; }

        /// <summary>
        /// Main function of this detector: Find the artifact provided by the configuration.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        /// <exception cref="ArgumentNullException">If there are no images.</exception>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (IsScreenActive(ref runtimeInformation))
            {
                Logger.LogInformation("Not detecting, screen is locked.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }
            Logger.LogInformation("Detecting visual features now.");

            // Shorthand for reference images.
            ReferenceImages = runtimeInformation.ReferenceImages.GetProcessedImages();

            // Do we have reference images?
            if (ReferenceImages.Count < 1)
            {
                Logger.LogInformation("No reference images given for detector. Can not detect visual matches.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            InitializeDetection(ref runtimeInformation);

            //TODO: Versions for icons.
            if (WindowInformation.Count > 0)
            {
                AnalyzeWindowHandles();
            }
            else
            {
                AnalyzeScreens();
            }

            if (ArtifactFound)
            {
                runtimeInformation.CountVisualFeautureMatches = 1;
                
                Logger.LogInformation("Found a match in reference images.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }
            
            Logger.LogInformation("Found no matches in reference images.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Initialize (or reset) the detection for FindArtifact.
        /// </summary>
        /// <param name="runtimeInformation">Reference to object to initialize from.</param>
        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            ArtifactFound = false;
            WindowInformation = runtimeInformation.WindowsInformation;
        }

        private void AnalyzeWindowHandles()
        {
            foreach (var windowInformation in WindowInformation)
            {
                // Make screenshot of artifact window and extract the features.
                var observedImage = FeatureExtractor.ExtractFeatures(VisualCapturer.CaptureRegion(windowInformation.BoundingArea));

                if (observedImage == null)
                {
                    continue;
                }

                ArtifactFound = FeatureExtractor.ImageMatchesReference(observedImage, ReferenceImages, out var drawingResult, out int matchCount);

#if DEBUG
                // Show the results in a window.
                if (drawingResult != null)
                    Application.Run(new ImageViewer(drawingResult));
#endif

                // Stop if the artifact was found.
                if (ArtifactFound)
                {
                    break;
                }
            }
        }

        private void AnalyzeScreens()
        {
            foreach (var screen in Screen.AllScreens)
            {
                // Make screenshot of whole screen and extract the features.
                var observedImage = FeatureExtractor.ExtractFeatures(VisualCapturer.CaptureScreen(screen));

                ArtifactFound = FeatureExtractor.ImageMatchesReference(observedImage, ReferenceImages, out var drawingResult, out int matchCount);

                // Stop if the artifact was found.
                if (ArtifactFound)
                {
#if DEBUG
                    // Show the results in a window.
                    if (drawingResult != null)
                        //Application.Run(new ImageViewer(drawingResult));
#endif
                    break;
                }
            }
        }
    }
}
