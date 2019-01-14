/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using System;
using System.Collections.Generic;

namespace ArtifactDetector.Model
{
    /// <summary>
    /// All data belonging to a single artifact type.
    /// </summary>
    [Serializable]
    public class ArtifactType
    {
        /// <summary>
        /// Optional name of this artifact type.
        /// </summary>
        private readonly string name;
        /// <summary>
        /// A set of observed images associated with this artifact type.
        /// </summary>
        public List<ObservedImage> Images { get; private set; }

        /// <summary>
        /// Constructor needs at least one observed image for this artifact type.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="_name"></param>
        public ArtifactType(ObservedImage image, string _name = "")
        {
            name = _name;

            if (image == null)
                throw new ArgumentNullException(nameof(image));

            // Add first observed image to be not empty.
            Images.Add(image);
        }
    }
}
