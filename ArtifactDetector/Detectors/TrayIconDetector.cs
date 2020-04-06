using System;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect tray icons.
    /// </summary>
    internal class TrayIconDetector : IconDetector<NativeMethods.TBBUTTON>
    {
        public TrayIconDetector() : base(NativeMethods.TB.GETBUTTON, NativeMethods.TB.BUTTONCOUNT, NativeMethods.TB.GETBUTTONTEXTW, instance => new IntPtr(instance.idCommand))
        {
        }

        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <param name="previousResponse">Not necessary for this.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Get tray window.
            var trayWindowHandle = GetSystemTrayHandle();

            // This error is really unlikely.
            if (trayWindowHandle == IntPtr.Zero)
            {
                StopStopwatch("Got tray icons in {0}ms.");
                Logger.LogError("The system tray handle is not available.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }

            return FindIcon(trayWindowHandle, ref runtimeInformation);
        }

        /// <summary>
        /// Get the system tray window's handle.
        /// </summary>
        /// <returns>The handle if found or IntPtr.Zero if not.</returns>
        private IntPtr GetSystemTrayHandle()
        {
            return GetWindowHandle(new string[] { "Shell_TrayWnd", "TrayNotifyWnd", "SysPager", "ToolbarWindow32" });
        }
    }
}
