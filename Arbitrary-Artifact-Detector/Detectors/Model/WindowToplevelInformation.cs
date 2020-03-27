using System;
using System.Windows.Forms;
using static ItsApe.ArtifactDetector.Utilities.NativeMethods;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class to contain a Windows windows' information.
    /// </summary>
    internal class WindowToplevelInformation
    {
        /// <summary>
        /// Window handle from user32.dll.
        /// </summary>
        public IntPtr Handle = IntPtr.Zero;

        /// <summary>
        /// Title of the window.
        /// </summary>
        public string Title = Application.ProductName;

        /// <summary>
        /// Percentage of how many of the window is visible.
        /// </summary>
        public float Visibility = 0f;

        public WindowVisualInformation VisualInformation = new WindowVisualInformation();

        /// <summary>
        /// Z-index of this window, positive integer. Higher means more in the background.
        /// </summary>
        public int ZIndex = 0;

        /// <summary>
        /// Placement information returned from user32.dll.
        /// </summary>
        internal WindowPlacement Placement = new WindowPlacement();

        /// <summary>
        /// For debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Title;
        }
    }
}
