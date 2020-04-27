using System;
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
            if (args.Length != 2)
            {
                return;
            }

            // Get important information from arguments.
            var memoryStreamName = args[0];
            var memoryStreamMutexName = args[1];

            var memoryStreamLock = Semaphore.OpenExisting(memoryStreamMutexName);

            // Preemtively instantiate detectors to save time when called.
            var openWindowDetector = new OpenWindowDetector();
            var desktopIconDetector = new DesktopIconDetector();
            var trayIconDetector = new TrayIconDetector();

            // This is intentional.
            while (true)
            {
                try
                {
                    // The release of the mutex is the signal to detect.
                    if (memoryStreamLock.WaitOne())
                    {
                        // Get runtime information from memory mapped file from external process.
                        ArtifactRuntimeInformation runtimeInformation;
                        using (var memoryMappedFile = MemoryMappedFile.OpenExisting(memoryStreamName, MemoryMappedFileRights.ReadWrite))
                        {
                            using (var memoryStream = memoryMappedFile.CreateViewStream())
                            {
                                // Fetch runtime information from mmf.
                                runtimeInformation = MessagePackSerializer.Deserialize<ArtifactRuntimeInformation>(memoryStream);

                                // Choose which detector to call.
                                switch (runtimeInformation.DetectorToRun)
                                {
                                    case ExternalDetector.OpenWindowDetector:
                                        openWindowDetector.FindArtifact(ref runtimeInformation);
                                        break;

                                    case ExternalDetector.DesktopIconDetector:
                                        desktopIconDetector.FindArtifact(ref runtimeInformation);
                                        break;

                                    case ExternalDetector.TrayIconDetector:
                                        trayIconDetector.FindArtifact(ref runtimeInformation);
                                        break;

                                    case ExternalDetector.None:
                                    default:
                                        // Misconfiguration, stop further execution immediately.
                                        memoryStreamLock.Release();
                                        continue;
                                }

                                // Write new runtime information to mmf.
                                memoryStream.Position = 0;
                                MessagePackSerializer.Serialize(memoryStream, runtimeInformation);
                            }
                        }

                        memoryStreamLock.Release();
                        // Runtime information is garbage collected now to get the slimmest process possible.
                    }
                }
                catch (Exception)
                {
                    memoryStreamLock.Release();
                }
            }
        }
    }
}
