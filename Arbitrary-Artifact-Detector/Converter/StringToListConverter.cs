using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Converter
{
    internal class StringToListConverter : JsonConverter<List<string>>
    {
        public override List<string> ReadJson(JsonReader reader, Type objectType, List<string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            List<string> outputList = existingValue;

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
                JObject jObject = JObject.Load(reader);
                entry = jObject.First.Value<string>();
            }

            outputList.AddRange(entry.Split('|'));

            return outputList;
        }

        public override void WriteJson(JsonWriter writer, List<string> value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Join<string>("|", value));
        }
    }
}