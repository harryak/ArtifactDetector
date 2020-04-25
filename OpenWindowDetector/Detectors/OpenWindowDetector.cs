﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using ItsApe.OpenWindowDetector.Utilities;

namespace ItsApe.OpenWindowDetector.Detectors
{
    /// <summary>
    /// Detector to detect if a window is opened and visible.
    ///
    /// Needs:  WindowHandle _or_ WindowTitle
    /// Yields: WindowHandle, WindowTitle
    /// </summary>
    internal class OpenWindowDetector
    {
        /// <summary>
        /// Special window handle to "desktop window".
        /// </summary>
        private IntPtr ProgramManagerWindowHandle { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public void FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.PossibleWindowTitleSubstrings.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                AnalyzeVisibleWindowsLight(ref runtimeInformation);
            }

            AnalyzeVisibleWindows(ref runtimeInformation);
        }

        /// <summary>
        /// Calculates how much (percentage) of the queriedWindow is visible other windows above.
        /// </summary>
        /// <param name="queriedWindow">Queried window.</param>
        /// <param name="windowsAbove">The windows above (z-index) the queried window.</param>
        /// <returns>The percentage of how much of the window is visible.</returns>
        protected float CalculateWindowVisibility(Rectangle queriedWindow, ICollection<Rectangle> windowsAbove)
        {
            // If there are no windows above: Return immediately.
            if (windowsAbove.Count < 1)
            {
                return 100f;
            }

            // If there is no area of the window, return "no visibility".
            if (queriedWindow.Area < 1)
            {
                return 0f;
            }

            int subtractArea = new RectangleUnionCalculator().CalculateRectangleUnion(queriedWindow, windowsAbove);

            return (float)(queriedWindow.Area - subtractArea) / queriedWindow.Area * 100f;
        }

        /// <summary>
        /// Function used as delegate for NativeMethods.EnumWindows to analyze every open window.
        /// </summary>
        /// <param name="windowHandle">IntPtr for current window.</param>
        /// <param name="_">Unused.</param>
        /// <returns>True, always.</returns>
        private bool AnalyzeVisibleWindowDelegate(IntPtr windowHandle, IntPtr lParam)
        {
            // If we already found enough windows or the window is invisible, skip.
            // This includes windows which title can not be retrieved.
            if (!NativeMethods.IsWindowVisible(windowHandle)
                || NativeMethods.IsIconic(windowHandle)
                || !GetWindowTitle(windowHandle, out var windowTitle)
                || windowHandle == ProgramManagerWindowHandle)
            {
                return true;
            }

            // Get visual information about the current window.
            var visualInformation = new NativeMethods.WindowVisualInformation();
            NativeMethods.GetWindowInfo(windowHandle, ref visualInformation);

            var handle = GCHandle.FromIntPtr(lParam);
            var runtimeInformation = (ArtifactRuntimeInformation)handle.Target;

            // If it is one of the windows we want to find: Add to that list.
            if (WindowMatchesConstraints(windowTitle, windowHandle, ref runtimeInformation))
            {
                float visibility = CalculateWindowVisibility(
                            visualInformation.rcClient,
                            runtimeInformation.VisibleWindowOutlines.Values);
                runtimeInformation.WindowsInformation.Add(
                    new WindowInformation()
                    {
                        BoundingArea = visualInformation.rcWindow,
                        Handle = windowHandle,
                        Title = windowTitle,
                        Visibility = visibility,
                        ZIndex = runtimeInformation.VisibleWindowOutlines.Count + 1
                    });
                runtimeInformation.CountOpenWindows++;

                if (runtimeInformation.MaxWindowVisibilityPercentage < visibility)
                {
                    runtimeInformation.MaxWindowVisibilityPercentage = visibility;
                }
            }

            // Add the current window to all windows now.
            runtimeInformation.VisibleWindowOutlines.Add(
                runtimeInformation.VisibleWindowOutlines.Count + 1,
                visualInformation.rcClient);

            return true;
        }

        private void AnalyzeVisibleWindows(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Access all open windows and analyze each of them.
            var windowStringsHandle = GCHandle.Alloc(runtimeInformation);
            try
            {
                NativeMethods.EnumWindows(
                    AnalyzeVisibleWindowDelegate,
                    GCHandle.ToIntPtr(windowStringsHandle));
            }
            finally
            {
                windowStringsHandle.Free();
            }
        }

        private void AnalyzeVisibleWindowsLight(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Access all open windows and analyze each of them.
            var windowStringsHandle = GCHandle.Alloc(runtimeInformation);
            try
            {
                NativeMethods.EnumWindows(
                    GetVisibleWindowDelegate,
                    GCHandle.ToIntPtr(windowStringsHandle));
            }
            finally
            {
                windowStringsHandle.Free();
            }
        }

        /// <summary>
        /// Retrieve the desktop window handle to exclude from open windows.
        /// </summary>
        /// <returns>Desktop window handle.</returns>
        private IntPtr GetDesktopWindowHandle()
        {
            return NativeMethods.FindWindow("Progman", "Program Manager");
        }

        /// <summary>
        /// Function used as delegate for NativeMethods.EnumWindows to just store every open window.
        /// </summary>
        /// <param name="windowHandle">IntPtr for current window.</param>
        /// <param name="lParam">Pointer to GCHandle for ArtifactRuntimeInformation.</param>
        /// <returns>True, always.</returns>
        private bool GetVisibleWindowDelegate(IntPtr windowHandle, IntPtr lParam)
        {
            // If we already found enough windows or the window is invisible, skip.
            // This includes windows which title can not be retrieved.
            if (!NativeMethods.IsWindowVisible(windowHandle)
                || !GetWindowTitle(windowHandle, out string windowTitle)
                || windowHandle == ProgramManagerWindowHandle)
            {
                return true;
            }

            // Get visual information about the current window.
            var visualInformation = new NativeMethods.WindowVisualInformation();
            NativeMethods.GetWindowInfo(windowHandle, ref visualInformation);

            var handle = GCHandle.FromIntPtr(lParam);
            var runtimeInformation = (ArtifactRuntimeInformation)handle.Target;

            // Add the current window to all windows now.
            runtimeInformation.VisibleWindowOutlines.Add(
                runtimeInformation.VisibleWindowOutlines.Count + 1,
                visualInformation.rcClient);

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
        /// "Light" version of detection initialization without copying anything from runtime information.
        /// </summary>
        private void InitializeDetectionLight()
        {
            ProgramManagerWindowHandle = GetDesktopWindowHandle();
        }

        /// <summary>
        /// Check if a window matches the detector's constraints.
        /// </summary>
        /// <param name="windowTitle">Obvious.</param>
        /// <param name="windowHandle">Internal IntPtr for window.</param>
        /// <returns>True if the window matches.</returns>
        private bool WindowMatchesConstraints(string windowTitle, IntPtr windowHandle, ref ArtifactRuntimeInformation runtimeInformation)
        {
            return runtimeInformation.WindowHandles.Contains(windowHandle)
                || windowTitle.ContainsAny(runtimeInformation.PossibleWindowTitleSubstrings);
        }
    }
}