/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using ArbitraryArtifactDetector.ArbitraryArtifactDetector.Detectors;
using ArbitraryArtifactDetector.Helper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArbitraryArtifactDetector.Model
{
    /// <summary>
    /// This class manages type info of artifacts.
    /// It has an internal storage map to extract features for
    /// an artifact only once and can be saved to and retrieved
    /// from a binary data file.
    /// </summary>
    [Serializable]
    class ArtifactLibrary
    {
        [NonSerialized]
        private IArtifactDetector _artifactDetector;
        [NonSerialized]
        private ILogger _logger;
        [NonSerialized]
        private string _filePath;

        private bool _dataChanged;

        /// <summary>
        /// This map is a storage for already processed features.
        /// </summary>
        private Dictionary<string, ArtifactType> Library { get; set; }
        private List<string> Types { get; set; }

        public string FilePath { get => _filePath; set => _filePath = value; }
        public IArtifactDetector ArtifactDetector { get => _artifactDetector; set => _artifactDetector = value; }
        public ILogger Logger { get => _logger; set => _logger = value; }
        public bool DataChanged { get => _dataChanged; set => _dataChanged = value; }

        /// <summary>
        /// Needs a file path to operate in and an artifact artifactDetector.
        /// </summary>
        /// <param name="filePath">The path of all library resources.</param>
        /// <param name="artifactDetector">An artifact artifactDetector instance to extract features.</param>
        /// <param name="_loggerFactory">Logger factory to get a new logger.</param>
        public ArtifactLibrary(string filePath, IArtifactDetector artifactDetector, ILoggerFactory _loggerFactory)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            // Make sure the path is set right.
            FilePath = FileHelper.AddDirectorySeparator(filePath);
            ArtifactDetector = artifactDetector ?? throw new ArgumentNullException(nameof(artifactDetector));

            // Instantiate library.
            Library = new Dictionary<string, ArtifactType>();

            Logger = _loggerFactory.CreateLogger("ArtifactLibrary");

            DataChanged = false;

            // Parse available artifact types from folder.
            ImportAvailableArtifactTypes();
        }

        /// <summary>
        /// Returns the artifact type given by its name.
        /// If the name is not found in the library, a new feature
        /// extraction is done.
        /// </summary>
        /// <param name="name">Name of the artifact</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <returns>The retrieved artifact type.</returns>
        public ArtifactType GetArtifactType(string name, VADStopwatch stopwatch = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!Types.Contains(name))
                throw new NotImplementedException("Artifact type was not found.");

            if (stopwatch != null)
                stopwatch.Restart();

            // Either read from library, or generate.
            if (!Library.ContainsKey(name))
            {
                // Generate artifact object freshly.
                Library[name] = new ArtifactType(_name: name);

                foreach (string imageFile in GetImageFilesForArtifactType(name))
                {
                    ProcessedImage image = ArtifactDetector.ExtractFeatures(imageFile);
                    if (image != null)
                    {
                        DataChanged = true;
                        Library[name].Images.Add(image);
                    }
                }
            }

            if (stopwatch != null)
            {
                stopwatch.Stop("artifacttype_retrieved");
                Logger.LogDebug("Retrieved artifact type in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return Library[name];
        }

        /// <summary>
        /// Extracts all available artifact types extracted from recipes in FilePath.
        /// </summary>
        private void ImportAvailableArtifactTypes()
        {
            Types = new List<string>();
            foreach (string file in Directory.EnumerateFiles(FilePath, "*.yml"))
            {
                // The type name is only the filename without extension.
                string type = Path.GetFileNameWithoutExtension(file);

                // Add directory separator plus "screenshot" to check if the corresponding folder exists.
                if (Directory.Exists(FileHelper.AddDirectorySeparator(FilePath + type) + "screenshot"))
                {
                    Types.Add(type);
                }
            }
        }

        private string[] GetImageFilesForArtifactType(string artifactType)
        {
            return Directory.GetFiles(FileHelper.AddDirectorySeparator(FilePath + artifactType) + "screenshot", "*.png");
        }

        /// <summary>
        /// Extracts a saved library from the given file.
        /// </summary>
        /// <param name="fileName">Filename to load the library from.</param>
        /// <param name="artifactDetector">A new artifact artifactDetector for the loaded instance.</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <param name="logger">A logging factory.</param>
        /// <returns>The read artifact library.</returns>
        public static ArtifactLibrary FromFile(string fileName, IArtifactDetector artifactDetector, VADStopwatch stopwatch = null, ILoggerFactory loggerFactory = null)
        {
            ILogger logger = loggerFactory.CreateLogger("ArtifactLibrary");

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            if (stopwatch != null)
                stopwatch.Restart();

            ArtifactLibrary artifactLibrary = null;

            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();

                artifactLibrary = (ArtifactLibrary) binaryFormatter.Deserialize(stream);
                artifactLibrary.FilePath = FileHelper.AddDirectorySeparator(Path.GetDirectoryName(fileName));
                artifactLibrary.ArtifactDetector = artifactDetector;
                artifactLibrary.Logger = logger;
                artifactLibrary.DataChanged = false;
            }

            if (stopwatch != null)
            {
                stopwatch.Stop("library_retrieved");
                logger.LogDebug("Retrieved full artifact library in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return artifactLibrary;
        }

        /// <summary>
        /// Exports this library to a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="filePath"></param>
        public void ExportToFile(string fileName, string filePath = null)
        {
            // We do not need to export anything, if nothing's changed.
            if (!DataChanged) return;

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Ensure we have a valid path.
            if (filePath == null)
                filePath = FilePath;
            else
                filePath = FileHelper.AddDirectorySeparator(filePath);

            // Set flag to false just in case.
            DataChanged = false;

            var binaryFormatter = new BinaryFormatter();
            FileHelper.WriteToFile(filePath + fileName, stream => binaryFormatter.Serialize(stream, this), FileMode.Create);
        }
    }
}
