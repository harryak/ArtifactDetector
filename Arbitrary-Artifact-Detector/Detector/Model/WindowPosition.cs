using System.Drawing;
using System.Runtime.InteropServices;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// A two-dimensional point for window positions.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct WindowPosition
    {
        public int X;
        public int Y;

        public WindowPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Point(WindowPosition p)
        {
            return new Point(p.X, p.Y);
        }

        public static implicit operator WindowPosition(Point p)
        {
            return new WindowPosition(p.X, p.Y);
        }
    }
}
