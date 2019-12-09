using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    /// <summary>
    /// Data class to contain a Windows windows' information.
    /// </summary>
    [Serializable]
    class WindowToplevelInformation
    {
        /// <summary>
        /// Information on the responsible executable's path.
        /// </summary>
        public FileInfo ExecutablePath = new FileInfo(Application.ExecutablePath);
        /// <summary>
        /// Window handle from user32.dll.
        /// </summary>
        public IntPtr Handle = IntPtr.Zero;

        /// <summary>
        /// Placement information returned from user32.dll.
        /// </summary>
        internal WindowPlacement Placement = new WindowPlacement();

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
        /// For debugging purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ExecutablePath.Name;
        }
    }
}
