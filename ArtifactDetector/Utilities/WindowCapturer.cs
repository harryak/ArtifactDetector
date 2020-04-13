using System.Drawing;
using System.Drawing.Imaging;
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
    }
}
