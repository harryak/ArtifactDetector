using ArbitraryArtifactDetector.Model;
using Emgu.CV;
using System.Drawing;

namespace ArbitraryArtifactDetector.Detectors.VisualFeatureExtractor
{
    interface IVisualFeatureExtractor
    {
        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="image">The image to extract features from.</param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
        ProcessedImage ExtractFeatures(Mat image);

        /// <summary>
        /// Extract features of the given image using an OpenCV feature extractor.
        /// </summary>
        /// <param name="imagePath">The path to the image to extract features from.</param>
        /// <returns>The observed image with keypoints and descriptors.</returns>
        ProcessedImage ExtractFeatures(string imagePath);

        /// <summary>
        /// Analyze the given observed image, whether the artifact type can be found within.
        /// </summary>
        /// <param name="observedImage">The observed image.</param>
        /// <param name="artifactType">The artifact type containing visual information.</param>
        /// <param name="drawingResult">The resulting image to draw in a window.</param>
        /// <param name="matchCount">Count of matches, if one was found.</param>
        /// <returns>Whether a match was found.</returns>
        bool ImageContainsArtifactType(ProcessedImage observedImage, ArtifactConfiguration artifactType, out Mat drawingResult, out int matchCount);
    }
}
