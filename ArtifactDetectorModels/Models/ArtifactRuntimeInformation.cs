using ItsApe.ArtifactDetector.Converters;
using MessagePack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// This class contains information on an artifact collected during the runtime by various detectors.
    /// </summary>
    [MessagePackObject]
    public class ArtifactRuntimeInformation : ICloneable
    {
        [JsonIgnore]
        [IgnoreMember]
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

        /// <summary>
        /// Name of the artifact type, immutable once created.
        /// </summary>
        [JsonProperty("artifact_name")]
        [Key(0)]
        public string ArtifactName { get; set; }

        #region artifact counts

        /// <summary>
        /// How many desktop icons have been found.
        /// </summary>
        [JsonIgnore]
        [Key(1)]
        public int CountDesktopIcons { get; set; } = 0;

        /// <summary>
        /// How many installed programs have been found.
        /// </summary>
        [JsonIgnore]
        [Key(2)]
        public int CountInstalledPrograms { get; set; } = 0;

        /// <summary>
        /// How many open windows have been found.
        /// </summary>
        [JsonIgnore]
        [Key(3)]
        public int CountOpenWindows { get; set; } = 0;

        /// <summary>
        /// How many runnind processes have been found.
        /// </summary>
        [JsonIgnore]
        [Key(4)]
        public int CountRunningProcesses { get; set; } = 0;

        /// <summary>
        /// How many tray icons have been found.
        /// </summary>
        [JsonIgnore]
        [Key(5)]
        public int CountTrayIcons { get; set; } = 0;

        /// <summary>
        /// How many reference image matches have been found.
        /// </summary>
        [JsonIgnore]
        [Key(6)]
        public int CountVisualFeautureMatches { get; set; } = 0;

        #endregion artifact counts

        #region watch task arguments

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        [JsonProperty("icon_titles")]
        [JsonConverter(typeof(StringToListConverter))]
        [Key(7)]
        public List<string> PossibleIconSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        [JsonProperty("process_names")]
        [JsonConverter(typeof(StringToListConverter))]
        [Key(8)]
        public List<string> PossibleProcessSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible names of the processes.
        /// </summary>
        [JsonProperty("program_names")]
        [JsonConverter(typeof(StringToListConverter))]
        [Key(9)]
        public List<string> PossibleProgramSubstrings { get; set; } = new List<string>();

        /// <summary>
        /// Possible (fragments of the) titles of the windows to get.
        /// </summary>
        [JsonProperty("window_titles")]
        [JsonConverter(typeof(StringToListConverter))]
        [Key(10)]
        public List<string> PossibleWindowTitleSubstrings { get; set; } = new List<string>();

        #endregion watch task arguments

        #region detected information

        /// <summary>
        /// Store the visibility of the most visible window here.
        /// </summary>
        [JsonIgnore]
        [Key(11)]
        public float MaxWindowVisibilityPercentage { get; set; } = 0f;

        /// <summary>
        /// Information about the matching executables.
        /// </summary>
        [JsonIgnore]
        [Key(12)]
        public List<string> ProgramExecutables { get; set; } = new List<string>();

        /// <summary>
        /// Image cache for reference image.
        /// </summary>
        [JsonIgnore]
        [IgnoreMember]
        public ArtifactReferenceImageCache ReferenceImages { get => _referenceImages; set => _referenceImages = value; }

        /// <summary>
        /// Can hold currently visible windows.
        /// Index is the z-index (order) with 1 being the topmost.
        /// </summary>
        [JsonIgnore]
        [Key(13)]
        public Dictionary<int, Rectangle> VisibleWindowOutlines { get; set; } = new Dictionary<int, Rectangle>();

        /// <summary>
        /// Window handles to look for.
        /// </summary>
        [JsonIgnore]
        [Key(14)]
        [MessagePackFormatter(typeof(IntPtrListFormatter))]
        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();

        /// <summary>
        /// For each matching window stores the handle and its visibility.
        /// </summary>
        [JsonIgnore]
        [Key(15)]
        public List<WindowInformation> WindowsInformation { get; set; } = new List<WindowInformation>();

        #endregion detected information

        /// <summary>
        /// Control information for the external detector process.
        /// </summary>
        [JsonIgnore]
        [Key(16)]
        public ExternalProcessCommand ProcessCommand { get; set; } = ExternalProcessCommand.None;

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