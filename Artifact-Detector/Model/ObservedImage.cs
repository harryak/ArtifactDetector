/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using Emgu.CV;
using Emgu.CV.Util;
using System;

namespace ArtifactDetector.Model
{
    [Serializable]
    public class ObservedImage
    {
        public ObservedImage(Mat image, VectorOfKeyPoint keyPoints, Mat descriptors)
        {
            Image = image;
            KeyPoints = keyPoints;
            Descriptors = descriptors;
        }

        public Mat Image { get; }
        public VectorOfKeyPoint KeyPoints { get; }
        public Mat Descriptors { get; }
    }
}
