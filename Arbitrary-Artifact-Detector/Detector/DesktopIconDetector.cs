using ArbitraryArtifactDetector.EnvironmentalDetector.Models;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.Detector
{
    class DesktopIconDetector : BaseDetector, IDetector
    {
        public DesktopIconDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }

        public IDictionary<int, DesktopIcon> GetDesktopIcons()
        {
            StartStopwatch();

            Dictionary<int , DesktopIcon> icons = new Dictionary<int , DesktopIcon>();

            IntPtr desktopHandle = FindWindow("Progman", "Program Manager");
            desktopHandle = FindWindowEx(desktopHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            desktopHandle = FindWindowEx(desktopHandle, IntPtr.Zero, "SysListView32", "FolderView");

            int iconCount = SendMessage(desktopHandle, LVM_GETITEMCOUNT, 0, 0);

            GetWindowThreadProcessId(desktopHandle, out uint vProcessId);

            IntPtr vProcess = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ |
                    PROCESS_VM_WRITE, false, vProcessId);

            IntPtr vPointer = VirtualAllocEx(vProcess, IntPtr.Zero, 4096,
                    MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

            try
            {
                for (int i = 0; i < iconCount; i++)
                {
                    byte[] vBuffer = new byte[256];

                    LVITEM[] vItem = new LVITEM[1];
                    vItem[0].mask = LVIF_TEXT;
                    vItem[0].iItem = i;
                    vItem[0].iSubItem = 0;
                    vItem[0].cchTextMax = vBuffer.Length;
                    vItem[0].pszText = (IntPtr) ((int) vPointer + Marshal.SizeOf(typeof(LVITEM)));

                    uint vNumberOfBytesRead = 0;

                    WriteProcessMemory(vProcess, vPointer,
                            Marshal.UnsafeAddrOfPinnedArrayElement(vItem, 0),
                            Marshal.SizeOf(typeof(LVITEM)), ref vNumberOfBytesRead);

                    SendMessage(desktopHandle, LVM_GETITEMW, i, vPointer.ToInt32());

                    ReadProcessMemory(vProcess,
                            (IntPtr) ((int) vPointer + Marshal.SizeOf(typeof(LVITEM))),
                            Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer, 0),
                            vBuffer.Length, ref vNumberOfBytesRead);

                    string iconName = Encoding.Unicode.GetString(vBuffer, 0,
                            (int)vNumberOfBytesRead);
                    iconName = iconName.Substring(0, iconName.IndexOf('\0'));

                    SendMessage(desktopHandle, LVM_GETITEMPOSITION, i, vPointer.ToInt32());

                    Point[] vPoint = new Point[1];
                    ReadProcessMemory(vProcess, vPointer,
                            Marshal.UnsafeAddrOfPinnedArrayElement(vPoint, 0),
                            Marshal.SizeOf(typeof(Point)), ref vNumberOfBytesRead);

                    icons.Add(i, new DesktopIcon(iconName, vPoint[0]));
                }
            }
            finally
            {
                VirtualFreeEx(vProcess, vPointer, 0, MEM_RELEASE);
                CloseHandle(vProcess);
            }

            StopStopwatch("Got all opened windows in {0} ms.");

            return icons;
        }

        public override DetectorResponse FindArtifact(Setup setup)
        {
            throw new NotImplementedException();
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        #region DLL imports

        [DllImport("user32.DLL")]
        private static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.DLL")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, ref uint vNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
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
