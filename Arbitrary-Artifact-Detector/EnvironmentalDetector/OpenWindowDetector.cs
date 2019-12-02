/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.EnvironmentalDetector.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.EnvironmentalDetector
{
    public class OpenWindowDetector : BaseEnvironmentalDetector, IEnvironmentalDetector
    {
        public OpenWindowDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }

        /// <summary>
        /// Gets the currently active window.
        /// </summary>
        /// <returns>The information of the active window.</returns>
        public WindowInformation GetActiveWindow()
        {
            StartStopwatch();

            IntPtr hWnd = GetForegroundWindow();

            int length = GetWindowTextLength( hWnd );
            StringBuilder builder = new StringBuilder( length );
            GetWindowText(hWnd, builder, length + 1);

            WindowPlacement Placement = new WindowPlacement();
            GetWindowPlacement(hWnd, ref Placement);

            var windowInformation = new WindowInformation
            {
                Handle = hWnd,
                ExecutablePath = new FileInfo(GetProcessPath(hWnd)),
                Title = builder.ToString(),
                Placement = Placement
            };

            StopStopwatch("Got active window in {0} ms.");

            return windowInformation;
        }

        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public IDictionary<IntPtr, WindowInformation> GetOpenedWindows()
        {
            StartStopwatch();

            IntPtr shellWindow = GetShellWindow();
            Dictionary<IntPtr , WindowInformation> windows = new Dictionary<IntPtr , WindowInformation>();

            EnumWindows(
                new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
                {
                    if (hWnd == shellWindow) return true;
                    if (!IsWindowVisible(hWnd)) return true;
                    int length = GetWindowTextLength( hWnd );
                    if (length == 0) return true;

                    StringBuilder builder = new StringBuilder( length );
                    GetWindowText(hWnd, builder, length + 1);

                    WindowPlacement Placement = new WindowPlacement();
                    GetWindowPlacement(hWnd, ref Placement);

                    windows[hWnd] = new WindowInformation
                    {
                        Handle = hWnd,
                        ExecutablePath = new FileInfo(GetProcessPath(hWnd)),
                        Title = builder.ToString(),
                        Placement = Placement
                    };

                    return true;
                }),
                0
            );

            StopStopwatch("Got all opened windows in {0} ms.");

            return windows;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public string GetProcessPath(IntPtr hwnd)
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (hwnd != IntPtr.Zero)
            {
                if (pid != 0)
                {
                    var process = Process.GetProcessById( (int) pid );
                    if (process != null)
                    {
                        return process.MainModule.FileName.ToString();
                    }
                }
            }
            return "";
        }

        #region DLL imports
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        //WARN: Only for "Any CPU":
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);
        #endregion
    }
}
