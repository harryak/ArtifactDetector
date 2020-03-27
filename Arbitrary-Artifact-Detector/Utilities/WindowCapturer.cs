using System;
using System.Drawing;
using System.Runtime.InteropServices;
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
            IntPtr hdcSrc = NativeMethods.GetWindowDC(handle);
            // Get the size of the window.
            NativeMethods.RECT windowRect = new NativeMethods.RECT();
            NativeMethods.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // Create a device context we can copy to.
            IntPtr hdcDest = NativeMethods.CreateCompatibleDC(hdcSrc);
            // Create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height.
            IntPtr hBitmap = NativeMethods.CreateCompatibleBitmap(hdcSrc, width, height);
            // Select the bitmap object.
            IntPtr hOld = NativeMethods.SelectObject(hdcDest, hBitmap);
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
            Image<Bgr, byte> imageCV = new Image<Bgr, byte>(bitmap);
            return imageCV.Mat;
        }

        #region DLL imports

        //TODO: Move to global NativeMethods.
        private class NativeMethods
        {
            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

            [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

            [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

            [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

            [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteDC(IntPtr hDC);

            [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject(IntPtr hObject);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
        }

        #endregion DLL imports
    }
}
