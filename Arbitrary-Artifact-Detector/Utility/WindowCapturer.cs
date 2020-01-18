using Emgu.CV;
using System;
using System.Runtime.InteropServices;

namespace ArbitraryArtifactDetector.Utility
{
    class WindowCapturer
    {
        /// <summary>
        /// Creates an image object containing a screenshot of a specific window.
        /// </summary>
        /// <param name="handle">The handle to the window.</param>
        /// <returns>An image of the window.</returns>
        public static Mat CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = GetWindowDC(handle);
            // get the size
            RECT windowRect = new RECT();
            GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc,width,height);
            // select the bitmap object
            IntPtr hOld = SelectObject(hdcDest,hBitmap);
            // bitblt over
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            // restore selection
            SelectObject(hdcDest, hOld);
            // clean up
            DeleteDC(hdcDest);
            ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Mat image = CvInvoke.CvArrToMat(hBitmap);
            // free up the Bitmap object
            DeleteObject(hBitmap);
            return image;
        }
        
        #region DLL imports
        private const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        #endregion
    }
}
