using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// All data belonging to a single artifact type.
    /// </summary>
    [Serializable]
    class ArtifactConfiguration
    {
        private string ReferenceImagePath { get; set; }

        private string[] ReferenceImageFiles { get; set; }

        /// <summary>
        /// A set of observed images associated with this artifact type.
        /// </summary>
        private List<ProcessedImage> _referenceImages = null;
        public List<ProcessedImage> ReferenceImages {
            get
            {
                // Lazy load the images as late as possible.
                if (_referenceImages == null)
                {
                    FetchImages();
                }
                return _referenceImages;
            }
            private set => _referenceImages = value;
        }

        public delegate List<ProcessedImage> ProcessImagesFunction(string[] imagePaths);
        private ProcessImagesFunction _processImagesFunction { get; set; } = null;

        /// <summary>
        /// Name (primary identifier) of this artifact.
        /// </summary>
        public string ArtifactName { get; private set; }
        
        /// <summary>
        /// Simple constructor.
        /// </summary>
        /// <param name="artifactName"></param>
        public ArtifactConfiguration(string artifactName, string imagePath = "", ProcessImagesFunction processImagesFunction = null)
        {
            ArtifactName = artifactName;
            ReferenceImagePath = imagePath;
            _processImagesFunction = processImagesFunction;
        }

        /// <summary>
        /// Helper function to lazy load the reference images if they are needed.
        /// </summary>
        private void FetchImages()
        {
            if (_processImagesFunction == null)
                _referenceImages = null;
            _referenceImages = _processImagesFunction(ReferenceImageFiles);
        }
    }
}
