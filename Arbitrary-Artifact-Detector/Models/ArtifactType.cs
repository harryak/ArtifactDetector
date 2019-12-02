/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// All data belonging to a single artifact type.
    /// </summary>
    [Serializable]
    public class ArtifactType
    {
        /// <summary>
        /// A set of observed images associated with this artifact type.
        /// </summary>
        public List<ProcessedImage> Images { get; private set; }

        public string Name { get; }

        /// <summary>
        /// Constructor needs at least one observed image for this artifact type.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="name"></param>
        public ArtifactType(ProcessedImage image = null, string name = "")
        {
            Name = name;

            // Instantiate Images and add first observed image so list is never empty.
            if (image != null)
            {
                Images = new List<ProcessedImage>
                {
                    image
                };
            } else
            {
                Images = new List<ProcessedImage>();
            }
        }
    }
}
