using System;
using System.Collections.Generic;

namespace ItsApe.OpenWindowDetector.Models
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    [Serializable]
    internal class ArtifactRuntimeInformation
    {   
        public bool ActiveScreenChecked { get; set; } = false;

        /// <summary>
        /// Name of the artifact type, immutable once created.
        /// </summary>
        public string ArtifactName { get; private set; }

        /// <summary>
        /// How many desktop icons have been found.
        /// </summary>
        public int CountDesktopIcons { get; set; } = 0;

        /// <summary>
        /// How many installed programs have been found.
        /// </summary>
        public int CountInstalledPrograms { get; set; } = 0;

        /// <summary>
        /// How many open windows have been found.
        /// </summary>
        public int CountOpenWindows { get; set; } = 0;

        /// <summary>
        /// How many runnind processes have been found.
        /// </summary>
        public int CountRunningProcesses { get; set; } = 0;

        /// <summary>
        /// How many tray icons have been found.
        /// </summary>
        public int CountTrayIcons { get; set; } = 0;

        /// <summary>
        /// How many reference image matches have been found.
        /// </summary>
        public int CountVisualFeautureMatches { get; set; } = 0;

        /// <summary>
        /// Store the visibility of the most visible window here.
        /// </summary>
        public float MaxWindowVisibilityPercentage { get; set; } = 0f;

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        public List<string> PossibleIconSubstrings { get; internal set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        public List<string> PossibleProcessSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        public List<string> PossibleProgramSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        public List<string> PossibleWindowTitleSubstrings { get; internal set; } = new List<string>();

        /// <summary>
        /// Information about the matching executables.
        /// </summary>
        public List<string> ProgramExecutables { get; set; } = new List<string>();

        /// <summary>
        /// Can hold currently visible windows.
        /// Index is the z-index (order) with 1 being the topmost.
        /// </summary>
        public Dictionary<int, Rectangle> VisibleWindowOutlines { get; set; } = new Dictionary<int, Rectangle>();

        /// <summary>
        /// Window handles to look for.
        /// </summary>
        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();

        /// <summary>
        /// For each matching window stores the handle and its visibility.
        /// </summary>
        public List<WindowInformation> WindowsInformation { get; set; } = new List<WindowInformation>();
    }
}
