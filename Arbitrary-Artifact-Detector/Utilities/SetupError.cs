using System;
using System.Runtime.Serialization;

namespace ItsApe.ArtifactDetector.Utilities
{
    /// <summary>
    /// Error/Exception class to throw when the ApplicationSetup is faulty.
    /// </summary>
    [Serializable]
    internal class ApplicationSetupError : Exception
    {
        public ApplicationSetupError()
        {
        }

        public ApplicationSetupError(string message) : base(message)
        {
        }

        public ApplicationSetupError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ApplicationSetupError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
