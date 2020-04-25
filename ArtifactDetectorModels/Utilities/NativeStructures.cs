using System.Runtime.InteropServices;

namespace ItsApe.ArtifactDetector.Utilities
{
    public class NativeStructures
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RectangularOutline
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}