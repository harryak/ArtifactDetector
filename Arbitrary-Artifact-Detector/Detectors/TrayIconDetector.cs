using ItsApe.ArtifactDetector.Models;
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
        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <param name="previousResponse">Not necessary for this.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            const int BUFFER_SIZE = 0x1000;

            // Get tray window.
            IntPtr trayWindowHandle = GetSystemTrayHandle();

            // This shouldn't happen during a normal system boot, but well.
            if (trayWindowHandle == IntPtr.Zero)
            {
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            // Count subwindows of desktop => count of icons.
            int iconCount = (int) NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
            string currentIconTitle;

            // Get the desktop window's process to enumerate child windows.
            NativeMethods.GetWindowThreadProcessId(trayWindowHandle, out uint ProcessId);
            IntPtr ProcessHandle = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, ProcessId);
            IntPtr BufferPointer = NativeMethods.VirtualAllocEx(ProcessHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);
            NativeMethods.TBBUTTON currentTrayIcon = new NativeMethods.TBBUTTON();

            try
            {
                // Loop through available desktop icons.
                for (int i = 0; i < iconCount; i++)
                {
                    // Initialize buffer for current icon.
                    byte[] vBuffer = new byte[BUFFER_SIZE];
                    uint vNumberOfBytesRead = 0;

                    // Get TBBUTTON struct filled.
                    NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.GETBUTTON, new IntPtr(i), BufferPointer);
                    NativeMethods.ReadProcessMemory(ProcessHandle, BufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr((uint) Marshal.SizeOf(currentTrayIcon)), ref vNumberOfBytesRead);

                    // This error is really unlikely.
                    if (vNumberOfBytesRead != Marshal.SizeOf(currentTrayIcon))
                    {
                        throw new Exception("Read false amount of bytes.");
                    }
                    // Get actual struct filled from buffer.
                    currentTrayIcon = Marshal.PtrToStructure<NativeMethods.TBBUTTON>(Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0));

                    int titleLength = ( int ) NativeMethods.SendMessage(trayWindowHandle, NativeMethods.TB.GETBUTTONTEXTW, new IntPtr(currentTrayIcon.idCommand), BufferPointer);

                    NativeMethods.ReadProcessMemory(ProcessHandle, BufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), new UIntPtr(BUFFER_SIZE), ref vNumberOfBytesRead);

                    currentIconTitle = Marshal.PtrToStringUni(Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0), titleLength);

                    // Check if the current title has a substring in the possible titles.
                    if (runtimeInformation.PossibleIconTitles.FirstOrDefault(s => currentIconTitle.Contains(s)) != default(string))
                    {
                        // Clean up unmanaged memory.
                        NativeMethods.VirtualFreeEx(ProcessHandle, BufferPointer, UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                        NativeMethods.CloseHandle(ProcessHandle);

                        return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
                    }
                }
            }
            finally
            {
                // Clean up unmanaged memory.
                NativeMethods.VirtualFreeEx(ProcessHandle, BufferPointer, UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(ProcessHandle);
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
            internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UIntPtr nSize, ref uint vNumberOfBytesRead);

            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

            [DllImport("kernel32.dll")]
            internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

            [DllImport("kernel32.dll")]
            internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UIntPtr nSize, ref uint vNumberOfBytesRead);

#pragma warning disable IDE1006

            [StructLayout(LayoutKind.Sequential)]
            internal struct TBBUTTON
            {
                public int iBitmap;
                public int idCommand;

                [StructLayout(LayoutKind.Explicit)]
                private struct TBBUTTON_U
                {
                    [FieldOffset(0)] public byte fsState;
                    [FieldOffset(1)] public byte fsStyle;
                    [FieldOffset(0)] private readonly IntPtr bReserved;
                }

                private TBBUTTON_U union;
                public byte fsState { get { return union.fsState; } set { union.fsState = value; } }
                public byte fsStyle { get { return union.fsStyle; } set { union.fsStyle = value; } }
                public UIntPtr dwData;
                public IntPtr iString;
            }

#pragma warning restore IDE1006

            #region Windows Messages

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

            internal class TB
            {
                public const uint BUTTONCOUNT    = WM.USER + 24;
                public const uint GETBUTTON      = WM.USER + 23;
                public const uint GETBUTTONTEXTA = WM.USER + 45;
                public const uint GETBUTTONTEXTW = WM.USER + 75;
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