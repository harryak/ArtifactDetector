using MessagePack;
using MessagePack.Formatters;
using System;

namespace ItsApe.ArtifactDetector.Converters
{
    public class IntPtrFormatter : IMessagePackFormatter<IntPtr>
    {
        public IntPtr Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return IntPtr.Zero;
            }
            options.Security.DepthStep(ref reader);
            var ptr = reader.ReadInt64();
            reader.Depth--;

            return new IntPtr(ptr);
        }

        public void Serialize(ref MessagePackWriter writer, IntPtr value, MessagePackSerializerOptions options)
        {
            if (value == IntPtr.Zero)
            {
                writer.WriteNil();
                return;
            }
            writer.WriteInt64(value.ToInt64());
        }
    }
}