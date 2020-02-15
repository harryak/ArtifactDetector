using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using ItsApe.ArtifactDetector.Viewers;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Image recognizing detector.
    /// </summary>
    internal class VisualFeatureDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Feature extractor used in this run.
        /// </summary>
        protected IVisualFeatureExtractor FeatureExtractor { get; set; }

        public VisualFeatureDetector()
        {
            FeatureExtractor = VisualFeatureExtractorFactory.GetFeatureExtractor();
        }

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
            ICollection<ProcessedImage> referenceImages = runtimeInformation.ReferenceImages.GetProcessedImages();

            // Do we have reference images?
            if (referenceImages.Count < 1)
            {
                throw new ArgumentNullException("No images for the artifact type found.");
            }

            // For all matching windows:
            bool artifactFound = false;
            foreach (KeyValuePair<IntPtr, WindowToplevelInformation> matchingWindowEntry in runtimeInformation.MatchingWindowsInformation)
            {
                // Make screenshot of artifact window and extract the features.
                ProcessedImage observedImage = FeatureExtractor.ExtractFeatures(WindowCapturer.CaptureWindow(matchingWindowEntry.Key));
                artifactFound = FeatureExtractor.ImageContainsArtifactType(observedImage, referenceImages, out Mat drawingResult, out int matchCount);

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