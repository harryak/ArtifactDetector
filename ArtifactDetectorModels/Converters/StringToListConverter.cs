using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ItsApe.ArtifactDetector.Converters
{
    /// <summary>
    /// Converter from a JSON-string to Systems.Collection.Generic.List<string>.
    /// </summary>
    public class StringToListConverter : JsonConverter<List<string>>
    {
        /// <summary>
        /// Read json and parse it into a List of strings.
        /// </summary>
        /// <param name="reader">Load the JObject from here.</param>
        /// <param name="objectType">Unused here.</param>
        /// <param name="existingValue">Used for initialization.</param>
        /// <param name="hasExistingValue">Used for initialization.</param>
        /// <param name="serializer">Unused here.</param>
        /// <returns></returns>
        public override List<string> ReadJson(JsonReader reader, Type objectType, List<string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var outputList = existingValue;

            string entry;
            if (!hasExistingValue)
            {
                outputList = new List<string>();
            }

            if (reader.ValueType == typeof(string))
            {
                entry = reader.Value as string;
            }
            else
            {
                var jObject = JObject.Load(reader);
                entry = jObject.First.Value<string>();
            }

            outputList.AddRange(entry.Split('|'));

            return outputList;
        }

        /// <summary>
        /// Write a List<string> object to a JSON string via glueing the entries with "|".
        /// </summary>
        /// <param name="writer">Writer to write out the JSON.</param>
        /// <param name="value">List of strings to be converted.</param>
        /// <param name="serializer">Unused here.</param>
        public override void WriteJson(JsonWriter writer, List<string> value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Join<string>("|", value));
        }
    }
}
