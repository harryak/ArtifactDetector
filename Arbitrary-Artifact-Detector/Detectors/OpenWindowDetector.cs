using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ItsApe.ArtifactDetector.Detectors
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
            IList<Rectangle> windows = new List<Rectangle>();
            IDictionary<IntPtr, WindowToplevelInformation> matchingWindows = new Dictionary<IntPtr, WindowToplevelInformation>();

            // Use simple counting index as the windows' z-index, as EnumWindows sorts them by it.
            int i = 0;
            WindowToplevelInformation currentWindow;
            StringBuilder titleStringBuilder;
            string currentWindowTitle;

            bool enoughWindowsFound = false;

            NativeMethods.EnumWindows(
                new NativeMethods.EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
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
                    NativeMethods.WindowPlacement Placement = new NativeMethods.WindowPlacement();
                    NativeMethods.GetWindowPlacement(hWnd, ref Placement);
                    NativeMethods.WindowVisualInformation visualInformation = new NativeMethods.WindowVisualInformation();
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

                    bool windowMatches = possibleWindowTitles.FirstOrDefault(s => currentWindowTitle.Contains(s)) != default(string) || windowHandles.Contains(hWnd);

                    // If it is not the first/topmost (visible) window and one of the windows we want to find...
                    if (i > 0 && windowMatches)
                    {
                        // ...get the current's window visibility percentage.
                        currentWindow.Visibility = CalculateWindowVisibility(windows, visualInformation.rcClient);
                    }

                    // Add the current window to all windows.
                    windows.Add(visualInformation.rcClient);

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
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }

            runtimeInformation.MatchingWindowsInformation = matchingWindows;

            StopStopwatch("Got all opened windows in {0}ms.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
        }

        /// <summary>
        /// Calculate the area of intersection of two window rectangles.
        /// </summary>
        /// <param name="firstRectangle">The overlapped rectangle.</param>
        /// <param name="secondRectangle">The new rectangle above.</param>
        /// <param name="overlappingRectangles">Other areas within first rectangle that are overlapping.</param>
        /// <returns>The area of the intersection.</returns>
        private int CalculateOverlappingArea(Rectangle boundingRectangle, IList<Rectangle> overlayingRectangles)
        {
            // Perform sweep line algorithm on union of rectangles.
            // Ordered list of X-coordinates and a list of tuples which are "activated" there.
            SortedList<int, List<Tuple<int,int>>> availableAbscissae = new SortedList<int, List<Tuple<int,int>>>();
            // All available Y-coordinates.
            SortedSet<int> availableOrdinates = new SortedSet<int>();

            Rectangle intersectionRectangle;
            foreach (Rectangle currentRectangle in overlayingRectangles)
            {
                // Try to find intersection, throws ArgumentException if there is none.
                try
                {
                    // Get rectangle intersection between overlapping rectangle and bounding box.
                    intersectionRectangle = Intersection(boundingRectangle, currentRectangle);

                    // Add left X-coordinate entry to events list, if necessary.
                    if (!availableAbscissae.ContainsKey(intersectionRectangle.Left))
                    {
                        availableAbscissae.Add(
                            intersectionRectangle.Left,
                            new List<Tuple<int,int>>());
                    }
                    availableAbscissae[intersectionRectangle.Left].Add(
                        new Tuple<int, int>(intersectionRectangle.Top, intersectionRectangle.Bottom));

                    // Add right X-coordinate entry to events list, if necessary.
                    if (!availableAbscissae.ContainsKey(intersectionRectangle.Right))
                    {
                        availableAbscissae.Add(
                            intersectionRectangle.Right,
                            new List<Tuple<int, int>>());
                    }
                    availableAbscissae[intersectionRectangle.Right].Add(
                        new Tuple<int, int>(intersectionRectangle.Top, intersectionRectangle.Bottom));

                    // Add Y-coordinates, if necessary.
                    if (!availableOrdinates.Contains(intersectionRectangle.Top))
                    {
                        availableOrdinates.Add(intersectionRectangle.Top);
                    }
                    if (!availableOrdinates.Contains(intersectionRectangle.Bottom))
                    {
                        availableOrdinates.Add(intersectionRectangle.Bottom);
                    }
                }
                catch (ArgumentException)
                { }
            }

            // Construct segment tree for sweep line algorithm.
            SegmentTree segmentTree = new SegmentTree(availableOrdinates.ToArray());

            // Sweep line over ordered events on X-axis.
            int unionArea = 0;
            int previousAbscissa = -1;
            foreach (var abscissaEvent in availableAbscissae)
            {
                // For all intervals in list of this event: Activate in segment tree.
                foreach (Tuple<int, int> interval in abscissaEvent.Value)
                {
                    segmentTree.ActivateInterval(interval.Item1, interval.Item2);
                }

                // If we are not at the first abscissa:
                if (previousAbscissa > 0)
                {
                    // Add rectangle to total.
                    unionArea += (abscissaEvent.Key - previousAbscissa) * segmentTree.GetActiveLength();
                }

                previousAbscissa = abscissaEvent.Key;
            }

            return boundingRectangle.Area;
        }

        /// <summary>
        /// Calculates how much (percentage) of the queriedWindow is visible other windows above.
        /// </summary>
        /// <param name="windowsAbove">The windows above (z-index) the queried window.</param>
        /// <param name="queriedWindow">Queried window.</param>
        /// <returns>The percentage of how much of the window is visible.</returns>
        private float CalculateWindowVisibility(IList<Rectangle> windowsAbove, Rectangle queriedWindow)
        {
            // If there is no area of the window, return "no visibility".
            if (queriedWindow.Area < 1)
            {
                return 0f;
            }

            int subtractArea = CalculateOverlappingArea(queriedWindow, windowsAbove);

            return ((queriedWindow.Area - subtractArea) / queriedWindow.Area) * 100f;
        }

        private Rectangle Intersection(Rectangle firstRectangle, Rectangle secondRectangle)
        {
            int left = Math.Max(firstRectangle.Left, secondRectangle.Left);
            int right = Math.Min(firstRectangle.Right, secondRectangle.Right);

            if (left >= right)
            {
                throw new ArgumentException("No intersection possible.");
            }

            int top = Math.Max(firstRectangle.Top, secondRectangle.Top);
            int bottom = Math.Min(firstRectangle.Bottom, secondRectangle.Bottom);

            if (top >= bottom)
            {
                throw new ArgumentException("No intersection possible.");
            }

            return new Rectangle(left, top, right, bottom);
        }
    }
}