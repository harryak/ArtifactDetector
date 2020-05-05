using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;

namespace ItsApe.ArtifactDetectorProcess.Utilities
{
    /// <summary>
    /// Screenshot utility capturing windows or sub-windows.
    /// </summary>
    internal class VisualCapturer
    {
        /// <summary>
        /// Lock for screenshot utility.
        /// </summary>
        private static readonly object memoryLock = new object();

        /// <summary>
        /// Take screenshots of all existing screens and write to memory.
        /// </summary>
        /// <param name="screenshotMemory">Memory to write to.</param>
        public void TakeScreenshots(ref MemoryMappedViewStream screenshotMemoryStream)
        {
            lock (memoryLock)
            {
                using (var screenCaptureBitmap = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height, PixelFormat.Format32bppArgb))
                using (var captureWrapper = Graphics.FromImage(screenCaptureBitmap))
                {
                    screenshotMemoryStream.Position = 0;
                    captureWrapper.CopyFromScreen(SystemInformation.VirtualScreen.Left, SystemInformation.VirtualScreen.Top, 0, 0, SystemInformation.VirtualScreen.Size);
                    screenCaptureBitmap.Save(screenshotMemoryStream, ImageFormat.Bmp);
                    screenshotMemoryStream.Flush();
                }
            }
        }
    }
}
