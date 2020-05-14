using Emgu.CV;
using Emgu.CV.Structure;
using ItsApe.ArtifactDetector.DetectorConditions;
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
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="matchConditions">Condition to determine whether the detector's output yields a match.</param>
        /// <param name="sessionId">ID of the session to detect in (if appliccable).</param>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, int sessionId)
        {
            Logger.LogInformation("Detecting visual features now.");

            // Do we have reference images?
            if (runtimeInformation.ReferenceImages == null || runtimeInformation.ReferenceImages.ImagesCount < 1)
            {
                Logger.LogWarning("No reference images given for detector. Can not detect visual matches.");
                return DetectorResponse.PresencePossible;
            }

            InitializeDetection(sessionId, out var screenshot);

            if (screenshot != null)
            {
                Logger.LogInformation("Analyzing screenshot using {0} reference images.", runtimeInformation.ReferenceImages.ImagesCount);
                AnalyzeScreenshot(ref runtimeInformation, ref screenshot);
            }
            else
            {
                Logger.LogWarning("Could not make screenshot. Can not detect visual matches.");
                return DetectorResponse.PresencePossible;
            }

            if (ArtifactFound)
            {
                runtimeInformation.CountVisualFeautureMatches = 1;

                if (matchConditions != null)
                {
                    if (matchConditions.ObjectMatchesConditions(ref runtimeInformation))
                    {
                        Logger.LogInformation("Found a match in reference images and conditions for a match are met.");
                        return DetectorResponse.PresenceCertain;
                    }
                    else
                    {
                        Logger.LogInformation("Found a match in reference images, but conditions for a match are not met.");
                        return DetectorResponse.PresenceImpossible;
                    }
                }

                Logger.LogInformation("Found a match in reference images.");
                return DetectorResponse.PresenceCertain;
            }

            Logger.LogInformation("Found no matches in reference images.");
            return DetectorResponse.PresenceImpossible;
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

        /// <summary>
        /// Analyze the given screenshot in respect to the artifact reference images.
        /// </summary>
        /// <param name="runtimeInformation"></param>
        /// <param name="screenshot"></param>
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
            for (var i = 0; i < runtimeInformation.WindowsInformation.Count; i++)
            {
                // Cut out area of screenshot that the artifact is in.
                GetScreenshotCutout(ref runtimeInformation, ref screenshot, i, out var screenshotCutout);
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

        /// <summary>
        /// Get a cutout from the given screenshot with all overlaying windows filled as white rectangles.
        /// </summary>
        /// <param name="runtimeInformation"></param>
        /// <param name="screenshot"></param>
        /// <param name="windowIndex"></param>
        /// <param name="screenshotCutout"></param>
        private void GetScreenshotCutout(ref ArtifactRuntimeInformation runtimeInformation, ref Image<Rgba, byte> screenshot, int windowIndex, out Mat screenshotCutout)
        {
            // Cutout the region of interest from the "whole screenshot".
            screenshot.ROI = runtimeInformation.WindowsInformation[windowIndex].BoundingArea;
            var screenshotCutoutImg = screenshot.Copy();

            // Now fill every overlay with white color to devoid the area of any features.
            foreach (var overlays in runtimeInformation.VisibleWindowOutlines)
            {
                if (overlays.Key >= runtimeInformation.WindowsInformation[windowIndex].ZIndex)
                {
                    // Only consider overlays with zindex lower than the current window.
                    break;
                }
                // Fill the current overlay with white.
                screenshotCutoutImg.Draw(overlays.Value, new Rgba(255, 255, 255, 1), 0);
            }

            // Get Mat from Image<Rgba, byte>.
            screenshotCutout = screenshotCutoutImg.Mat;
        }
    }
}
