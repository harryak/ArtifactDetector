using System;
using System.Runtime.InteropServices;
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
        /// Special window handle to "desktop window".
        /// </summary>
        private IntPtr ProgramManagerWindowHandle { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (!IsScreenActive(ref runtimeInformation))
            {
                Logger.LogInformation("Not detecting, screen is locked.");
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }
            Logger.LogInformation("Detecting open windows now.");

            // Check whether we have enough data to detect the artifact.
            if (runtimeInformation.WindowHandles.Count < 1 && runtimeInformation.PossibleWindowTitleSubstrings.Count < 1)
            {
                Logger.LogInformation("No matching windows or possible window titles given for detector. Only getting visible windows now.");
                return FindArtifactLight(ref runtimeInformation);
            }

            InitializeDetection(ref runtimeInformation);
            AnalyzeVisibleWindows(ref runtimeInformation);

            return PrepareResponse(ref runtimeInformation);
        }

        /// <summary>
        /// Initialize (or reset) the detection for FindArtifact.
        /// </summary>
        /// <param name="runtimeInformation">Reference to object to initialize from.</param>
        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            InitializeDetectionLight();
            runtimeInformation.CountOpenWindows = 0;
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

            // Assumption: If the current window is an overlapped window it is "visible".
            if ((visualInformation.dwExStyle & NativeMethods.WindowStyles.WS_EX_NOACTIVATE) != NativeMethods.WindowStyles.WS_EX_NOACTIVATE)
            {
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
            }

            return true;
        }

        private void AnalyzeVisibleWindows(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Access all open windows and analyze each of them.
            var runtimeInformationHandle = GCHandle.Alloc(runtimeInformation);
            try
            {
                NativeMethods.EnumWindows(
                    AnalyzeVisibleWindowDelegate,
                    GCHandle.ToIntPtr(runtimeInformationHandle));
            }
            finally
            {
                runtimeInformationHandle.Free();
            }
        }

        /// <summary>
        /// Only get the visible windows into the runtime information object and do not detect anything.
        /// </summary>
        /// <param name="runtimeInformation">The runtime object reference to fill.</param>
        /// <returns>Detector response with "artifact presence possible" set.</returns>
        private DetectorResponse FindArtifactLight(ref ArtifactRuntimeInformation runtimeInformation)
        {
            InitializeDetectionLight();

            // Access all open windows and just store each of them.
            var runtimeInformationHandle = GCHandle.Alloc(runtimeInformation);
            try
            {
                NativeMethods.EnumWindows(
                    GetVisibleWindowDelegate,
                    GCHandle.ToIntPtr(runtimeInformationHandle));
            }
            finally
            {
                runtimeInformationHandle.Free();
            }

            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
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

            // Assumption: If the current window is an overlapped window it is "visible".
            if ((visualInformation.dwExStyle & NativeMethods.WindowStyles.WS_EX_NOACTIVATE) != NativeMethods.WindowStyles.WS_EX_NOACTIVATE)
            {
                // Add the current window to all windows now.
                runtimeInformation.VisibleWindowOutlines.Add(
                    runtimeInformation.VisibleWindowOutlines.Count + 1,
                    visualInformation.rcClient);
            }

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
        /// Create a response based on what was found.
        /// </summary>
        /// <param name="runtimeInformation">The runtime information to write to.</param>
        /// <returns></returns>
        private DetectorResponse PrepareResponse(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // If we found not a single matching window the artifact can't be present.
            if (runtimeInformation.CountOpenWindows > 0)
            {
                Logger.LogInformation("Found {0} matching open windows.", runtimeInformation.CountOpenWindows);
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            Logger.LogInformation("Found no matching open windows.");
            return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
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
