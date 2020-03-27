using System;
using System.Windows.Forms;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using ItsApe.ArtifactDetector.Viewers;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Image recognizing detector.
    /// </summary>
    internal class VisualFeatureDetector : BaseDetector, IDetector
    {
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
            // Shorthand for reference images.
            var referenceImages = runtimeInformation.ReferenceImages.GetProcessedImages();

            // Do we have reference images?
            if (referenceImages.Count < 1)
            {
                throw new ArgumentNullException("No images for the artifact type found.");
            }

            // For all matching windows:
            bool artifactFound = false;
            foreach (var matchingWindowEntry in runtimeInformation.MatchingWindowsInformation)
            {
                // Make screenshot of artifact window and extract the features.
                var observedImage = FeatureExtractor.ExtractFeatures(WindowCapturer.CaptureWindow(matchingWindowEntry.Key));
                artifactFound = FeatureExtractor.ImageContainsArtifactType(observedImage, referenceImages, out var drawingResult, out int matchCount);

#if DEBUG
                // Show the results in a window.
                if (drawingResult != null)
                    Application.Run(new ImageViewer(drawingResult));
#endif

                // Stop if the artifact was found.
                if (artifactFound)
                {
                    break;
                }
            }

            //TODO: Adjust certainty.
            return new DetectorResponse() { ArtifactPresent = artifactFound ? DetectorResponse.ArtifactPresence.Certain : DetectorResponse.ArtifactPresence.Impossible };
        }
    }
}
