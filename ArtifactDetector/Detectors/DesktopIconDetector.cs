using System;
using System.Runtime.InteropServices;
using System.Text;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Detector to detect desktop icons.
    /// </summary>
    internal class DesktopIconDetector : IconDetector<NativeMethods.ListViewItem>, IDetector
    {
        public DesktopIconDetector()
            : base(
                  NativeMethods.LVM.GETITEMW,
                  NativeMethods.LVM.GETITEMCOUNT,
                  NativeMethods.LVM.GETITEMRECT,
                  GetDesktopHandle())
        { }

        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountDesktopIcons = 0;
        }

        protected override int GetIconCount(ref ArtifactRuntimeInformation runtimeInformation)
        {
            return runtimeInformation.CountDesktopIcons;
        }

        /// <summary>
        /// Check if the given icon at the index matches the titles from runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information on what to look for.</param>
        /// <param name="index">Index of icon in parent window.</param>
        /// <param name="icon">Icon structure.</param>
        /// <returns>True if the icon matches.</returns>
        protected override string GetIconTitle(int index, NativeMethods.ListViewItem icon)
        {
            icon.iItem = index;

            uint bytesRead = 0;

            // Write to desktop process the structure we want to get.
            var currentIcon = FillStructFromProcess(icon, ref bytesRead);

            // Use the now set pszText pointer to read the icon text into the buffer. Maximum length is 260, more characters won't be displayed.
            NativeMethods.ReadProcessMemory(ProcessHandle, currentIcon.pszText, Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr(260), ref bytesRead);

            // Read from buffer into string with unicode encoding, then trim string.
            return IconTitleFromBuffer((int)bytesRead);
        }

        protected override void IncreaseIconCount(ref ArtifactRuntimeInformation runtimeInformation)
        {
            runtimeInformation.CountDesktopIcons++;
        }

        /// <summary>
        /// Creates a new (usable) instance of the icon struct.
        /// </summary>
        /// <returns>The created instance.</returns>
        protected override NativeMethods.ListViewItem InitIconStruct()
        {
            // Initialize basic structure.
            return new NativeMethods.ListViewItem
            {
                // We want to get the icon's text, so set the mask accordingly.
                mask = NativeMethods.LVIF.TEXT,

                // Set maximum text length to buffer length minus offset used in pszText.
                cchTextMax = BUFFER_SIZE - Marshal.SizeOf(typeof(NativeMethods.ListViewItem)),

                // Set pszText at point in the remote process's buffer.
                pszText = GetBufferPointer(ProcessHandle) + Marshal.SizeOf(typeof(NativeMethods.ListViewItem))
            };
        }

        /// <summary>
        /// Get the desktop window's handle.
        /// </summary>
        /// <returns>The handle if found or IntPtr.Zero if not.</returns>
        private static IntPtr GetDesktopHandle()
        {
            return GetWindowHandle(new string[][] { new string[] { "Progman", "Program Manager" }, new string[] { "SHELLDLL_DefView", null }, new string[] { "SysListView32", "FolderView" } });
        }

        /// <summary>
        /// Let the process at ProcessHandle write data to an icon.
        /// </summary>
        /// <param name="icon">The icon struct to use as scheme.</param>
        /// <param name="bytesRead">Reference of read bytes.</param>
        /// <returns>The filled structure</returns>
        private NativeMethods.ListViewItem FillStructFromProcess(NativeMethods.ListViewItem icon, ref uint bytesRead)
        {
            // Initialize current icon.
            var currentIcon = new NativeMethods.ListViewItem();
            // Wrap icon in array so we can get the pinned address of it.
            NativeMethods.ListViewItem[] pinnedArray = new NativeMethods.ListViewItem[] { icon };

            // Write scheme to foreign process.
            NativeMethods.WriteProcessMemory(ProcessHandle, GetBufferPointer(ProcessHandle), Marshal.UnsafeAddrOfPinnedArrayElement(pinnedArray, 0), new UIntPtr((uint)Marshal.SizeOf(icon)), ref bytesRead);

            // Get i-th item of desktop and read its memory.
            FillIconStruct(icon.iItem, ref currentIcon);

            return currentIcon;
        }

        /// <summary>
        /// Parse icon title from buffer.
        /// </summary>
        /// <param name="length">Title length in buffer.</param>
        /// <returns>Trimmed title string.</returns>
        private string IconTitleFromBuffer(int length)
        {
            string iconTitle = Encoding.Unicode.GetString(_buffer, 0, length);
            return iconTitle.Substring(0, iconTitle.IndexOf('\0'));
        }
    }
}
