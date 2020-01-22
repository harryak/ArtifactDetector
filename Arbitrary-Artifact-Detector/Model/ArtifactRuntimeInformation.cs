using Emgu.CV;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    internal class ArtifactRuntimeInformation
    {
        /// <summary>
        /// Name of the artifact type, immutable once created.
        /// </summary>
        public string ArtifactName { get; private set; }

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        public IList<string> PossibleProcessNames { get; set; } = new List<string>();

        /// <summary>
        /// Screenshots of the matching windows.
        /// </summary>
        public IDictionary<int, Mat> Screenshots { get; set; } = new Dictionary<int, Mat>();

        /// <summary>
        /// Information about the matching windows.
        /// </summary>
        public IDictionary<IntPtr, WindowToplevelInformation> MatchingWindowsInformation { get; set; } = new Dictionary<IntPtr, WindowToplevelInformation>();

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        public IList<string> PossibleWindowTitles { get; internal set; } = new List<string>();

        public void PreFillWith(ArtifactRuntimeInformation runtimeInformation)
        {
            PossibleProcessNames = runtimeInformation.PossibleProcessNames;
            Screenshots = runtimeInformation.Screenshots;
            MatchingWindowsInformation = runtimeInformation.MatchingWindowsInformation;
            PossibleWindowTitles = runtimeInformation.PossibleWindowTitles;
        }
    }
}