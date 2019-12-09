using ArbitraryArtifactDetector.EnvironmentalDetector.Models;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.EnvironmentalDetector
{
    class OpenWindowDetector : BaseEnvironmentalDetector, IEnvironmentalDetector
    {
        public OpenWindowDetector(ILogger logger, VADStopwatch stopwatch = null) : base(logger, stopwatch) { }

        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public IDictionary<IntPtr, WindowToplevelInformation> GetOpenedWindows()
        {
            StartStopwatch();
            Dictionary<IntPtr, WindowToplevelInformation> windows = new Dictionary<IntPtr , WindowToplevelInformation>();

            // Use simple counting index as the windows' z-index, as EnumWindows sorts them by it.
            int i = 0;
            WindowToplevelInformation currentWindow;
            EnumWindows(
                new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
                {
                    // If the window is invisible or has no title, skip.
                    if (!IsWindowVisible(hWnd)) return true;
                    int length = GetWindowTextLength(hWnd);
                    if (length == 0) return true;

                    // Get window title into string builder.
                    StringBuilder titleStringBuilder = new StringBuilder(length);
                    GetWindowText(hWnd, titleStringBuilder, length + 1);

                    // Get all placement information we can get from user32.dll
                    WindowPlacement Placement = new WindowPlacement();
                    GetWindowPlacement(hWnd, ref Placement);
                    WindowVisualInformation visualInformation = new WindowVisualInformation();
                    GetWindowInfo(hWnd, ref visualInformation);

                    // Get the current window's information.
                    currentWindow = new WindowToplevelInformation
                    {
                        ExecutablePath = new FileInfo(GetProcessPath(hWnd)),
                        Handle = hWnd,
                        Placement = Placement,
                        Title = titleStringBuilder.ToString(),
                        Visibility = 100f,
                        VisualInformation = visualInformation,
                        ZIndex = i
                    };

                    // If it is not the first/topmost (visible) window...
                    if (i > 0)
                    {
                        // ..get the current's window visibility percentage.
                        currentWindow.Visibility = CalculateWindowVisibility(windows, currentWindow);
                    }

                    // Add the current window to all windows.
                    windows[hWnd] = currentWindow;

                    // Increase the z-index if we got here.
                    i++;

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

        public override DetectorResponse FindArtifact(Setup setup)
        {
            throw new NotImplementedException();
        }

        private int CalculateOverlappingArea(WindowToplevelInformation window, WindowToplevelInformation overlappingWindow)
        {
            return Math.Max(
                0,
                Math.Min(window.VisualInformation.rcClient.Right, overlappingWindow.VisualInformation.rcClient.Right)
                    - Math.Max(window.VisualInformation.rcClient.Left, overlappingWindow.VisualInformation.rcClient.Left)
            ) * Math.Max(
                0,
                Math.Min(window.VisualInformation.rcClient.Bottom, overlappingWindow.VisualInformation.rcClient.Bottom)
                    - Math.Max(window.VisualInformation.rcClient.Top, overlappingWindow.VisualInformation.rcClient.Top)
            );
        }

        private float CalculateWindowVisibility(Dictionary<IntPtr, WindowToplevelInformation> windowsAbove, WindowToplevelInformation queriedWindow)
        {
            // If there is no area of the window, return "no visibility".
            if (queriedWindow.VisualInformation.rcClient.Area < 1)
                return 0f;

            // Accumulator for total not viewable area that gets subtracted later.
            float subtractArea = 0f;

            // Loop through all windows "above" to look if one window overlaps the queried window.
            foreach (KeyValuePair<IntPtr, WindowToplevelInformation> windowAboveEntry in windowsAbove)
            {
                subtractArea += CalculateOverlappingArea(queriedWindow, windowAboveEntry.Value);

                if (subtractArea >= queriedWindow.VisualInformation.rcClient.Area)
                {
                    return 0f;
                }
            }
            
            if (subtractArea == 0)
                return 100f;

            return ((queriedWindow.VisualInformation.rcClient.Area - subtractArea) / (float) queriedWindow.VisualInformation.rcClient.Area) * 100f;
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

        //WARN: Only for "Any CPU":
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref WindowVisualInformation pwi);
        #endregion
    }
}
