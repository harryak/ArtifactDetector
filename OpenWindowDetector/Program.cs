using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using ItsApe.ArtifactDetector.Models;
using ItsApe.OpenWindowDetector.Models;

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
            using (var writer = new StreamWriter(@"C:\Users\Felix\Desktop\lel.lol", true))
            {
                var binaryFormatter = new BinaryFormatter();
                ArtifactRuntimeInformation runtimeInformation;
                // Get runtime information from memory mapped file from external process.
                try
                {
                    using (var memoryMappedFile = MemoryMappedFile.OpenExisting(@"Global\AD-runtimeInfo", MemoryMappedFileRights.ReadWrite))
                    {
                        using (var memoryStream = memoryMappedFile.CreateViewStream())
                        {
                            writer.WriteLine("Rausholen");
                            runtimeInformation = (ArtifactRuntimeInformation)binaryFormatter.Deserialize(memoryStream);

                            writer.WriteLine("Schreiben");
                            runtimeInformation.CountOpenWindows = 50;

                            writer.WriteLine("Reinlegen");
                            binaryFormatter.Serialize(memoryStream, runtimeInformation);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    writer.WriteLine("Nicht gefunden 1");
                }
                catch (Exception e)
                {
                    writer.WriteLine("Unspezifizierter Fehler {0}.", e.Message);
                }
            }
        }
    }
}
