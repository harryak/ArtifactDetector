using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data of an image which keypoints and feature descriptors have been extracted.
    /// Serializable for file caching.
    /// </summary>
    [Serializable]
    public class ProcessedImage
    {
        /// <summary>
        /// Instantiate with EmguCV objects.
        /// </summary>
        /// <param name="image">Only relevant in Debug setting: The actual image data.</param>
        /// <param name="keyPoints">Points of interest in the image.</param>
        /// <param name="descriptors">Feature descriptors of this image.</param>
        public ProcessedImage(Mat image, VectorOfKeyPoint keyPoints, Mat descriptors)
        {
#if DEBUG
            Image = image;
#endif
            Dimensions = new SizeF(image.Width, image.Height);
            KeyPoints = keyPoints;
            Descriptors = descriptors;
        }

        public SizeF Dimensions { get; }
#if DEBUG
        public Mat Image { get; }
#endif
        public VectorOfKeyPoint KeyPoints { get; }
        public Mat Descriptors { get; }
    }
}
