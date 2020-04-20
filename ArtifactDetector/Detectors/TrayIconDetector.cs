using System;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect tray icons.
    /// </summary>
    internal class TrayIconDetector : IconDetector<NativeMethods.TaskBarButton>, IDetector
    {
        public TrayIconDetector()
            : base(
                  NativeMethods.TB.GETBUTTON,
                  NativeMethods.TB.BUTTONCOUNT,
                  0,
                  GetSystemTrayHandle())
        {
        }

        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountTrayIcons = 0;
        }

        protected override Rectangle GetAbsoluteIconRectangle(int iconIndex)
        {
            var rect = new NativeMethods.RectangularOutline();
            NativeMethods.GetWindowRect(WindowHandle, ref rect);
            return new Rectangle(rect);
        }

        protected override int GetIconCount(ref ArtifactRuntimeInformation runtimeInformation)
        {
            return runtimeInformation.CountTrayIcons;
        }

        /// <summary>
        /// Check if the given icon at the index matches the titles from runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information on what to look for.</param>
        /// <param name="index">Index of icon in parent window.</param>
        /// <param name="icon">Icon structure.</param>
        /// <returns>True if the icon matches.</returns>
        protected override string GetIconTitle(int index, NativeMethods.TaskBarButton icon)
        {
            FillIconStruct(index, ref icon);

            var bufferPointer = GetBufferPointer(ProcessHandle);

            uint bytesRead = 0;
            int titleLength = (int) NativeMethods.SendMessage(WindowHandle, NativeMethods.TB.GETBUTTONTEXTW, new IntPtr(icon.idCommand), bufferPointer);
            NativeMethods.ReadProcessMemory(ProcessHandle, bufferPointer, Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr(BUFFER_SIZE), ref bytesRead);

            return Marshal.PtrToStringUni(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), titleLength);
        }

        protected override void IncreaseIconCount(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountTrayIcons++;
        }

        /// <summary>
        /// Return a new (usable) instance of the icon struct.
        /// </summary>
        /// <returns></returns>
        protected override NativeMethods.TaskBarButton InitIconStruct()
        {
            return new NativeMethods.TaskBarButton();
        }

        /// <summary>
        /// Get the system tray window's handle.
        /// </summary>
        /// <returns>The handle if found or IntPtr.Zero if not.</returns>
        private static IntPtr GetSystemTrayHandle()
        {
            return GetWindowHandle(new string[][] {
                new string[] { "Shell_TrayWnd", null },
                new string[] { "TrayNotifyWnd", null },
                new string[] { "SysPager", null },
                new string[] { "ToolbarWindow32", null }
            });
        }
    }
}
