using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ItsApe.ArtifactDetector.Utilities
{
    /// <summary>
    /// Screenshot utility capturing windows or sub-windows.
    /// </summary>
    internal class VisualCapturer
    {
        public static Mat CaptureRegion(Models.Rectangle boundingArea)
        {
            var screenCaptureBitmap = new Bitmap(boundingArea.Width, boundingArea.Height, PixelFormat.Format32bppRgb);
            var captureWrapper = Graphics.FromImage(screenCaptureBitmap);
            captureWrapper.CopyFromScreen(boundingArea.Left, boundingArea.Top, 0, 0, boundingArea.Size);
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
