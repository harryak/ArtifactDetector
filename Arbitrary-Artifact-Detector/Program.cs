using ArbitraryArtifactDetector.EnvironmentalDetector;
using Microsoft.Extensions.Logging;
using System;

namespace ArbitraryArtifactDetector
{
    class Program
    {
        internal static Setup Setup { get; set; }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Setup = new Setup(args);
            }
            catch (SetupError)
            {
                return -1;
            }

            var logger = Setup.GetLogger("Main");

            var installedProgramsDetector = new InstalledProgramsDetector(Setup.GetLogger("InstalledProgramsDetector"), Setup.Stopwatch);
            var installedPrograms = installedProgramsDetector.GetInstalledPrograms();

            if (installedPrograms.Count > 0)
            {
                foreach (var installedProgram in installedPrograms)
                {
                    logger.LogDebug("Have desktop icon {installedProgram}.", installedProgram.ToString());
                }

                return 1;
            }
            // Unreachable for now.

            var iconDetector = new DesktopIconDetector(Setup.GetLogger("DesktopIconDetector"), Setup.Stopwatch);
            var desktopIcons = iconDetector.GetDesktopIcons();

            if (desktopIcons.Count > 0)
            {
                foreach(var desktopIcon in desktopIcons)
                {
                    logger.LogDebug("Have desktop icon {desktopIcon}.", desktopIcon.ToString());
                }

                return 1;
            }
            // Unreachable for now.

            var envDetector = new OpenWindowDetector(Setup.GetLogger("OpenWindowDetector"), Setup.Stopwatch);
            var activeWindow = envDetector.GetActiveWindow();
            logger.LogDebug("Foreground window is {windowName}.", activeWindow.ToString());

            var openWindows = envDetector.GetOpenedWindows();

            if (openWindows.Count > 0)
            {
                foreach(var openWindow in openWindows)
                {
                    logger.LogDebug("Have open window {windowName}.", openWindow.ToString());
                }
                return 1;
            }
            // Unreachable for now.

            // Launch the actual program.
            logger.LogDebug("Call the actual comparison.");

            var artifactFound = Setup.ArtifactDetector.FindArtifact(Setup);

            return artifactFound.ArtifactFound ? 0 : 1;
        }
    }
}
