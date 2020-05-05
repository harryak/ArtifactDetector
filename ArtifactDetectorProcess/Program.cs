using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetectorProcess.Detectors;
using ItsApe.ArtifactDetectorProcess.Utilities;
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

            #region setup

            // Get important information from arguments.
            var memoryStreamBaseName = args[0];
            var memoryStreamMutexName = args[1];

            // Setup mmf for common shared memory. NOTE: This is not actively released here, the service has to do that!
            var sharedMemoryLock = Semaphore.OpenExisting(memoryStreamMutexName);
            var sharedMemory = MemoryMappedFile.OpenExisting(memoryStreamBaseName, MemoryMappedFileRights.ReadWrite);
            var sharedMemoryStream = sharedMemory.CreateViewStream();

            // Setup mmf for screenshot of entire screen. NOTE: This is not actively released here, the service has to do that!
            var screenshotMemory = MemoryMappedFile.OpenExisting(memoryStreamBaseName + "-screen", MemoryMappedFileRights.ReadWrite);
            var screenshotMemoryStream = screenshotMemory.CreateViewStream();

            // Preemtively instantiate detectors to save time when called.
            var openWindowDetector = new OpenWindowDetector();
            var desktopIconDetector = new DesktopIconDetector();
            var trayIconDetector = new TrayIconDetector();

            // The same for the screenshot tool.
            var screenshotCapturer = new VisualCapturer();

            bool writeBack = false;

            #endregion setup

            // This is intentional.
            while (true)
            {
                try
                {
                    // The release of the mutex is the signal to detect.
                    if (sharedMemoryLock.WaitOne())
                    {
                        // Get runtime information from memory mapped file from external process.
                        ArtifactRuntimeInformation runtimeInformation;
                        // Fetch runtime information from mmf.
                        sharedMemoryStream.Position = 0;
                        runtimeInformation = MessagePackSerializer.Deserialize<ArtifactRuntimeInformation>(sharedMemoryStream);

                        // Choose which detector to call.
                        switch (runtimeInformation.ProcessCommand)
                        {
                            case ExternalProcessCommand.OpenWindowDetector:
                                openWindowDetector.FindArtifact(ref runtimeInformation);
                                writeBack = true;
                                break;

                            case ExternalProcessCommand.DesktopIconDetector:
                                desktopIconDetector.FindArtifact(ref runtimeInformation);
                                writeBack = true;
                                break;

                            case ExternalProcessCommand.TrayIconDetector:
                                trayIconDetector.FindArtifact(ref runtimeInformation);
                                writeBack = true;
                                break;

                            case ExternalProcessCommand.ScreenshotCapturer:
                                screenshotCapturer.TakeScreenshots(ref screenshotMemoryStream);
                                break;

                            case ExternalProcessCommand.None:
                            default:
                                // Misconfiguration, stop further execution immediately.
                                sharedMemoryLock.Release();
                                continue;
                        }

                        if (writeBack)
                        {
                            // Write new runtime information to mmf.
                            sharedMemoryStream.Position = 0;
                            MessagePackSerializer.Serialize(sharedMemoryStream, runtimeInformation);
                            sharedMemoryStream.Flush();
                            writeBack = false;
                        }

                        sharedMemoryLock.Release();
                        // Runtime information is garbage collected now to get the slimmest process possible.
                    }
                }
                catch (Exception)
                {
                    sharedMemoryLock.Release();
                }
            }
        }
    }
}
