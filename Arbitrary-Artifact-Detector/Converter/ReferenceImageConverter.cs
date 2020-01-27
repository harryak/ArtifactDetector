using ArbitraryArtifactDetector.Detector;
using ArbitraryArtifactDetector.Detector.VisualFeatureExtractor;
using ArbitraryArtifactDetector.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ArbitraryArtifactDetector.Converter
{
    internal class ArtifactRuntimeInformationConverter : JsonConverter<ArtifactRuntimeInformation>
    {
        public override bool CanWrite => false;

        public override ArtifactRuntimeInformation ReadJson(JsonReader reader, Type objectType, ArtifactRuntimeInformation existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            ArtifactRuntimeInformation outputInformation = new ArtifactRuntimeInformation();
            JObject jObject = JObject.Load(reader);

            // Populare the properties not requiring custom handling.
            using (var subReader = jObject.CreateReader())
                serializer.Populate(subReader, outputInformation);

            outputInformation.ReferenceImages = ArtifactReferenceImageCache.GetInstance(outputInformation.ArtifactName, Setup.GetInstance(), VisualFeatureExtractorFactory.GetFeatureExtractor());

            JToken referenceImagePath;
            jObject.TryGetValue("reference_images", out referenceImagePath);
            DirectoryInfo filePath = new DirectoryInfo(referenceImagePath.Value<string>());

            outputInformation.ReferenceImages.ProcessImagesInPath(filePath);

            return outputInformation;
        }

        public override void WriteJson(JsonWriter writer, ArtifactRuntimeInformation value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Can't write.");
        }
    }
}