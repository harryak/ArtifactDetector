using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Utilities
{
    /// <summary>
    /// Screenshot utility capturing windows or sub-windows.
    /// </summary>
    internal class WindowCapturer
    {
        /// <summary>
        /// Creates an image object containing a screenshot of a specific window.
        /// </summary>
        /// <param name="information">Information object of the window to capture.</param>
        /// <returns>An image of the window.</returns>
        public static Mat CaptureWindow(WindowInformation information)
        {
            var screenCaptureBitmap = new Bitmap(information.BoundingArea.Width, information.BoundingArea.Height, PixelFormat.Format32bppRgb);
            var captureWrapper = Graphics.FromImage(screenCaptureBitmap);
            captureWrapper.CopyFromScreen(information.BoundingArea.Left, information.BoundingArea.Top, 0, 0, information.BoundingArea.Size);
            return screenCaptureBitmap.ToImage<Bgr, byte>().Mat;
        }

        /// <summary>
        /// Creates an image object containing a screenshot of a complete screen.
        /// </summary>
        /// <param name="screen">Screen to capture.</param>
        /// <returns>An image of the screen.</returns>
        internal static Mat CaptureScreen(Screen screen)
        {
            var screenCaptureBitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppRgb);
            var captureWrapper = Graphics.FromImage(screenCaptureBitmap);
            captureWrapper.CopyFromScreen(screen.Bounds.Left, screen.Bounds.Top, 0, 0, screen.Bounds.Size);
            return screenCaptureBitmap.ToImage<Bgr, byte>().Mat;
        }
    }
}
