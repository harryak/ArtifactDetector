using ArbitraryArtifactDetector.Detectors;
using ArbitraryArtifactDetector.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Converters
{
    internal class DetectorConverter : JsonConverter<IDetector>
    {
        public override bool CanWrite => false;

        public override IDetector ReadJson(JsonReader reader, Type objectType, IDetector existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            if (!jObject.HasValues)
            {
                return null;
            }

            string[] entry;
            if (jObject.Count < 2)
            {
                // Only one detector, instantiate directly.
                entry = jObject.First.Value<string>().Split(';');
                return InstantiateDetectorFromString(entry[0], entry[1] ?? "", entry[2] ?? "");
            }

            // We have multiple detectors, chain them up using a CompoundDetector.
            ICompoundDetector compoundDetector = new CompoundDetector();
            foreach (KeyValuePair<string, JToken> childObject in jObject)
            {
                entry = childObject.Value.Value<string>().Split(';');
                compoundDetector.AddDetector(InstantiateDetectorFromString(entry[0], entry[1] ?? "", entry[2] ?? ""));
            }

            return compoundDetector;
        }

        public override void WriteJson(JsonWriter writer, IDetector value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private IDetector InstantiateDetectorFromString(string detectorClassName, string preConditions, string goals)
        {
            string detectorNamespace = typeof(IDetector).Namespace;
            Type detectorType = Type.GetType(detectorNamespace + "." + detectorClassName, true, false);
            IDetector detectorInstance = (IDetector) Activator.CreateInstance(detectorType);
            detectorInstance.SetPreConditions(DetectorConditionParser<ArtifactRuntimeInformation>.ParseConditionString(preConditions));
            detectorInstance.SetTargetConditions(DetectorConditionParser<DetectorResponse>.ParseConditionString(goals));

            return detectorInstance;
        }
    }
}