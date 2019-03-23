/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using Accord.Imaging;
using ArtifactDetector.Model;
using System.Diagnostics;
using System.Drawing;

namespace ArtifactDetector
{
    class ArtifactExtractor
    {
        private BagOfVisualWords _bow;
        public Stopwatch Stopwatch { get; }

        public ArtifactExtractor(Stopwatch stopwatch = null)
        {
            Stopwatch = stopwatch;
        }

        public ArtifactType Extract(Bitmap[] images)
        {
            if (Stopwatch != null)
                Stopwatch.Restart();

            //TODO: Adjust numberOfWords
            _bow = BagOfVisualWords.Create(numberOfWords: 36);
            //TODO: Check learning Algorithm etc.
            _bow.Learn(images);

            if (Stopwatch != null)
                Stopwatch.Stop();

            return new ArtifactType(images, _bow, _bow.Transform(images));
        }
    }
}
