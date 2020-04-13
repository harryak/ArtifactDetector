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
        protected const int BUFFER_SIZE = 0x1100;

        /// <summary>
        /// Buffer to use for getting data from other processes.
        /// </summary>
        protected readonly byte[] _buffer = new byte[BUFFER_SIZE];

        /// <summary>
        /// Pointers to other processes' buffers-
        /// </summary>
        private readonly IDictionary<IntPtr, IntPtr> _bufferPointers = new Dictionary<IntPtr, IntPtr>();

        /// <summary>
        /// Windows API code for SendMessage to get the icon count.
        /// </summary>
        private readonly uint _getIconCountCode;

        /// <summary>
        /// Windows API code for SendMessage to get the icons.
        /// </summary>
        private readonly uint _getIconsCode;

        /// <summary>
        /// Counter for matching icons.
        /// </summary>
        private int foundMatches;

        /// <summary>
        /// Constructor to switch between icon types.
        /// </summary>
        /// <param name="getIconsCode">Windows API code for SendMessage.</param>
        /// <param name="getIconCountCode">Windows API code for SendMessage.</param>
        /// <param name="windowHandle">Handle of the icons' parent window.</param>
        protected IconDetector(uint getIconsCode, uint getIconCountCode, IntPtr windowHandle)
        {
            _getIconsCode = getIconsCode;
            _getIconCountCode = getIconCountCode;

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
        /// Copy from runtime information.
        /// </summary>
        private IList<string> PossibleIconSubstrings { get; set; }

        /// <summary>
        /// Find the artifact provided by the runtime information.
        /// </summary>
        /// <param name="runtimeInformation">Information must contain "possibleIconTitles" for this to work.</param>
        /// <returns>Response based on whether the artifact was found.</returns>
        public override DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation)
        {
            Logger.LogInformation("Detecting icons now.");

            // This error is really unlikely.
            if (WindowHandle == IntPtr.Zero)
            {
                Logger.LogError("The window handle is not available.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
            }

            if (runtimeInformation.PossibleIconSubstrings.Count < 1)
            {
                Logger.LogInformation("No possible icon titles given for detector. Could not find matching icons.");
                return new DetectorResponse() { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible };
            }

            // Stopwatch for evaluation.
            StartStopwatch();

            InitializeDetection(ref runtimeInformation);
            AnalizeIcons();

            if (foundMatches > 0)
            {
                StopStopwatch("Got icons in {0}ms.");
                Logger.LogInformation("Found {0} matching icons.", foundMatches);
                return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Certain };
            }

            StopStopwatch("Got icons in {0}ms.");
            Logger.LogInformation("Found no matching icons.");
            return new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible };
        }

        /// <summary>
        /// Initialize (or reset) the detection for FindArtifact.
        /// </summary>
        /// <param name="runtimeInformation">Reference to object to initialize from.</param>
        public override void InitializeDetection(ref ArtifactRuntimeInformation runtimeInformation)
        {
            foundMatches = 0;
            PossibleIconSubstrings = runtimeInformation.PossibleIconSubstrings;
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
        /// Allocate memory in the given process.
        /// </summary>
        /// <param name="processHandle">Address of the process.</param>
        /// <returns>The allocated memory's address.</returns>
        protected IntPtr AllocateBufferInProcess(IntPtr processHandle)
        {
            return NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, new UIntPtr(BUFFER_SIZE), NativeMethods.MEM.RESERVE | NativeMethods.MEM.COMMIT, NativeMethods.PAGE.READWRITE);
        }

        /// <summary>
        /// Gets the icon count and iterates over all icons in the window handle.
        /// </summary>
        protected void AnalizeIcons()
        {
            // Get the desktop window's process to enumerate child windows.
            ProcessHandle = InitProcessHandle();
            // Count subwindows of window => count of icons.
            int iconCount = GetIconCount(WindowHandle);

            var icon = InitIconStruct();

            try
            {
                // Loop through available desktop icons.
                for (int i = 0; i < iconCount; i++)
                {
                    if (IconMatches(i, icon))
                    {
                        foundMatches++;
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
        protected void CleanUnmanagedMemory()
        {
            foreach (var entry in _bufferPointers)
            {
                NativeMethods.VirtualFreeEx(entry.Key, entry.Value, UIntPtr.Zero, NativeMethods.MEM.RELEASE);
                NativeMethods.CloseHandle(entry.Key);
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
        /// <param name="index">Index of icon in parent window.</param>
        /// <param name="icon">Icon instance.</param>
        /// <returns>The icon title.</returns>
        protected abstract string GetIconTitle(int index, StructType icon);

        /// <summary>
        /// Check if the given icon at the index matches the titles from runtime information.
        /// </summary>
        /// <param name="index">Index of icon in parent window.</param>
        /// <param name="icon">Icon structure.</param>
        /// <returns>True if the icon matches.</returns>
        protected bool IconMatches(int index, StructType icon)
        {
            string currentIconTitle = GetIconTitle(index, icon);

            // Check if the current title has a substring in the possible titles.
            return IconTitleMatches(currentIconTitle);
        }

        /// <summary>
        /// Check if an icon title contains a substring from the PossibleIconTitles.
        /// Ignoring the case.
        /// </summary>
        /// <param name="iconTitle">Obvious.</param>
        /// <returns>True if the icon title contains any substring.</returns>
        protected bool IconTitleMatches(string iconTitle)
        {
            return iconTitle.ContainsAny(PossibleIconSubstrings);
        }

        /// <summary>
        /// Return a new (usable) instance of the icon struct.
        /// </summary>
        /// <returns></returns>
        protected abstract StructType InitIconStruct();

        /// <summary>
        /// Retrieve a process handle by a given window.
        /// </summary>
        /// <param name="windowHandle">Address of a window.</param>
        /// <returns>Pointer to the window's process.</returns>
        protected IntPtr InitProcessHandle()
        {
            NativeMethods.GetWindowThreadProcessId(WindowHandle, out uint processId);
            return NativeMethods.OpenProcess(NativeMethods.PROCESS_VM.OPERATION | NativeMethods.PROCESS_VM.READ | NativeMethods.PROCESS_VM.WRITE, false, processId);
        }
    }
}
