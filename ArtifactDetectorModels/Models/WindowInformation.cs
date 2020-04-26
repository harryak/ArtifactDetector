using ItsApe.ArtifactDetector.Converters;
using MessagePack;
using System;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Data class to contain a Windows windows' information.
    /// </summary>
    [Serializable]
    [MessagePackObject]
    public class WindowInformation
    {
        /// <summary>
        /// Rectangle of the window.
        /// </summary>
        [Key(0)]
        public Rectangle BoundingArea;

        /// <summary>
        /// Window handle from user32.dll.
        /// </summary>
        [Key(1)]
        [MessagePackFormatter(typeof(IntPtrFormatter))]
        public IntPtr Handle = IntPtr.Zero;

        /// <summary>
        /// Title of the window.
        /// </summary>
        [Key(2)]
        public string Title;

        /// <summary>
        /// Percentage of how many of the window is visible.
        /// </summary>
        [Key(3)]
        public float Visibility = 0f;

        /// <summary>
        /// Z-index of this window, positive integer. Higher means more in the background.
        /// </summary>
        [Key(4)]
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
