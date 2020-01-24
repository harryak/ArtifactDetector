using ArbitraryArtifactDetector.DebugUtility;
using ArbitraryArtifactDetector.Model;

namespace ArbitraryArtifactDetector.Parser
{
    /// <summary>
    /// A small configuration parser used to parse strings from IPC into ArtifactConfiguration.
    /// </summary>
    internal class ArtifactConfigurationParser : Debuggable
    {
        /// <summary>
        /// Empty constructor, just to setup the Debuggable.
        /// </summary>
        /// <param name="setup">The current setup of this program.</param>
        public ArtifactConfigurationParser(Setup setup) : base(setup) { }

        public ArtifactConfiguration ParseConfigurationString(string artifactConfigurationString)
        {
            StartStopwatch();

            //string recipePath = FileHelper.AddDirectorySeparator(Setup.WorkingDirectory) + Setup.ArtifactGoal + ".yml";

            /*var deserializer = new DeserializerBuilder()
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
            */

            StopStopwatch("Finished generating artifact configuration in {0}ms.");

            return null;//artifactConfiguration;
        }
    }
}