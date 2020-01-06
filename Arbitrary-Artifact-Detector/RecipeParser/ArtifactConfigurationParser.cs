using ArbitraryArtifactDetector.DebugUtilities;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.RecipeParser.Model;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ArbitraryArtifactDetector.RecipeParser
{
    class ArtifactConfigurationParser : Debuggable
    {
        private Setup Setup { get; set; }

        internal ArtifactConfigurationParser(Setup setup) : base(setup)
        {
            Setup = setup;
        }

        public ArtifactConfiguration ParseRecipe()
        {
            StartStopwatch();

            string recipePath = FileHelper.AddDirectorySeparator(Setup.WorkingDirectory) + Setup.ArtifactGoal + ".yml";
            
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTagMapping("!ruby/object:Recipe", typeof(RawRecipe))
                .Build();

            RawRecipe rawRecipe;
            using (StreamReader streamReader = new StreamReader(recipePath))
            {
                rawRecipe = deserializer.Deserialize<RawRecipe>(streamReader);
            }

            StopStopwatch("Finished parsing of recipe in {0}ms.");
            StartStopwatch();

            ArtifactConfiguration artifactConfiguration = new ArtifactConfiguration(
                Setup.ArtifactGoal,
                DetectorParser.FromRawRecipe(rawRecipe, Setup),
                FileHelper.AddDirectorySeparator(Setup.WorkingDirectory) + "screenshot");

            StopStopwatch("Finished generating artifact configuration in {0}ms.");

            return artifactConfiguration;
        }
    }
}
