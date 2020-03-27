using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect desktop icons.
    /// </summary>
    internal class DesktopIconDetector : BaseDetector, IDetector
    {
        private const int BUFFER_SIZE = 0x110;

        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <param name="previousResponse">Not necessary for this.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Find window handles via process names.
            var desktopWindowHandle = GetDesktopHandle();

            // This error is really unlikely.
            if (desktopWindowHandle == IntPtr.Zero)
            {
                throw new Exception("Desktop is not available.");
            }

            // Count subwindows of desktop => count of icons.
            int iconCount = (int) NativeMethods.SendMessage(desktopWindowHandle, NativeMethods.LVM.GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            string currentIconTitle;

            // Get the desktop window's process to enumerate child windows.
            NativeMethods.GetWindowThreadProcessId(desktopWindowHandle, out uint desktopProcessId);
            var desktopProcessHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, desktopProcessId);

            // Allocate memory in the desktop process.
            var bufferPointer = NativeMethods.VirtualAllocEx(desktopProcessHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);

            // Initialize loop variables.
            var currentDesktopIcon = new NativeMethods.LVITEMA();
            byte[] vBuffer = new byte[BUFFER_SIZE];
            uint bytesRead = 0;

            // Instantiate an item to get to the remote buffer and be filled there.
            NativeMethods.LVITEMA[] remoteBufferDesktopIcon = new NativeMethods.LVITEMA[1];

            // Initialize basic structure.
            // We want to get the icon's text, so set the mask accordingly.
            remoteBufferDesktopIcon[0].mask = NativeMethods.LVIF.TEXT;

            // Set maximum text length to buffer length minus offset used in pszText.
            remoteBufferDesktopIcon[0].cchTextMax = vBuffer.Length - Marshal.SizeOf(typeof(NativeMethods.LVITEMA));

            // Set pszText at point in the remote process's buffer.
            remoteBufferDesktopIcon[0].pszText = (IntPtr)((int)bufferPointer + Marshal.SizeOf(typeof(NativeMethods.LVITEMA)));

            try
            {
                // Loop through available desktop icons.
                for (int i = 0; i < iconCount; i++)
                {
                    remoteBufferDesktopIcon[0].iItem = i;

                    // Write to desktop process the structure we want to get.
                    NativeMethods.WriteProcessMemory(desktopProcessHandle, bufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(remoteBufferDesktopIcon, 0), new UIntPtr((uint)Marshal.SizeOf(typeof(NativeMethods.LVITEMA))), ref bytesRead);

                    // Get i-th item of desktop and read its memory.
                    NativeMethods.SendMessage(desktopWindowHandle, NativeMethods.LVM.GETITEMW, new IntPtr(i), bufferPointer);
                    NativeMethods.ReadProcessMemory(desktopProcessHandle, bufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr((uint)Marshal.SizeOf(currentDesktopIcon)), ref bytesRead);

                    // This error is really unlikely.
                    if (bytesRead != Marshal.SizeOf(currentDesktopIcon))
                    {
                        throw new Exception("Read false amount of bytes.");
                    }

                    // Get actual struct filled from buffer.
                    currentDesktopIcon = Marshal.PtrToStructure<NativeMethods.LVITEMA>(Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0));

                    // Use the now set pszText pointer to read the icon text into the buffer. Maximum length is 260, more characters won't be displayed.
                    NativeMethods.ReadProcessMemory(desktopProcessHandle, currentDesktopIcon.pszText, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr(260), ref bytesRead);

                    // Read from buffer into string with unicode encoding, then trim string.
                    currentIconTitle = Encoding.Unicode.GetString(vBuffer, 0, (int)bytesRead);
                    currentIconTitle = currentIconTitle.Substring(0, currentIconTitle.IndexOf('\0'));

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
                NativeMethods.VirtualFreeEx(desktopProcessHandle, bufferPointer, UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(desktopProcessHandle);
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Get the desktop window's handle.
        /// </summary>
        /// <returns>The handle if found or IntPtr.Zero if not.</returns>
        private IntPtr GetDesktopHandle()
        {
            // Get desktop window via program manager.
            var hWndPDesktop = NativeMethods.FindWindow("Progman", "Program Manager");
            if (hWndPDesktop != IntPtr.Zero)
            {
                hWndPDesktop = NativeMethods.FindWindowEx(hWndPDesktop, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (hWndPDesktop != IntPtr.Zero)
                {
                    hWndPDesktop = NativeMethods.FindWindowEx(hWndPDesktop, IntPtr.Zero, "SysListView32", "FolderView");
                    return hWndPDesktop;
                }
            }
            return IntPtr.Zero;
        }
    }
}
