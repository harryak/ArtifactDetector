using System;
using ItsApe.ArtifactDetector.Detectors;
using ItsApe.ArtifactDetector.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ItsApe.ArtifactDetector.Converters
{
    /// <summary>
    /// Converter from a JSON-string to ItsApe.ArtifactDetector.DetectorsIDetector.
    /// Can not convert the other way round.
    /// </summary>
    internal class DetectorConverter : JsonConverter<IDetector>
    {
        /// <summary>
        /// Read json and parse it into an IDetector of the appropriate type.
        /// </summary>
        /// <param name="reader">Load the JObject from here.</param>
        /// <param name="objectType">Unused here.</param>
        /// <param name="existingValue">Unused here.</param>
        /// <param name="hasExistingValue">Unused here.</param>
        /// <param name="serializer">Unused here.</param>
        /// <returns></returns>
        public override IDetector ReadJson(JsonReader reader, Type objectType, IDetector existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.TokenType != JsonToken.StartObject) return null;
            var jObject = JObject.Load(reader);

            if (!jObject.HasValues)
            {
                return null;
            }

            string[] entry = new string[3];
            if (jObject.Count < 2)
            {
                // Only one detector, instantiate directly.
                entry[0] = jObject.First.First.Value<string>();
                if (entry[0].IndexOf(';') > 0)
                {
                    entry = entry[0].Split(';');
                }
                return InstantiateDetectorFromString(entry[0], entry[1] ?? "", entry[2] ?? "");
            }

            // We have multiple detectors, chain them up using a CompoundDetector.
            ICompoundDetector compoundDetector = new CompoundDetector();
            foreach (var childObject in jObject)
            {
                entry = childObject.Value.Value<string>().Split(';');
                compoundDetector.AddDetector(InstantiateDetectorFromString(entry[0], entry[1] ?? "", entry[2] ?? ""));
            }

            return compoundDetector;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, IDetector value, JsonSerializer serializer)
        {
            var jObject = new JObject();
            if (value.GetType() == typeof(CompoundDetector))
            {
                var compoundDetector = value as CompoundDetector;
                foreach (var detector in compoundDetector.GetDetectors())
                {
                    jObject.Add(detector.Key.ToString(), detector.Value.GetType().Name + ";;");
                }
            }
            else
            {
                jObject.Add("0", value.GetType().Name + ";;");
            }
            jObject.WriteTo(writer);
        }

        /// <summary>
        /// Try to instanciate a detector in the namespace of IDetector given by
        /// its class name.
        /// </summary>
        /// <param name="detectorClassName">Class to instantiate.</param>
        /// <param name="preConditions">Pre-conditions for the instance.</param>
        /// <param name="goals">Goals for the instance.</param>
        /// <returns>A new IDetector subclass instance.</returns>
        private IDetector InstantiateDetectorFromString(string detectorClassName, string preConditions, string goals)
        {
            string detectorNamespace = typeof(IDetector).Namespace;
            var detectorType = Type.GetType(detectorNamespace + "." + detectorClassName, true, false);
            var detectorInstance = (IDetector) Activator.CreateInstance(detectorType);
            detectorInstance.SetPreConditions(DetectorConditionParser<ArtifactRuntimeInformation>.ParseConditionString(preConditions));
            detectorInstance.SetTargetConditions(DetectorConditionParser<DetectorResponse>.ParseConditionString(goals));

            return detectorInstance;
        }
    }
}
