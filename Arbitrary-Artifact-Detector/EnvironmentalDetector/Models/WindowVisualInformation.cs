using System;
using System.Runtime.InteropServices;

namespace ArbitraryArtifactDetector.EnvironmentalDetector.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowVisualInformation
    {
        public uint cbSize;
        public Rectangle rcWindow;
        public Rectangle rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;

        public WindowVisualInformation(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        {
            cbSize = (UInt32) (Marshal.SizeOf(typeof(WindowVisualInformation)));
        }

    }
}
