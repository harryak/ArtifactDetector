using ArbitraryArtifactDetector.Models;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect desktop icons.
    /// </summary>
    internal class DesktopIconDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <param name="previousResponse">Not necessary for this.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Get desktop window via program manager.
            IntPtr desktopHandle = NativeMethods.FindWindow("Progman", "Program Manager");
            desktopHandle = NativeMethods.FindWindowEx(desktopHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            desktopHandle = NativeMethods.FindWindowEx(desktopHandle, IntPtr.Zero, "SysListView32", "FolderView");

            // Count subwindows of desktop => count of icons.
            int iconCount = NativeMethods.SendMessage(desktopHandle, NativeMethods.LVM.GETITEMCOUNT, IntPtr.Zero, IntPtr.Zero);
            string currentIconName;

            // Get the desktop window's process to enumerate child windows.
            NativeMethods.GetWindowThreadProcessId(desktopHandle, out uint vProcessId);
            IntPtr vProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, vProcessId);
            IntPtr vPointer = NativeMethods.VirtualAllocEx(vProcess, IntPtr.Zero, 4096, NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);

            try
            {
                // Loop through available desktop icons.
                for (int i = 0; i < iconCount; i++)
                {
                    byte[] vBuffer = new byte[256];

                    NativeMethods.LVITEM[] vItem = new NativeMethods.LVITEM[1];
                    vItem[0].mask = NativeMethods.LVIF.TEXT;
                    vItem[0].iItem = i;
                    vItem[0].iSubItem = 0;
                    vItem[0].cchTextMax = vBuffer.Length;
                    vItem[0].pszText = vPointer + Marshal.SizeOf(typeof(NativeMethods.LVITEM));

                    uint vNumberOfBytesRead = 0;

                    NativeMethods.WriteProcessMemory(vProcess, vPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vItem, 0), Marshal.SizeOf(typeof(NativeMethods.LVITEM)), ref vNumberOfBytesRead);
                    NativeMethods.SendMessage(desktopHandle, NativeMethods.LVM.GETITEMW, new IntPtr(i), vPointer);
                    NativeMethods.ReadProcessMemory(vProcess, vPointer + Marshal.SizeOf(typeof(NativeMethods.LVITEM)), Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), vBuffer.Length, ref vNumberOfBytesRead);

                    // Read icon title in unicode until end of string.
                    currentIconName = Encoding.Unicode.GetString(vBuffer, 0, (int)vNumberOfBytesRead);
                    currentIconName = currentIconName.Substring(0, currentIconName.IndexOf('\0'));

                    // Check if the current title has a substring in the possible titles.
                    if (runtimeInformation.PossibleIconTitles.FirstOrDefault(s => currentIconName.Contains(s)) != default(string))
                    {
                        // Clean up unmanaged memory.
                        NativeMethods.VirtualFreeEx(vProcess, vPointer, 0, NativeMethods.MEM.RELEASE);
                        NativeMethods.CloseHandle(vProcess);

                        return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
                    }
                }
            }
            finally
            {
                // Clean up unmanaged memory.
                NativeMethods.VirtualFreeEx(vProcess, vPointer, 0, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(vProcess);
            }

            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        #region DLL imports

        internal class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern bool CloseHandle(IntPtr handle);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint dwProcessId);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32.dll")]
            internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, [MarshalAs(UnmanagedType.SysInt)] int nSize, ref uint vNumberOfBytesRead);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.SysInt)]
            internal static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, [MarshalAs(UnmanagedType.SysUInt)] uint dwSize, uint flAllocationType, uint flProtect);

            [DllImport("kernel32.dll")]
            internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, [MarshalAs(UnmanagedType.SysUInt)] uint dwSize, uint dwFreeType);

            [DllImport("kernel32.dll")]
            internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, [MarshalAs(UnmanagedType.SysInt)] int nSize, ref uint vNumberOfBytesRead);

#pragma warning disable CS0649
            internal struct LVITEM
            {
                public int cchTextMax;
                public int cColumns;
                public int iGroupId;
                public int iImage;
                public int iIndent;
                public int iItem;
                public int iSubItem;
                public IntPtr lParam;
                public int mask;
                public IntPtr pszText;

                // string
                public IntPtr puColumns;

                public int state;
                public int stateMask;
            }
#pragma warning restore CS0649

            #region Windows Messages

            internal class LVIF
            {
                public const int TEXT = 0x0001;
            }

            internal class LVM
            {
                public const uint FIRST = 0x1000;
                public const uint GETITEMCOUNT    = FIRST + 4;
                public const uint GETITEMPOSITION = FIRST + 16;
                public const uint GETITEMW        = FIRST + 75;
                public const uint SETITEMPOSITION = FIRST + 15;
            }

            internal class MEM
            {
                public const uint COMMIT  = 0x1000;
                public const uint FREE    = 0x10000;
                public const uint RELEASE = 0x8000;
                public const uint RESERVE = 0x2000;
            }

            internal class PAGE
            {
                public const uint READWRITE = 4;
            }

            internal class PROCESS_VM
            {
                public const uint OPERATION = 0x0008;
                public const uint READ      = 0x0010;
                public const uint WRITE     = 0x0020;
            }

            internal class WM
            {
                public const uint USER = 0x0400;
            }

            #endregion Windows Messages
        }

        #endregion DLL imports
    }
}
 