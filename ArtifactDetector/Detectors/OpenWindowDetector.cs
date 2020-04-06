using System;
using System.Collections.Generic;
using System.Text;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

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
        /// List of matching windows that have been found.
        /// </summary>
        private IDictionary<IntPtr, WindowToplevelInformation> MatchingWindowsFound { get; set; }

        /// <summary>
        /// Local copy of possible window titles for nested delegate function.
        /// </summary>
        private IList<string> PossibleWindowTitles { get; set; }

        /// <summary>
        /// Local copy of previously found window handles for nested delegate function.
        /// </summary>
        private ICollection<IntPtr> WindowHandles { get; set; }

        /// <summary>
        /// List of all (visible) windows that have been found with their z-index as key.
        /// </summary>
        private IDictionary<int, Rectangle> WindowsFound { get; set; }

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
                Logger.LogInformation("No matching windows or possible window titles given for detector. Only getting visible windows now.");
                return FindArtifactLight(ref runtimeInformation);
            }

            Logger.LogInformation("Detecting open windows now.");
            InitializeDetection(ref runtimeInformation);

            // Access all open windows and analyze each of them.
            NativeMethods.EnumWindows(
                new NativeMethods.EnumWindowsProc(AnalyzeVisibleWindowDelegate),
                IntPtr.Zero
            );

            return PrepareResponse(ref runtimeInformation);
        }

        /// <summary>
        /// Function used as delegate for NativeMethods.EnumWindows to analyze every open window.
        /// </summary>
        /// <param name="windowHandle">IntPtr for current window.</param>
        /// <param name="parameters">Unused.</param>
        /// <returns>True, always.</returns>
        private bool AnalyzeVisibleWindowDelegate(IntPtr windowHandle, int parameters)
        {
            // If we already found enough windows or the window is invisible, skip.
            // This includes windows which title can not be retrieved.
            if (!NativeMethods.IsWindowVisible(windowHandle)
                || !GetWindowTitle(windowHandle, out string windowTitle))
            {
                return true;
            }

            // Get visual information about the current window.
            var visualInformation = new NativeMethods.WindowVisualInformation();
            NativeMethods.GetWindowInfo(windowHandle, ref visualInformation);

            // If it is one of the windows we want to find: Add to that list.
            if (WindowMatchesConstraints(windowTitle, windowHandle))
            {
                MatchingWindowsFound.Add(windowHandle, new WindowToplevelInformation
                {
                    Handle = windowHandle,
                    Title = windowTitle,
                    Visibility = CalculateWindowVisibility(visualInformation.rcWindow, WindowsFound.Values),
                    VisualInformation = visualInformation,
                    ZIndex = WindowsFound.Count
                });
            }

            // Add the current window to all windows now.
            WindowsFound.Add(WindowsFound.Count, visualInformation.rcWindow);

            return true;
        }

        /// <summary>
        /// Only get the visible windows into the runtime information object and do not detect anything.
        /// </summary>
        /// <param name="runtimeInformation">The runtime object reference to fill.</param>
        /// <returns>Detector response with "artifact presence possible" set.</returns>
        private DetectorResponse FindArtifactLight(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Access all open windows and just store each of them.
            NativeMethods.EnumWindows(
                new NativeMethods.EnumWindowsProc(GetVisibleWindowDelegate),
                IntPtr.Zero
            );

            runtimeInformation.VisibleWindows = WindowsFound;

            StopStopwatch("Got all opened windows in {0}ms.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
        }

        /// <summary>
        /// Function used as delegate for NativeMethods.EnumWindows to just store every open window.
        /// </summary>
        /// <param name="windowHandle">IntPtr for current window.</param>
        /// <param name="parameters">Unused.</param>
        /// <returns>True, always.</returns>
        private bool GetVisibleWindowDelegate(IntPtr windowHandle, int parameters)
        {
            // If we already found enough windows or the window is invisible, skip.
            // This includes windows which title can not be retrieved.
            if (!NativeMethods.IsWindowVisible(windowHandle)
                || !GetWindowTitle(windowHandle, out string windowTitle))
            {
                return true;
            }

            // Get visual information about the current window.
            var visualInformation = new NativeMethods.WindowVisualInformation();
            NativeMethods.GetWindowInfo(windowHandle, ref visualInformation);

            // Add the current window to all windows now.
            WindowsFound.Add(WindowsFound.Count, visualInformation.rcWindow);

            return true;
        }

        /// <summary>
        /// Tries to get the title of the given window.
        /// If returning false this did not work and the out parameter is an empty string.
        /// </summary>
        /// <param name="windowHandle">Window handle IntPtr to get the title from.</param>
        /// <param name="windowTitle">Out parameter to write the title to, if possible.</param>
        /// <returns>True if the title could be obtained.</returns>
        private bool GetWindowTitle(IntPtr windowHandle, out string windowTitle)
        {
            // Some windows have no title, so make sure we don't access the title if it is not there.
            int titleLength = NativeMethods.GetWindowTextLength(windowHandle);

            if (titleLength > 0)
            {
                // Get window title into string builder.
                var titleStringBuilder = new StringBuilder(titleLength);
                NativeMethods.GetWindowText(windowHandle, titleStringBuilder, titleLength + 1);
                windowTitle = titleStringBuilder.ToString();
                return true;
            }
            else
            {
                windowTitle = "";
                return false;
            }
        }

        /// <summary>
        /// (Re)set necessary class variables for detection.
        /// </summary>
        /// <param name="runtimeInformation">The object to get information from.</param>
        private void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Copy to local variables for EnumWindowsProc.
            WindowHandles = runtimeInformation.MatchingWindowsInformation.Keys;
            PossibleWindowTitles = runtimeInformation.PossibleWindowTitles;

            // Initialize class properties for this detection run.
            WindowsFound = new Dictionary<int, Rectangle>();
            MatchingWindowsFound = new Dictionary<IntPtr, WindowToplevelInformation>();
        }

        /// <summary>
        /// Create a response based on what was found.
        /// </summary>
        /// <param name="runtimeInformation">The runtime information to write to.</param>
        /// <returns></returns>
        private DetectorResponse PrepareResponse(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Copy visible windows to runtime information for other detectors.
            runtimeInformation.VisibleWindows = WindowsFound;

            // If we found not a single matching window the artifact can't be present.
            if (MatchingWindowsFound.Count < 1)
            {
                StopStopwatch("Got all opened windows in {0}ms.");
                Logger.LogInformation("Found no matching open windows.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }

            // Copy found information back to object reference.
            runtimeInformation.MatchingWindowsInformation = MatchingWindowsFound;

            StopStopwatch("Got all opened windows in {0}ms.");
            Logger.LogInformation("Found {0} matching open windows.", MatchingWindowsFound.Count);
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
        }

        /// <summary>
        /// Check if a window matches the detector's constraints.
        /// </summary>
        /// <param name="windowTitle">Obvious.</param>
        /// <param name="windowHandle">Internal IntPtr for window.</param>
        /// <returns>True if the window matches.</returns>
        private bool WindowMatchesConstraints(string windowTitle, IntPtr windowHandle)
        {
            return WindowHandles.Contains(windowHandle) || WindowTitleMatches(windowTitle);
        }

        /// <summary>
        /// Check if a window title contains a substring from the PossibleWindowTitles.
        /// Ignoring the case.
        /// </summary>
        /// <param name="windowTitle">Obvious.</param>
        /// <returns>True if the window title contains any substring.</returns>
        private bool WindowTitleMatches(string windowTitle)
        {
            return windowTitle.ContainsAny(PossibleWindowTitles, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
