using ArbitraryArtifactDetector.Model;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// Detector to detect if a window is opened and visible.
    ///
    /// Needs:  WindowHandle _or_ WindowTitle
    /// Yields: WindowHandle, WindowTitle
    /// </summary>
    internal class OpenWindowDetector : BaseDetector, IDetector
    {
        /// <summary>
        /// Delegate function to loop over windows.
        /// </summary>
        /// <param name="hWnd">Input window handle.</param>
        /// <param name="lParam">Parameters for the current window.</param>
        /// <returns>Can be disregarded.</returns>
        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <param name="previousResponse">Optional: Response from another detector run before.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation, DetectorResponse previousResponse = null)
        {
            // Stopwatch for evaluation.
            StartStopwatch();

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.MatchingWindowsInformation.Count < 1 && runtimeInformation.PossibleWindowTitles.Count < 1)
            {
                throw new ArgumentException("Neither window handle nor window title given.");
            }

            // Copy to local variables for EnumWindowsProc.
            ICollection<IntPtr> windowHandles = runtimeInformation.MatchingWindowsInformation.Keys;
            IList<string> possibleWindowTitles = runtimeInformation.PossibleWindowTitles;

            // Initialize list of windows for later use.
            IList<WindowToplevelInformation> windows = new List<WindowToplevelInformation>();
            IDictionary<IntPtr, WindowToplevelInformation> matchingWindows = new Dictionary<IntPtr, WindowToplevelInformation>();

            // Use simple counting index as the windows' z-index, as EnumWindows sorts them by it.
            int i = 0;
            WindowToplevelInformation currentWindow;
            StringBuilder titleStringBuilder;
            string currentWindowTitle;

            bool enoughWindowsFound = false;

            NativeMethods.EnumWindows(
                new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
                {
                    // If the window is invisible, skip.
                    if (enoughWindowsFound || !NativeMethods.IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    // Some windows have no title, so make sure we don't access the title if it is not there.
                    currentWindowTitle = "";
                    int titleLength = NativeMethods.GetWindowTextLength(hWnd);
                    if (titleLength != 0)
                    {
                        // Get window title into string builder.
                        titleStringBuilder = new StringBuilder(titleLength);
                        NativeMethods.GetWindowText(hWnd, titleStringBuilder, titleLength + 1);
                        currentWindowTitle = titleStringBuilder.ToString();
                    }

                    // Get all placement information we can get from user32.dll
                    WindowPlacement Placement = new WindowPlacement();
                    NativeMethods.GetWindowPlacement(hWnd, ref Placement);
                    WindowVisualInformation visualInformation = new WindowVisualInformation();
                    NativeMethods.GetWindowInfo(hWnd, ref visualInformation);

                    // Get the current window's information.
                    currentWindow = new WindowToplevelInformation
                    {
                        Handle = hWnd,
                        Placement = Placement,
                        Title = currentWindowTitle,
                        Visibility = 100f,
                        VisualInformation = visualInformation,
                        ZIndex = i
                    };

                    bool windowMatches = possibleWindowTitles.Contains(currentWindowTitle) || windowHandles.Contains(hWnd);

                    // If it is not the first/topmost (visible) window and one of the windows we want to find...
                    if (i > 0 && windowMatches)
                    {
                        // ...get the current's window visibility percentage.
                        currentWindow.Visibility = CalculateWindowVisibility(windows, currentWindow);
                    }

                    // Add the current window to all windows.
                    windows.Add(currentWindow);

                    // If it is one of the windows we want to find: Add to that list.
                    if (windowMatches)
                    {
                        matchingWindows.Add(hWnd, currentWindow);

                        // Check if we found enough of the windows.
                        if (matchingWindows.Count >= windowHandles.Count)
                        {
                            enoughWindowsFound = true;
                        }
                    }

                    // Increase the z-index if we got here.
                    i++;

                    return true;
                }),
                0
            );

            // If we found not a single matching window the artifact can't be present.
            if (matchingWindows.Count < 1)
            {
                StopStopwatch("Got all opened windows in {0}ms.");
                return new DetectorResponse() { ArtifactPresent = false, Certainty = 100 };
            }

            runtimeInformation.MatchingWindowsInformation = matchingWindows;

            int certainty = 100 / matchingWindows.Count;
            StopStopwatch("Got all opened windows in {0}ms.");
            return new DetectorResponse() { ArtifactPresent = true, Certainty = certainty };
        }

        /// <summary>
        /// Calculate the area of intersection of two window rectangles.
        /// </summary>
        /// <param name="window">The window below.</param>
        /// <param name="overlappingWindow">The window above.</param>
        /// <returns>The area of the intersection.</returns>
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

        /// <summary>
        /// Calculates how much (percentage) of the queriedWindow is visible other windows above.
        /// </summary>
        /// <param name="windowsAbove">The windows above (z-index) the queried window.</param>
        /// <param name="queriedWindow">Queried window.</param>
        /// <returns>The percentage of how much of the window is visible.</returns>
        private float CalculateWindowVisibility(IList<WindowToplevelInformation> windowsAbove, WindowToplevelInformation queriedWindow)
        {
            // If there is no area of the window, return "no visibility".
            if (queriedWindow.VisualInformation.rcClient.Area < 1)
                return 0f;

            // Accumulator for total not viewable area that gets subtracted later.
            float subtractArea = 0f;

            // Loop through all windows "above" to look if one window overlaps the queried window.
            foreach (WindowToplevelInformation windowAboveEntry in windowsAbove)
            {
                subtractArea += CalculateOverlappingArea(queriedWindow, windowAboveEntry);

                if (subtractArea >= queriedWindow.VisualInformation.rcClient.Area)
                {
                    return 0f;
                }
            }

            if (subtractArea == 0)
                return 100f;

            return ((queriedWindow.VisualInformation.rcClient.Area - subtractArea) / queriedWindow.VisualInformation.rcClient.Area) * 100f;
        }

        /// <summary>
        /// Returns a dictionary that contains information of all the open windows.
        /// </summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        private IList<WindowToplevelInformation> GetOpenedWindows()
        {
            // Stopwatch for evaluation.
            StartStopwatch();

            // Initialize list of windows for later use.
            IList<WindowToplevelInformation> windows = new List<WindowToplevelInformation>();

            // Use simple counting index as the windows' z-index, as EnumWindows sorts them by it.
            int i = 0;
            WindowToplevelInformation currentWindow;
            StringBuilder titleStringBuilder;
            string windowTitle;

            NativeMethods.EnumWindows(
                new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
                {
                    // If the window is invisible, skip.
                    if (!NativeMethods.IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    // Some windows have no title, so make sure we don't access one if it is not there.
                    windowTitle = "";
                    int length = NativeMethods.GetWindowTextLength(hWnd);
                    if (length != 0)
                    {
                        // Get window title into string builder.
                        titleStringBuilder = new StringBuilder(length);
                        NativeMethods.GetWindowText(hWnd, titleStringBuilder, length + 1);
                        windowTitle = titleStringBuilder.ToString();
                    }

                    // Get all placement information we can get from user32.dll
                    WindowPlacement Placement = new WindowPlacement();
                    NativeMethods.GetWindowPlacement(hWnd, ref Placement);
                    WindowVisualInformation visualInformation = new WindowVisualInformation();
                    NativeMethods.GetWindowInfo(hWnd, ref visualInformation);

                    // Get the current window's information.
                    currentWindow = new WindowToplevelInformation
                    {
                        Handle = hWnd,
                        Placement = Placement,
                        Title = windowTitle,
                        Visibility = 100f,
                        VisualInformation = visualInformation,
                        ZIndex = i
                    };

                    // If it is not the first/topmost (visible) window...
                    if (i > 0)
                    {
                        // ...get the current's window visibility percentage.
                        currentWindow.Visibility = CalculateWindowVisibility(windows, currentWindow);
                    }

                    // Add the current window to all windows.
                    windows.Add(currentWindow);

                    // Increase the z-index if we got here.
                    i++;

                    return true;
                }),
                0
            );

            StopStopwatch("Got all opened windows in {0}ms.");

            return windows;
        }

        #region DLL imports

        private class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowVisualInformation pwi);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

            [DllImport("user32.dll")]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);
        }

        #endregion DLL imports
    }
}