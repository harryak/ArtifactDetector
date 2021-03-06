using Emgu.CV;
using ItsApe.ArtifactDetector.Models;
using System.Collections.Generic;

namespace ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor
{
    /// <summary>
    /// Interface for visual featrue extraction from images.
    /// </summary>
    public interface IVisualFeatureExtractor
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
        /// <param name="referenceImages">Reference images for this artifact type.</param>
        /// <returns>Whether a match was found.</returns>
        bool ImageMatchesReference(ProcessedImage observedImage, ICollection<ProcessedImage> referenceImages);
    }
}