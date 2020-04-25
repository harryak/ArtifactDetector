using Emgu.CV;
using ItsApe.ArtifactDetector.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    [Serializable]
    public class ArtifactRuntimeInformation : ICloneable
    {
        [JsonIgnore]
        [NonSerialized]
        private ArtifactReferenceImageCache _referenceImages;

        /// <summary>
        /// Empty constructor for setting the properties later on.
        /// </summary>
        public ArtifactRuntimeInformation()
        { }

        /// <summary>
        /// Constructor taking the arguments that can be known on first instantiation.
        /// </summary>
        /// <param name="artifactName">Name of the artifact type.</param>
        /// <param name="possibleProcessNames"></param>
        /// <param name="possibleWindowTitles"></param>
        /// <param name="referenceImages"></param>
        public ArtifactRuntimeInformation(string artifactName, List<string> possibleProcessNames, List<string> possibleWindowTitles, ArtifactReferenceImageCache referenceImages)
        {
            ArtifactName = artifactName;
            PossibleProcessSubstrings = possibleProcessNames;
            PossibleWindowTitleSubstrings = possibleWindowTitles;
            ReferenceImages = referenceImages;
        }

        [JsonIgnore]
        public bool ActiveScreenChecked { get; set; } = false;

        /// <summary>
        /// Name of the artifact type, immutable once created.
        /// </summary>
        [JsonProperty("artifact_name")]
        public string ArtifactName { get; private set; }

        /// <summary>
        /// How many desktop icons have been found.
        /// </summary>
        [JsonIgnore]
        public int CountDesktopIcons { get; set; } = 0;

        /// <summary>
        /// How many installed programs have been found.
        /// </summary>
        [JsonIgnore]
        public int CountInstalledPrograms { get; set; } = 0;

        /// <summary>
        /// How many open windows have been found.
        /// </summary>
        [JsonIgnore]
        public int CountOpenWindows { get; set; } = 0;

        /// <summary>
        /// How many runnind processes have been found.
        /// </summary>
        [JsonIgnore]
        public int CountRunningProcesses { get; set; } = 0;

        /// <summary>
        /// How many tray icons have been found.
        /// </summary>
        [JsonIgnore]
        public int CountTrayIcons { get; set; } = 0;

        /// <summary>
        /// How many reference image matches have been found.
        /// </summary>
        [JsonIgnore]
        public int CountVisualFeautureMatches { get; set; } = 0;

        /// <summary>
        /// Store the visibility of the most visible window here.
        /// </summary>
        [JsonIgnore]
        public float MaxWindowVisibilityPercentage { get; set; } = 0f;

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        [JsonProperty("icon_titles")]
        [JsonConverter(typeof(StringToListConverter))]
        public List<string> PossibleIconSubstrings { get; internal set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        [JsonProperty("process_names")]
        [JsonConverter(typeof(StringToListConverter))]
        public List<string> PossibleProcessSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        [JsonProperty("program_names")]
        [JsonConverter(typeof(StringToListConverter))]
        public List<string> PossibleProgramSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        [JsonProperty("window_titles")]
        [JsonConverter(typeof(StringToListConverter))]
        public List<string> PossibleWindowTitleSubstrings { get; internal set; } = new List<string>();

        /// <summary>
        /// Information about the matching executables.
        /// </summary>
        [JsonIgnore]
        public List<string> ProgramExecutables { get; set; } = new List<string>();

        /// <summary>
        /// Image cache for reference image.
        /// </summary>
        [JsonIgnore]
        public ArtifactReferenceImageCache ReferenceImages { get => _referenceImages; set => _referenceImages = value; }

        /// <summary>
        /// Can hold currently visible windows.
        /// Index is the z-index (order) with 1 being the topmost.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, Rectangle> VisibleWindowOutlines { get; set; } = new Dictionary<int, Rectangle>();

        /// <summary>
        /// Window handles to look for.
        /// </summary>
        [JsonIgnore]
        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();

        /// <summary>
        /// For each matching window stores the handle and its visibility.
        /// </summary>
        [JsonIgnore]
        public List<WindowInformation> WindowsInformation { get; set; } = new List<WindowInformation>();

        /// <summary>
        /// Possibility to copy settings from another object.
        /// </summary>
        /// <param name="runtimeInformation">The object to get arguments from.</param>
        public object Clone()
        {
            return new ArtifactRuntimeInformation(ArtifactName, PossibleProcessSubstrings, PossibleWindowTitleSubstrings, ReferenceImages);
        }
    }
}