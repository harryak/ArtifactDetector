/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using Emgu.CV.Features2D;
using Microsoft.Extensions.Logging;

namespace ArtifactDetector.ArtifactDetector
{
    class OrbArtifactDetector : BaseArtifactDetector, IArtifactDetector
    {
        public OrbArtifactDetector(ILoggerFactory _loggerFactory)
        {
            Logger = _loggerFactory.CreateLogger("OrbArtifactDetector");

            FeatureDetector = new ORBDetector(500, 1.2f, 4, 50, 0, 2, ORBDetector.ScoreType.Harris, 50);

            //LshIndexParams indexParams = new LshIndexParams(20, 10, 2);
            //SearchParams searchParams = new SearchParams();
            //DescriptorMatcher = new FlannBasedMatcher(indexParams, searchParams);
            DescriptorMatcher = new BFMatcher(DistanceType.Hamming);
        }

        /*public override void FindMatch(ObservedImage observedImage, ArtifactType artifactType, out VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, Stopwatch stopwatch = null)
        {
            matches = new VectorOfVectorOfDMatch();
            mask = null;
            homography = null;

            artifactType.Images[0].Descriptors.ConvertTo(artifactType.Images[0].Descriptors, Emgu.CV.CvEnum.DepthType.Cv32F);
            observedImage.Descriptors.ConvertTo(observedImage.Descriptors, Emgu.CV.CvEnum.DepthType.Cv32F);

            DescriptorMatcher.Add(artifactType.Images[0].Descriptors);
            DescriptorMatcher.KnnMatch(observedImage.Descriptors, matches, 2, mask);
        }*/

        //new FlannBasedMatcher DescriptorMatcher { get; set; }
    }
}
