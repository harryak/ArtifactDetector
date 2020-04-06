using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.Helpers;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// Common base class for icon detectors.
    /// <typeparam name="StructType">Type of the struct to fill (for marshalling).</typeparam>
    /// </summary>
    internal abstract class IconDetector<StructType> : BaseDetector where StructType : struct
    {
        /// <summary>
        /// Choose a buffer size which is large enough for the operations in this class.
        /// </summary>
        private const int BUFFER_SIZE = 0x1000;

        /// <summary>
        /// Buffer to use for getting data from other processes.
        /// </summary>
        private readonly byte[] _buffer = new byte[BUFFER_SIZE];

        /// <summary>
        /// Pointers to other processes' buffers-
        /// </summary>
        private readonly IDictionary<IntPtr, IntPtr> _bufferPointers = new Dictionary<IntPtr, IntPtr>();

        private readonly uint _getIconCountCode;
        private readonly Func<StructType, IntPtr> _getIconIdAction;
        private readonly uint _getIconsCode;
        private readonly uint _getIconTitleCode;

        protected IconDetector(uint getIconsCode, uint getIconCountCode, uint getIconTitleCode, Func<StructType, IntPtr> getIconIdAction)
        {
            _getIconsCode = getIconsCode;
            _getIconCountCode = getIconCountCode;
            _getIconTitleCode = getIconTitleCode;
            _getIconIdAction = getIconIdAction;
        }

        /// <summary>
        /// Allocate memory in the given process.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <returns>The allocated memory's address.</returns>
        protected IntPtr AllocateBufferInProcess(IntPtr processHandle)
        {
            return NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);
        }

        /// <summary>
        /// Clean up the buffer in given process.
        /// </summary>
        /// <param name="processHandle">Handle of the buffer's process.</param>
        protected void CleanUnmanagedMemory(IntPtr processHandle)
        {
            if (!_bufferPointers.ContainsKey(processHandle))
            {
                NativeMethods.VirtualFreeEx(processHandle, _bufferPointers[processHandle], UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(processHandle);
            }
        }

        /// <summary>
        /// Gets data from the supplied process and window into the struct of type StructType.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <param name="windowHandle">Address of the window.</param>
        /// <param name="iconIndex">Index of the icon in the window.</param>
        /// <param name="structToFill">Reference of a struct to fill.</param>
        protected void FillIconStruct(IntPtr processHandle, IntPtr windowHandle, int iconIndex, ref StructType structToFill)
        {
            if (!_bufferPointers.ContainsKey(processHandle))
            {
                _bufferPointers.Add(processHandle, AllocateBufferInProcess(processHandle));
            }

            uint bytesRead = 0;
            NativeMethods.SendMessage(windowHandle, _getIconsCode, new IntPtr(iconIndex), _bufferPointers[processHandle]);
            NativeMethods.ReadProcessMemory(processHandle, _bufferPointers[processHandle], Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr((uint)Marshal.SizeOf(structToFill)), ref bytesRead);

            if (bytesRead != Marshal.SizeOf(structToFill))
            {
                throw new ExternalException("Read false amount of bytes.");
            }

            structToFill = Marshal.PtrToStructure<StructType>(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0));
        }

        protected DetectorResponse FindIcon(IntPtr windowHandle, ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Stopwatch for evaluation.
            StartStopwatch();

            // Get the desktop window's process to enumerate child windows.
            var processHandle = GetProcessHandle(windowHandle);
            var currentIcon = new StructType();

            // Count subwindows of desktop => count of icons.
            int iconCount = GetIconCount(windowHandle);
            try
            {
                // Loop through available tray icons.
                for (int i = 0; i < iconCount; i++)
                {
                    // Get TBBUTTON struct filled.
                    FillIconStruct(processHandle, windowHandle, i, ref currentIcon);
                    string currentIconTitle = GetIconTitle(processHandle, windowHandle, _getIconIdAction(currentIcon));

                    // Check if the current title has a substring in the possible titles.
                    if (IconTitleMatches(currentIconTitle, runtimeInformation.PossibleIconTitles))
                    {
                        // Memory cleanup in finally clause is always executed.
                        StopStopwatch("Got icons in {0}ms.");
                        Logger.LogInformation("Found matching icon.");
                        return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
                    }
                }
            }
            finally
            {
                CleanUnmanagedMemory(processHandle);
            }

            StopStopwatch("Got icons in {0}ms.");
            Logger.LogInformation("Found no matching icons.");
            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Returns the icon cound for given window handle.
        /// </summary>
        /// <param name="windowHandle">Window address to get the icon count from.</param>
        /// <returns>Total count of icons in window.</returns>
        protected int GetIconCount(IntPtr windowHandle)
        {
            return (int)NativeMethods.SendMessage(windowHandle, _getIconCountCode, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Gets the title of the icon with the given ID.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <param name="windowHandle">Address of the window.</param>
        /// <param name="messageCode">Which message to send to the window.</param>
        /// <param name="iconId">Internal address of the icon.</param>
        /// <returns></returns>
        protected string GetIconTitle(IntPtr processHandle, IntPtr windowHandle, IntPtr iconId)
        {
            if (!_bufferPointers.ContainsKey(processHandle))
            {
                _bufferPointers.Add(processHandle, AllocateBufferInProcess(processHandle));
            }

            uint bytesRead = 0;
            int titleLength = (int) NativeMethods.SendMessage(windowHandle, _getIconTitleCode, iconId, _bufferPointers[processHandle]);
            NativeMethods.ReadProcessMemory(processHandle, _bufferPointers[processHandle], Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), new UIntPtr(BUFFER_SIZE), ref bytesRead);

            return Marshal.PtrToStringUni(Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0), titleLength);
        }

        /// <summary>
        /// Retrieve a process handle by a given window.
        /// </summary>
        /// <param name="windowHandle">Address of a window.</param>
        /// <returns>Pointer to the window's process.</returns>
        protected IntPtr GetProcessHandle(IntPtr windowHandle)
        {
            NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
            return NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, processId);
        }

        /// <summary>
        /// Retrieve window address from a list of nested processes.
        /// </summary>
        /// <param name="processNames">Nested process names.</param>
        /// <returns>The window's address or IntPtr.Zero on failure.</returns>
        protected IntPtr GetWindowHandle(string[] processNames)
        {
            var windowHandle = IntPtr.Zero;

            if (processNames.Length > 0)
            {
                windowHandle = NativeMethods.FindWindow(processNames[0], null);
            }

            if (processNames.Length > 1)
            {
                for (int i = 1; i < processNames.Length; i++)
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        return windowHandle;
                    }

                    windowHandle = NativeMethods.FindWindowEx(windowHandle, IntPtr.Zero, processNames[i], null);
                }
            }

            return windowHandle;
        }

        /// <summary>
        /// Check if an icon title contains a substring from the PossibleIconTitles.
        /// Ignoring the case.
        /// </summary>
        /// <param name="iconTitle">Obvious.</param>
        /// <returns>True if the icon title contains any substring.</returns>
        protected bool IconTitleMatches(string iconTitle, IList<string> PossibleIconTitles)
        {
            return iconTitle.ContainsAny(PossibleIconTitles, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
