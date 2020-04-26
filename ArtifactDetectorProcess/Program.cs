using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetectorProcess.Detectors;
using MessagePack;

namespace ItsApe.ArtifactDetectorProcess
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            using (var logWriter = new StreamWriter(@"C:\Users\Felix\Desktop\lol.log"))
            {
                if (args.Length != 2)
                {
                    logWriter.WriteLine("Tja, keene arjumente, wa. {0}.", args.ToString());
                    return;
                }

                // Get important information from arguments.
                var memoryStreamName = args[0];
                var memoryStreamMutexName = args[1];

                logWriter.WriteLine("Najaaa, immerhin ma, mutex is {0}.", memoryStreamMutexName);

                var memoryStreamMutex = Mutex.OpenExisting(memoryStreamMutexName);

                logWriter.WriteLine("Hab dit Mjuteks jefundn.");
                logWriter.Flush();

                try
                {
                    // This is intentional.
                    while (true)
                    {
                        // The release of the mutex is the signal to detect.
                        if (memoryStreamMutex.WaitOne())
                        {
                            logWriter.WriteLine("Hab dit Mjuteks.");
                            logWriter.Flush();
                            // Get runtime information from memory mapped file from external process.
                            ArtifactRuntimeInformation runtimeInformation;
                            IDetector detector;
                            using (var memoryMappedFile = MemoryMappedFile.OpenExisting(memoryStreamName, MemoryMappedFileRights.ReadWrite))
                            {
                                using (var memoryStream = memoryMappedFile.CreateViewStream())
                                {
                                    // Fetch runtime information from mmf.
                                    runtimeInformation = MessagePackSerializer.Deserialize<ArtifactRuntimeInformation>(memoryStream);

                                    switch (runtimeInformation.DetectorToRun)
                                    {
                                        case ExternalDetector.DesktopIconDetector:
                                            detector = new OpenWindowDetector();
                                            break;
                                        case ExternalDetector.None:
                                        default:
                                            // Misconfiguration, stop further execution immediately.
                                            memoryStreamMutex.ReleaseMutex();
                                            logWriter.WriteLine("Falsche Konfig.");
                                            continue;
                                    }

                                    // If we arrive here: 
                                    detector.FindArtifact(ref runtimeInformation);

                                    // Write new runtime information to mmf.
                                    memoryStream.Position = 0;
                                    MessagePackSerializer.Serialize(memoryStream, runtimeInformation);
                                }
                            }

                            logWriter.WriteLine("Un tschöö.");
                            logWriter.Flush();
                            memoryStreamMutex.ReleaseMutex();
                            // Runtime information is garbage collected now to get the slimmest process possible.
                        }
                    }
                }
                catch (Exception e)
                {
                    logWriter.WriteLine("Exception: {0}.", e.Message);
                    logWriter.Flush();
                }
            }
        }
    }
}
