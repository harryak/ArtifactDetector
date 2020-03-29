using System;
using System.IO;

namespace ItsApe.ArtifactDetector.Helpers
{
    /// <summary>
    /// Class of helper functions for files.
    /// </summary>
    internal class FileHelper
    {
        /// <summary>
        /// Writes to a file using the write action.
        /// </summary>
        /// <param name="filename">The filename to write to.</param>
        /// <param name="writeAction">An action writing to the stream.</param>
        /// <param name="fileMode"></param>
        public static void WriteToFile(string filename, Action<Stream> writeAction, FileMode fileMode = FileMode.OpenOrCreate)
        {
            Stream stream = null;
            try
            {
                stream = File.Open(filename, fileMode);
                writeAction(stream);
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }
    }
}
