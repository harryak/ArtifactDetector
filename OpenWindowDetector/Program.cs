using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using ItsApe.ArtifactDetector.Models;

namespace ArtifactDetector.OpenWindowDetector
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var detector = new ItsApe.OpenWindowDetector.Detectors.OpenWindowDetector();
            var binaryFormatter = new BinaryFormatter();
            ArtifactRuntimeInformation runtimeInformation;
            // Get runtime information from memory mapped file from external process.
            using (var memoryMappedFile = MemoryMappedFile.OpenExisting(@"Global\AD-runtimeInfo", MemoryMappedFileRights.ReadWrite))
            {
                using (var memoryStream = memoryMappedFile.CreateViewStream())
                {
                    runtimeInformation = (ArtifactRuntimeInformation)binaryFormatter.Deserialize(memoryStream);

                    detector.FindArtifact(ref runtimeInformation);

                    memoryStream.Position = 0;
                    binaryFormatter.Serialize(memoryStream, runtimeInformation);
                }
            }
        }
    }
}
