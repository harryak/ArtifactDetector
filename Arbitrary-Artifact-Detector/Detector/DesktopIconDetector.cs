using ArbitraryArtifactDetector.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.Detector
{
    class DesktopIconDetector : BaseDetector, IDetector
    {
        public DesktopIconDetector(Setup setup) : base(setup) { }

        public IDictionary<int, DesktopIcon> GetDesktopIcons()
        {
            StartStopwatch();

            Dictionary<int , DesktopIcon> icons = new Dictionary<int , DesktopIcon>();

            IntPtr desktopHandle = NativeMethods.FindWindow("Progman", "Program Manager");
            desktopHandle = NativeMethods.FindWindowEx(desktopHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            desktopHandle = NativeMethods.FindWindowEx(desktopHandle, IntPtr.Zero, "SysListView32", "FolderView");

            int iconCount = NativeMethods.SendMessage(desktopHandle, LVM_GETITEMCOUNT, 0, 0);

            NativeMethods.GetWindowThreadProcessId(desktopHandle, out uint vProcessId);

            IntPtr vProcess = NativeMethods.OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ |
                    PROCESS_VM_WRITE, false, vProcessId);

            IntPtr vPointer = NativeMethods.VirtualAllocEx(vProcess, IntPtr.Zero, 4096,
                    MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

            try
            {
                for (int i = 0; i < iconCount; i++)
                {
                    byte[] vBuffer = new byte[256];

                    NativeMethods.LVITEM[] vItem = new NativeMethods.LVITEM[1];
                    vItem[0].mask = LVIF_TEXT;
                    vItem[0].iItem = i;
                    vItem[0].iSubItem = 0;
                    vItem[0].cchTextMax = vBuffer.Length;
                    vItem[0].pszText = (IntPtr) ((int) vPointer + Marshal.SizeOf(typeof(NativeMethods.LVITEM)));

                    uint vNumberOfBytesRead = 0;

                    NativeMethods.WriteProcessMemory(vProcess, vPointer,
                            Marshal.UnsafeAddrOfPinnedArrayElement(vItem, 0),
                            Marshal.SizeOf(typeof(NativeMethods.LVITEM)), ref vNumberOfBytesRead);

                    NativeMethods.SendMessage(desktopHandle, LVM_GETITEMW, i, vPointer.ToInt32());

                    NativeMethods.ReadProcessMemory(vProcess,
                            (IntPtr) ((int) vPointer + Marshal.SizeOf(typeof(NativeMethods.LVITEM))),
                            Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0),
                            vBuffer.Length, ref vNumberOfBytesRead);

                    string iconName = Encoding.Unicode.GetString(vBuffer, 0,
                            (int)vNumberOfBytesRead);
                    iconName = iconName.Substring(0, iconName.IndexOf('\0'));

                    NativeMethods.SendMessage(desktopHandle, LVM_GETITEMPOSITION, i, vPointer.ToInt32());

                    Point[] vPoint = new Point[1];
                    NativeMethods.ReadProcessMemory(vProcess, vPointer,
                            Marshal.UnsafeAddrOfPinnedArrayElement(vPoint, 0),
                            Marshal.SizeOf(typeof(Point)), ref vNumberOfBytesRead);

                    icons.Add(i, new DesktopIcon(iconName, vPoint[0]));
                }
            }
            finally
            {
                NativeMethods.VirtualFreeEx(vProcess, vPointer, 0, MEM_RELEASE);
                NativeMethods.CloseHandle(vProcess);
            }

            StopStopwatch("Got all opened windows in {0} ms.");

            return icons;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            throw new NotImplementedException();
        }

        #region DLL imports

        internal class NativeMethods
        {
            [DllImport("user32.DLL")]
            internal static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

            [DllImport("user32.DLL")]
            internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [DllImport("user32.dll")]
            internal static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

            [DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint dwProcessId);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

            [DllImport("kernel32.dll")]
            internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

            [DllImport("kernel32.dll")]
            internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

            [DllImport("kernel32.dll")]
            internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);

            [DllImport("kernel32.dll")]
            internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);

            [DllImport("kernel32.dll")]
            internal static extern bool CloseHandle(IntPtr handle);

            internal struct LVITEM
            {
                public int mask;
                public int iItem;
                public int iSubItem;
                public int state;
                public int stateMask;
                public IntPtr pszText; // string
                public int cchTextMax;
                public int iImage;
                public IntPtr lParam;
                public int iIndent;
                public int iGroupId;
                public int cColumns;
                public IntPtr puColumns;
            }
        }

        #endregion

        #region Windows Messages

        private const uint LVM_FIRST = 0x1000;
        private const uint LVM_GETITEMCOUNT = LVM_FIRST + 4;
        private const uint LVM_GETITEMW = LVM_FIRST + 75;
        private const uint LVM_GETITEMPOSITION = LVM_FIRST + 16;
        private const uint LVM_SETITEMPOSITION = LVM_FIRST + 15;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 4;
        private const int LVIF_TEXT = 0x0001;

        #endregion
    }
}
