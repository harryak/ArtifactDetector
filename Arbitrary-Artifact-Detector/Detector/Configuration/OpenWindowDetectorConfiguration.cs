using System;

namespace ArbitraryArtifactDetector.Detector.Configuration
{
    /// <summary>
    /// Configuration for the OpenWindowDetector to detect whether a program's windows is open and visible.
    /// </summary>
    internal class OpenWindowDetectorConfiguration : BaseDetectorConfiguration, IDetectorConfiguration
    {
        /// <summary>
        /// Constructor: Expects a window handle to look for.
        /// </summary>
        /// <param name="windowHandle">Non-zero pointer to the window, the window handle.</param>
        public OpenWindowDetectorConfiguration(IntPtr windowHandle)
        {
            // Make sure the sender is set as detection without is impossible and it is always set.
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Handle can not be zero.");
            }

            WindowHandle = windowHandle;
        }

        /// <summary>
        /// The handle of the window to look for.
        /// Either this or the WindowTitle must be set.
        /// </summary>
        public IntPtr WindowHandle { get; } = IntPtr.Zero;

        /// <summary>
        /// The title of the window to look for.
        /// Either this or the WindowHandle must be set.
        /// </summary>
        public string WindowTitle { get; } = "";
    }
}