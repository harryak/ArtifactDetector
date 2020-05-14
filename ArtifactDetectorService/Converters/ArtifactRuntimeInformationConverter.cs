using System;
using System.IO;
using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ItsApe.ArtifactDetector.Converters
{
    /// <summary>
    /// Converter from a JSON-string to ItsApe.ArtifactDetector.Models.ArtifactRuntimeInformation.
    /// Can not convert the other way round.
    /// </summary>
    internal class ArtifactRuntimeInformationConverter : JsonConverter<ArtifactRuntimeInformation>
    {
        /// <summary>
        /// Read json and parse it into an ArtifactRuntimeInformation with appropriate property values.
        /// </summary>
        /// <param name="reader">Load the JObject from here.</param>
        /// <param name="objectType">Unused here.</param>
        /// <param name="existingValue">Unused here.</param>
        /// <param name="hasExistingValue">Unused here.</param>
        /// <param name="serializer">Used to populate the information properties.</param>
        /// <returns>ArtifactRuntimeInformation instance with appropriate property values.</returns>
        public override ArtifactRuntimeInformation ReadJson(JsonReader reader, Type objectType, ArtifactRuntimeInformation existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var outputInformation = new ArtifactRuntimeInformation();
            var jObject = JObject.Load(reader);

            // Populare the properties not requiring custom handling.
            using (var subReader = jObject.CreateReader())
                serializer.Populate(subReader, outputInformation);

            if (jObject.TryGetValue("reference_images_path", out var referenceImagePath))
            {
                outputInformation.ReferenceImages = ArtifactReferenceImageCache.GetInstance(
                    outputInformation.ArtifactName,
                    ApplicationSetup.GetInstance().GetLogger("Image Cache"),
                    ApplicationSetup.GetInstance().WorkingDirectory.FullName,
                    VisualFeatureExtractorFactory.GetFeatureExtractor(
                        ApplicationConfiguration.FeatureExtractorSelection,
                        ApplicationConfiguration.MatchDistanceThreshold,
                        ApplicationConfiguration.MatchUniquenessThreshold,
                        ApplicationConfiguration.MinimumMatchesRequired,
                        ApplicationConfiguration.MatchFilterSelection,
                        ApplicationSetup.GetInstance().GetLogger("Feature extractor")));

                var filePath = new DirectoryInfo(referenceImagePath.Value<string>());
                outputInformation.ReferenceImages.ProcessImagesInPath(filePath);
            }
            else if (jObject.TryGetValue("reference_images_cache", out var referenceImageCacheFile))
            {
                outputInformation.ReferenceImages = ArtifactReferenceImageCache.FromFile(
                    Uri.UnescapeDataString(referenceImageCacheFile.Value<string>()),
                    ApplicationSetup.GetInstance().GetLogger("Image Cache"),
                    VisualFeatureExtractorFactory.GetFeatureExtractor(
                        ApplicationConfiguration.FeatureExtractorSelection,
                        ApplicationConfiguration.MatchDistanceThreshold,
                        ApplicationConfiguration.MatchUniquenessThreshold,
                        ApplicationConfiguration.MinimumMatchesRequired,
                        ApplicationConfiguration.MatchFilterSelection,
                        ApplicationSetup.GetInstance().GetLogger("Feature extractor")));
            }

            return outputInformation;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, ArtifactRuntimeInformation value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var jObject = JObject.FromObject(value);
            if (value.ReferenceImages != null)
            {
                jObject.Add("reference_images_cache", JToken.FromObject(
                    Uri.UnescapeDataString(value.ReferenceImages.PersistentFilePath.FullName),
                    serializer));
            }
            jObject.WriteTo(writer);
        }
    }
}
