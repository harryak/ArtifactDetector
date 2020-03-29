using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

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
        /// <param name="handle">The handle to the window.</param>
        /// <returns>An image of the window.</returns>
        public static Mat CaptureWindow(IntPtr handle)
        {
            // Get the DC of the target window
            var hdcSrc = NativeMethods.GetWindowDC(handle);
            // Get the size of the window.
            var windowRect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // Create a device context we can copy to.
            var hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);
            // Create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height.
            var hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, width, height);
            // Select the bitmap object.
            var hOld = NativeMethods.SelectObject(hdcDest, hBitmap);
            // bitblt over
            NativeMethods.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, NativeMethods.SRCCOPY);
            // Restore selection.
            NativeMethods.SelectObject(hdcDest, hOld);
            // Clean up.
            NativeMethods.DeleteDC(hdcDest);
            NativeMethods.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Bitmap bitmap = Image.FromHbitmap(hBitmap);
            // Free up the Bitmap object.
            NativeMethods.DeleteObject(hBitmap);
            // Get OpenCV Mat from bitmap.
            var imageCV = new Image<Bgr, byte>(bitmap);
            return imageCV.Mat;
        }
    }
}
