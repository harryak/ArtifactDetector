using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using ItsApe.ArtifactDetectorProcess.Utilities;

namespace ItsApe.ArtifactDetectorProcess.Detectors
{
    /// <summary>
    /// Common base class for icon detectors.
    /// <typeparam name="StructType">Type of the struct to fill (for marshalling).</typeparam>
    /// </summary>
    internal abstract class IconDetector<StructType> : BaseDetector, IDetector where StructType : struct
    {
        /// <summary>
        /// Choose a buffer size which is large enough for the operations in this class.
        /// </summary>
        protected const int BUFFER_SIZE = 0x1100;

        /// <summary>
        /// Buffer to use for getting data from other processes.
        /// </summary>
        protected readonly byte[] _buffer = new byte[BUFFER_SIZE];

        /// <summary>
        /// Default zIndex for icons.
        /// </summary>
        protected int IconZIndex = 9999;

        /// <summary>
        /// Pointers to other processes' buffers-
        /// </summary>
        private readonly IDictionary<IntPtr, IntPtr> _bufferPointers = new Dictionary<IntPtr, IntPtr>();

        /// <summary>
        /// Windows API code for SendMessage to get the icon count.
        /// </summary>
        private readonly uint _getIconCountCode;

        /// <summary>
        /// Windows API code for SendMessage to get the icon rectangle.
        /// </summary>
        private readonly uint _getIconRectCode;

        /// <summary>
        /// Windows API code for SendMessage to get the icons.
        /// </summary>
        private readonly uint _getIconsCode;

        /// <summary>
        /// Constructor to switch between icon types.
        /// </summary>
        /// <param name="getIconsCode">Windows API code for SendMessage.</param>
        /// <param name="getIconCountCode">Windows API code for SendMessage.</param>
        /// <param name="windowHandle">Handle of the icons' parent window.</param>
        protected IconDetector(uint getIconsCode, uint getIconCountCode, uint getIconRectCode, IntPtr windowHandle)
        {
            _getIconsCode = getIconsCode;
            _getIconCountCode = getIconCountCode;
            _getIconRectCode = getIconRectCode;

            WindowHandle = windowHandle;
            ProcessHandle = InitProcessHandle();
        }

        /// <summary>
        /// Handle of the icons' process.
        /// </summary>
        protected IntPtr ProcessHandle { get; set; }

        /// <summary>
        /// Handle of the icons' window.
        /// </summary>
        protected IntPtr WindowHandle { get; set; }

        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public void FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // This error is really unlikely.
            if (WindowHandle == IntPtr.Zero)
            {
                return;
            }

            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                return;
            }

            InitializeDetection(ref runtimeInformation);

            AnalizeIcons(ref runtimeInformation);
        }

        /// <summary>
        /// Retrieve window address from a list of nested processes.
        /// </summary>
        /// <param name="processNames">Nested pairs of process names and window names.</param>
        /// <returns>The window's address or IntPtr.Zero on failure.</returns>
        protected static IntPtr GetWindowHandle(string[][] processNames)
        {
            var windowHandle = IntPtr.Zero;

            if (processNames.Length > 0)
            {
                windowHandle = NativeMethods.FindWindow(processNames[0][0], processNames[0][1]);
            }

            for (int i = 1; i < processNames.Length; i++)
            {
                if (windowHandle == IntPtr.Zero)
                {
                    return windowHandle;
                }

                windowHandle = NativeMethods.FindWindowEx(windowHandle, IntPtr.Zero, processNames[i][0], processNames[i][1]);
            }

            return windowHandle;
        }

        /// <summary>
        /// Gets data from the supplied process and window into the struct of type StructType.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <param name="windowHandle">Address of the window.</param>
        /// <param name="iconIndex">Index of the icon in the window.</param>
        /// <param name="structToFill">Reference of a struct to fill.</param>
        protected void FillIconStruct(int iconIndex, ref StructType structToFill)
        {
            if (!_bufferPointers.ContainsKey(ProcessHandle))
            {
                _bufferPointers.Add(ProcessHandle, AllocateBufferInProcess(ProcessHandle));
            }

            uint bytesRead = 0;
            NativeMethods.SendMessage(WindowHandle, _getIconsCode, new IntPtr(iconIndex), _bufferPointers[ProcessHandle]);
            NativeMethods.ReadProcessMemory(ProcessHandle, _bufferPointers[ProcessHandle], Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr((uint)Marshal.SizeOf(structToFill)), ref bytesRead);

            if (bytesRead != Marshal.SizeOf(structToFill))
            {
                throw new ExternalException("Read false amount of bytes.");
            }

            structToFill = Marshal.PtrToStructure<StructType>(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0));
        }

        /// <summary>
        /// Get an icons bounding rectangle in absolute positioning.
        /// </summary>
        /// <param name="iconIndex">Index of the icon in WindowHandle.</param>
        /// <returns>A filled rectangle structure, offset if needed.</returns>
        protected virtual Rectangle GetAbsoluteIconRectangle(int iconIndex)
        {
            var rectangle = new Rectangle();
            if (!_bufferPointers.ContainsKey(ProcessHandle))
            {
                _bufferPointers.Add(ProcessHandle, AllocateBufferInProcess(ProcessHandle));
            }

            uint bytesRead = 0;
            NativeMethods.SendMessage(WindowHandle, _getIconRectCode, new IntPtr(iconIndex), _bufferPointers[ProcessHandle]);
            NativeMethods.ReadProcessMemory(ProcessHandle, _bufferPointers[ProcessHandle], Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr((uint)Marshal.SizeOf(rectangle)), ref bytesRead);

            if (bytesRead != Marshal.SizeOf(rectangle))
            {
                throw new ExternalException("Read false amount of bytes.");
            }

            rectangle = Marshal.PtrToStructure<Rectangle>(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0));
            OffsetIconRectangleByParent(ref rectangle);
            return rectangle;
        }

        /// <summary>
        /// Yields the allocated buffer for this process.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <returns>Pointer to an allocated buffer.</returns>
        protected IntPtr GetBufferPointer(IntPtr processHandle)
        {
            if (!_bufferPointers.ContainsKey(processHandle))
            {
                _bufferPointers.Add(processHandle, AllocateBufferInProcess(processHandle));
            }

            return _bufferPointers[processHandle];
        }

        protected abstract int GetIconCount(ref ArtifactRuntimeInformation runtimeInformation);

        /// <summary>
        /// Gets the title of the icon with the given ID.
        /// </summary>
        /// <param name="index">Index of icon in parent window.</param>
        /// <param name="icon">Icon instance.</param>
        /// <returns>The icon title.</returns>
        protected abstract string GetIconTitle(int index, StructType icon);

        /// <summary>
        /// Ask sub-class to increase their own counter.
        /// </summary>
        /// <param name="runtimeInformation"></param>
        protected abstract void IncreaseIconCount(ref ArtifactRuntimeInformation runtimeInformation);

        /// <summary>
        /// Ask sub-class to initialize detection.
        /// </summary>
        /// <param name="runtimeInformation"></param>
        protected abstract void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation);

        /// <summary>
        /// Return a new (usable) instance of the icon struct.
        /// </summary>
        /// <returns></returns>
        protected abstract StructType InitIconStruct();

        /// <summary>
        /// Allocate memory in the given process.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <returns>The allocated memory's address.</returns>
        private IntPtr AllocateBufferInProcess(IntPtr processHandle)
        {
            return NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.Memory.RESERVE | NativeMethods.Memory.COMMIT, NativeMethods.Page.READWRITE);
        }

        /// <summary>
        /// Gets the icon count and iterates over all icons in the window handle.
        /// </summary>
        private void AnalizeIcons(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Get the desktop window's process to enumerate child windows.
            ProcessHandle = InitProcessHandle();
            // Count subwindows of window => count of icons.
            int iconCount = GetIconCount();

            var icon = InitIconStruct();

            try
            {
                // Loop through available desktop icons.
                for (int i = 0; i < iconCount; i++)
                {
                    string currentIconTitle = GetIconTitle(i, icon);
                    var iconRectangle = GetAbsoluteIconRectangle(i);

                    if (IconTitleMatches(currentIconTitle, ref runtimeInformation))
                    {
                        float visibility = CalculateWindowVisibility(iconRectangle, runtimeInformation.VisibleWindowOutlines.Values);
                        runtimeInformation.WindowsInformation.Add(new WindowInformation
                        {
                            BoundingArea = iconRectangle,
                            Title = currentIconTitle,
                            Visibility = visibility,
                            ZIndex = IconZIndex
                        });
                        IncreaseIconCount(ref runtimeInformation);

                        if (runtimeInformation.MaxWindowVisibilityPercentage < visibility)
                        {
                            runtimeInformation.MaxWindowVisibilityPercentage = visibility;
                        }
                    }
                }
            }
            finally
            {
                // Clean up unmanaged memory.
                CleanUnmanagedMemory();
            }
        }

        /// <summary>
        /// Clean up the buffer in given process.
        /// </summary>
        /// <param name="processHandle">Handle of the buffer's process.</param>
        private void CleanUnmanagedMemory()
        {
            foreach (var entry in _bufferPointers)
            {
                NativeMethods.VirtualFreeEx(entry.Key, entry.Value, UIntPtr.Zero, NativeMethods.Memory.RELEASE);
                NativeMethods.CloseHandle(entry.Key);
            }
        }

        /// <summary>
        /// Returns the icon cound for given window handle.
        /// </summary>
        /// <param name="windowHandle">Window address to get the icon count from.</param>
        /// <returns>Total count of icons in window.</returns>
        private int GetIconCount()
        {
            return (int)NativeMethods.SendMessage(WindowHandle, _getIconCountCode, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Check if an icon title contains a substring from the PossibleIconTitles.
        /// Ignoring the case.
        /// </summary>
        /// <param name="iconTitle">Obvious.</param>
        /// <returns>True if the icon title contains any substring.</returns>
        private bool IconTitleMatches(string iconTitle, ref ArtifactRuntimeInformation runtimeInformation)
        {
            return iconTitle.ContainsAny(runtimeInformation.PossibleIconSubstrings);
        }

        /// <summary>
        /// Retrieve a process handle by a given window.
        /// </summary>
        /// <returns>Pointer to the window's process.</returns>
        private IntPtr InitProcessHandle()
        {
            NativeMethods.GetWindowThreadProcessId(WindowHandle, out uint processId);
            return NativeMethods.OpenProcess(NativeMethods.ProcessVM.OPERATION | NativeMethods.ProcessVM.READ | NativeMethods.ProcessVM.WRITE, false, processId);
        }

        /// <summary>
        /// Add the parent window's offset to an icon rectangle.
        /// </summary>
        /// <param name="rectangle"></param>
        private void OffsetIconRectangleByParent(ref Rectangle rectangle)
        {
            var rect = new NativeStructures.RectangularOutline();
            NativeMethods.GetWindowRect(WindowHandle, ref rect);
            rectangle.Left += rect.left;
            rectangle.Right += rect.left;
            rectangle.Top += rect.top;
            rectangle.Bottom += rect.top;
        }
    }
}
