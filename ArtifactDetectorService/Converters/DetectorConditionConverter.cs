using System;
using ItsApe.ArtifactDetector.DetectorConditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ItsApe.ArtifactDetector.Converters
{
    internal class DetectorConditionConverter<ObjectType> : JsonConverter<IDetectorCondition<ObjectType>>
    {
        public override IDetectorCondition<ObjectType> ReadJson(JsonReader reader, Type objectType, IDetectorCondition<ObjectType> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            IDetectorCondition<ObjectType> outputCondition;

            string conditionString;
            if (reader.ValueType == typeof(string))
            {
                conditionString = reader.Value as string;
            }
            else
            {
                var jObject = JObject.Load(reader);
                conditionString = jObject.First.Value<string>();
            }

            if (hasExistingValue)
            {
                if (existingValue.GetType() != typeof(DetectorConditionSet<ObjectType>))
                {
                    outputCondition = new DetectorConditionSet<ObjectType>(ConditionOperator.AND);
                    ((DetectorConditionSet<ObjectType>)outputCondition).AddCondition(existingValue);
                } else
                {
                    outputCondition = existingValue;
                }

                ((DetectorConditionSet<ObjectType>)outputCondition).AddCondition(DetectorConditionParser<ObjectType>.ParseConditionString(conditionString));
            } else
            {
                outputCondition = DetectorConditionParser<ObjectType>.ParseConditionString(conditionString);
            }

            return outputCondition;
        }

        public override void WriteJson(JsonWriter writer, IDetectorCondition<ObjectType> value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
