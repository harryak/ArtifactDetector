/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using Emgu.CV;
using Emgu.CV.Util;
using System;
using System.Drawing;

namespace VisualArtifactDetector.Model
{
    [Serializable]
    public class ProcessedImage
    {
        public ProcessedImage(Mat image, VectorOfKeyPoint keyPoints, Mat descriptors)
        {
#if DEBUG
            Image = image;
#endif
            Dimensions = new SizeF(image.Width, image.Height);
            KeyPoints = keyPoints;
            Descriptors = descriptors;
        }

#if DEBUG
        public Mat Image { get; }
#endif
        public SizeF Dimensions { get; }
        public VectorOfKeyPoint KeyPoints { get; }
        public Mat Descriptors { get; }
    }
}
