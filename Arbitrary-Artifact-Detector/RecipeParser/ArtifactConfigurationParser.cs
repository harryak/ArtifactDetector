using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.RecipeParser.Model;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.RecipeParser
{
    class ArtifactConfigurationParser
    {
        /// <summary>
        /// Sorted list of detector configurations directly from YAML.
        /// </summary>
        private List<RawDetectorEntry> RawDetectorEntries { get; set; }

        public ArtifactConfiguration ParseRecipe(string recipePath)
        {
            throw new System.NotImplementedException();
        }
    }
}
