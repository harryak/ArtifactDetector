using System;
using System.Runtime.Serialization;

namespace ArbitraryArtifactDetector.Utilities
{
    [Serializable]
    internal class SetupError : Exception
    {
        public SetupError()
        {
        }

        public SetupError(string message) : base(message)
        {
        }

        public SetupError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SetupError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}