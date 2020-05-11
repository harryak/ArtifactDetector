using MessagePack;
using MessagePack.Formatters;
using System;

namespace ItsApe.ArtifactDetector.Converters
{
    /// <summary>
    /// MessagePack formatter to encode and decode IntPtr.
    /// </summary>
    public class IntPtrFormatter : IMessagePackFormatter<IntPtr>
    {
        /// <summary>
        /// Deserialize IntPtr from reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="options"></param>
        /// <returns>An IntPtr.</returns>
        public IntPtr Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // Try to read Nil value first.
            if (reader.TryReadNil())
            {
                // Zero IntPtrs are encoded as Nil to save space.
                return IntPtr.Zero;
            }

            options.Security.DepthStep(ref reader);
            try
            {
                return new IntPtr(reader.ReadInt64());
            }
            finally
            {
                reader.Depth--;
            }
        }

        /// <summary>
        /// Write the given IntPtr to writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public void Serialize(ref MessagePackWriter writer, IntPtr value, MessagePackSerializerOptions options)
        {
            // Save space if we got a zero. IntPtrs can't be null in this case, but make sure.
            if (value == null || value == IntPtr.Zero)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteInt64(value.ToInt64());
        }
    }
}