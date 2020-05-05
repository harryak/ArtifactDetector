using Emgu.CV;
using Emgu.CV.Structure;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Services;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Image recognizing detector.
    /// </summary>
    internal class VisualFeatureDetector : BaseDetector, IDetector
    {
        private bool ArtifactFound;

        /// <summary>
        /// Instantiate via setting the feature extractor from the configurations.
        /// </summary>
        public VisualFeatureDetector()
        {
            FeatureExtractor = VisualFeatureExtractorFactory.GetFeatureExtractor(
                ApplicationConfiguration.FeatureExtractorSelection,
                ApplicationConfiguration.MatchDistanceThreshold,
                ApplicationConfiguration.MatchUniquenessThreshold,
                ApplicationConfiguration.MinimumMatchesRequired,
                ApplicationConfiguration.MatchFilterSelection,
                ApplicationSetup.GetInstance().GetLogger("Feature extractor"));
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
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, int sessionId)
        {
            Logger.LogInformation("Detecting visual features now.");

            // Do we have reference images?
            if (runtimeInformation.ReferenceImages.ImagesCount < 1)
            {
                Logger.LogInformation("No reference images given for detector. Can not detect visual matches.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            InitializeDetection(sessionId, out var screenshot);

            if (screenshot != null)
            {
                Logger.LogInformation("Analyzing screenshot using {0} reference images.", runtimeInformation.ReferenceImages.ImagesCount);
                AnalyzeScreenshot(ref runtimeInformation, ref screenshot);
            }
            else
            {
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
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
        public void InitializeDetection(int sessionId, out Image<Rgba, byte> screenshot)
        {
            ArtifactFound = false;

            screenshot = SessionManager.GetInstance().RetrieveSessionScreenshot(sessionId);
        }

        private void AnalyzeScreenshot(ref ArtifactRuntimeInformation runtimeInformation, ref Image<Rgba, byte> screenshot)
        {
            ProcessedImage observedImage;
            if (runtimeInformation.WindowsInformation.Count < 1)
            {
                Logger.LogInformation("Taking entire screen for feature detection.");
                observedImage = FeatureExtractor.ExtractFeatures(screenshot.Mat);

                if (observedImage == null)
                {
                    Logger.LogInformation("Could not get features from screenshot.");
                    return;
                }

                // Stop if the artifact was found.
                ArtifactFound = FeatureExtractor.ImageMatchesReference(observedImage, runtimeInformation.ReferenceImages.GetProcessedImages());
                return;
            }

            Logger.LogInformation("Cutting out windows from screenshot.");
            Mat screenshotCutout;
            for (var i = 0; i < runtimeInformation.WindowsInformation.Count; i++)
            {
                // Cut out area of screenshot that the artifact is in.
                screenshot.ROI = runtimeInformation.WindowsInformation[i].BoundingArea;
                screenshotCutout = screenshot.Copy().Mat;
                observedImage = FeatureExtractor.ExtractFeatures(screenshotCutout);

                if (observedImage == null)
                {
                    Logger.LogError("Could not get features from screenshot-cutout.", screenshot);
                    continue;
                }

                // Stop if the artifact was found.
                if (FeatureExtractor.ImageMatchesReference(observedImage, runtimeInformation.ReferenceImages.GetProcessedImages()))
                {
                    ArtifactFound = true;
                    break;
                }
            }
        }
    }
}
