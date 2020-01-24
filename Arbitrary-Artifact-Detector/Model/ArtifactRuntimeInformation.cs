﻿using ArbitraryArtifactDetector.Detector;
using Emgu.CV;
using System;
using System.Collections.Generic;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    internal class ArtifactRuntimeInformation : ICloneable
    {
        /// <summary>
        /// Constructor taking the arguments that can be known on first instantiation.
        /// </summary>
        /// <param name="artifactName">Name of the artifact type.</param>
        /// <param name="possibleProcessNames"></param>
        /// <param name="possibleWindowTitles"></param>
        /// <param name="referenceImages"></param>
        public ArtifactRuntimeInformation(string artifactName, IList<string> possibleProcessNames, IList<string> possibleWindowTitles, ArtifactReferenceImageCache referenceImages)
        {
            ArtifactName = artifactName;
            PossibleProcessNames = possibleProcessNames;
            PossibleWindowTitles = possibleWindowTitles;
            ReferenceImages = referenceImages;
        }

        /// <summary>
        /// Name of the artifact type, immutable once created.
        /// </summary>
        public string ArtifactName { get; private set; }

        /// <summary>
        /// Information about the matching windows.
        /// </summary>
        public IDictionary<IntPtr, WindowToplevelInformation> MatchingWindowsInformation { get; set; } = new Dictionary<IntPtr, WindowToplevelInformation>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        public IList<string> PossibleProcessNames { get; set; } = new List<string>();

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        public IList<string> PossibleWindowTitles { get; internal set; } = new List<string>();

        /// <summary>
        /// Image cache for reference image.
        /// </summary>
        public ArtifactReferenceImageCache ReferenceImages { get; set; }

        /// <summary>
        /// Screenshots of the matching windows.
        /// </summary>
        public IDictionary<int, Mat> Screenshots { get; set; } = new Dictionary<int, Mat>();

        /// <summary>
        /// Possibility to copy settings from another object.
        /// </summary>
        /// <param name="runtimeInformation">The object to get arguments from.</param>
        public object Clone()
        {
            return new ArtifactRuntimeInformation(ArtifactName, PossibleProcessNames, PossibleWindowTitles, ReferenceImages);
        }
    }
}