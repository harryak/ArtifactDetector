using System;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class to contain a Windows windows' information.
    /// </summary>
    [Serializable]
    internal class WindowInformation
    {
        /// <summary>
        /// Rectangle of the window.
        /// </summary>
        public Rectangle BoundingArea;

        /// <summary>
        /// Window handle from user32.dll.
        /// </summary>
        public IntPtr Handle = IntPtr.Zero;

        /// <summary>
        /// Title of the window.
        /// </summary>
        public string Title;

        /// <summary>
        /// Percentage of how many of the window is visible.
        /// </summary>
        public float Visibility = 0f;

        /// <summary>
        /// Z-index of this window, positive integer. Higher means more in the background.
        /// </summary>
        public int ZIndex = 0;

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
