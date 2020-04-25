using System;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// Rectangle mainly used for Windows' windows boundaries and DLL calls.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public int Left, Top, Right, Bottom;

        public Rectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Constructor using a System.Drawing.Rectangle.
        /// </summary>
        /// <param name="rectangleObject"></param>
        public Rectangle(System.Drawing.Rectangle rectangleObject) : this(rectangleObject.Left, rectangleObject.Top, rectangleObject.Right, rectangleObject.Bottom)
        {
        }

        public Rectangle(NativeStructures.RectangularOutline rectStruct) : this(rectStruct.left, rectStruct.top, rectStruct.right, rectStruct.bottom)
        {
        }

        /// <summary>
        /// Accessor for the height.
        /// </summary>
        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        /// <summary>
        /// Accessor for the width.
        /// </summary>
        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }

        /// <summary>
        /// Accessor for the total area.
        /// </summary>
        public int Area
        {
            get { return Width * Height; }
        }

        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(Width, Height); }
        }

        /// <summary>
        /// Cast this structure to a System.Drawing.Rectangle.
        /// </summary>
        /// <param name="rectangleStruct">This.</param>
        public static implicit operator System.Drawing.Rectangle(Rectangle rectangleStruct)
        {
            return new System.Drawing.Rectangle(
                rectangleStruct.Left,
                rectangleStruct.Top,
                rectangleStruct.Width,
                rectangleStruct.Height);
        }

        /// <summary>
        /// Cast a System.Drawing.Rectangle to this structure.
        /// </summary>
        /// <param name="rectangleObject">System.Drawing.Rectangle to parse.</param>
        public static implicit operator Rectangle(System.Drawing.Rectangle rectangleObject)
        {
            return new Rectangle(rectangleObject);
        }
    }
}
