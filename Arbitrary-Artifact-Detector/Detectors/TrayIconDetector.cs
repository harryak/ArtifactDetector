using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect desktop icons.
    /// </summary>
    internal class TrayIconDetector : BaseDetector, IDetector
    {
        private const int BUFFER_SIZE = 0x1000;

        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <param name="previousResponse">Not necessary for this.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Get tray window.
            IntPtr trayWindowHandle = GetSystemTrayHandle();

            // This error is really unlikely.
            if (trayWindowHandle == IntPtr.Zero)
            {
                throw new Exception("System tray is not available.");
            }

            // Count subwindows of desktop => count of icons.
            int iconCount = (int) NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            string currentIconTitle;

            // Get the desktop window's process to enumerate child windows.
            NativeMethods.GetWindowThreadProcessId(trayWindowHandle, out uint ProcessId);
            IntPtr trayProcessHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, ProcessId);
            IntPtr bufferPointer = NativeMethods.VirtualAllocEx(trayProcessHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);
            NativeMethods.TBBUTTON currentTrayIcon = new NativeMethods.TBBUTTON();

            try
            {
                // Loop through available tray icons.
                for (int i = 0; i < iconCount; i++)
                {
                    // Initialize buffer for current icon.
                    byte[] vBuffer = new byte[BUFFER_SIZE];
                    uint bytesRead = 0;

                    // Get TBBUTTON struct filled.
                    NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.GETBUTTON, new IntPtr(i), bufferPointer);
                    NativeMethods.ReadProcessMemory(trayProcessHandle, bufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr((uint) Marshal.SizeOf(currentTrayIcon)), ref bytesRead);

                    // This error is really unlikely.
                    if (bytesRead != Marshal.SizeOf(currentTrayIcon))
                    {
                        throw new Exception("Read false amount of bytes.");
                    }
                    // Get actual struct filled from buffer.
                    currentTrayIcon = Marshal.PtrToStructure<NativeMethods.TBBUTTON>(Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0));

                    int titleLength = (int) NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.GETBUTTONTEXTW, new IntPtr(currentTrayIcon.idCommand), bufferPointer);
                    NativeMethods.ReadProcessMemory(trayProcessHandle, bufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr(BUFFER_SIZE), ref bytesRead);
                    currentIconTitle = Marshal.PtrToStringUni(Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), titleLength);

                    // Check if the current title has a substring in the possible titles.
                    if (runtimeInformation.PossibleIconTitles.FirstOrDefault(s => currentIconTitle.Contains(s, StringComparison.InvariantCultureIgnoreCase)) != default(string))
                    {
                        // Memory cleanup in finally clause is always executed.
                        return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
                    }
                }
            }
            finally
            {
                // Clean up unmanaged memory.
                NativeMethods.VirtualFreeEx(trayProcessHandle, bufferPointer, UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(trayProcessHandle);
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        private IntPtr GetSystemTrayHandle()
        {
            IntPtr hWndTray = NativeMethods.FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = NativeMethods.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                        return hWndTray;
                    }
                }
            }

            return IntPtr.Zero;
        }
    }
}