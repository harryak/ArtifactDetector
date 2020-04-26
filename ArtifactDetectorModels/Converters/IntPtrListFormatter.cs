using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.Generic;

namespace ItsApe.ArtifactDetector.Converters
{
    public class IntPtrListFormatter : IMessagePackFormatter<List<IntPtr>>
    {
        List<IntPtr> IMessagePackFormatter<List<IntPtr>>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);

            var outputList = new List<IntPtr>();
            var intPtrFormatter = new IntPtrFormatter();
            var count = reader.ReadArrayHeader();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    outputList.Add(intPtrFormatter.Deserialize(ref reader, options));
                }
            }
            finally
            {
                reader.Depth--;
            }

            return outputList;
        }

        public void Serialize(ref MessagePackWriter writer, List<IntPtr> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            var intPtrFormatter = new IntPtrFormatter();
            writer.WriteArrayHeader(value.Count);
            for (var i = 0; i < value.Count; i++)
            {
                intPtrFormatter.Serialize(ref writer, value[i], options);
            }
        }
    }
}