/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using Emgu.CV;
using Emgu.CV.Util;

namespace ArtifactDetector.Model
{
    public class ArtifactType
    {
        public ArtifactType(ObservedImage image)
        {
            Image = image.Image;
            KeyPoints = image.KeyPoints;
            Descriptors = image.Descriptors;
        }

        public Mat Image { get; }
        public VectorOfKeyPoint KeyPoints { get; }
        public Mat Descriptors { get; }
    }
}
