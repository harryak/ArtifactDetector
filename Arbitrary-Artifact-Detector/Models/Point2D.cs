/**
* Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
* 
* For license, please see "License-LGPL.txt".
*/

using System.Drawing;
using System.Runtime.InteropServices;

namespace ArbitraryArtifactDetector.Models
{
    /// <summary>
    /// A two-dimensional point for window positions.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Point2D
    {
        public int X;
        public int Y;

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator Point(Point2D p)
        {
            return new Point(p.X, p.Y);
        }

        public static implicit operator Point2D(Point p)
        {
            return new Point2D(p.X, p.Y);
        }
    }
}
