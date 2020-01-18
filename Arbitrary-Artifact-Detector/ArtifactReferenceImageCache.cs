﻿using ArbitraryArtifactDetector.DebugUtility;
using ArbitraryArtifactDetector.Detector.VisualFeatureExtractor;
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
    class ArtifactReferenceImageCache
    {
        [NonSerialized]
        private IVisualFeatureExtractor _artifactDetector;
        [NonSerialized]
        private ILogger _logger;
        [NonSerialized]
        private string _filePath;
        [NonSerialized]
        private AADStopwatch _stopwatch;

        /// <summary>
        /// This map is a storage for already processed features.
        /// </summary>
        private Dictionary<string, ArtifactConfiguration> Library { get; set; }
        private List<string> Types { get; set; }

        public string FilePath { get => _filePath; set => _filePath = value; }
        public IVisualFeatureExtractor ArtifactDetector { get => _artifactDetector; set => _artifactDetector = value; }
        public ILogger Logger { get => _logger; private set => _logger = value; }
        public AADStopwatch Stopwatch { get => _stopwatch; private set => _stopwatch = value; }

        public bool DataChanged { get; set; }

        /// <summary>
        /// Needs a file path to operate in and an artifact artifactDetector.
        /// </summary>
        /// <param name="filePath">The path of all library resources.</param>
        /// <param name="artifactDetector">An artifact artifactDetector instance to extract features.</param>
        /// <param name="_loggerFactory">Logger factory to get a new logger.</param>
        public ArtifactReferenceImageCache(string filePath, IVisualFeatureExtractor artifactDetector, ILogger logger, AADStopwatch stopwatch)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            // Make sure the path is set right.
            FilePath = FileHelper.AddDirectorySeparator(filePath);
            ArtifactDetector = artifactDetector ?? throw new ArgumentNullException(nameof(artifactDetector));

            // Instantiate library.
            Library = new Dictionary<string, ArtifactConfiguration>();

            Logger = logger;
            Stopwatch = stopwatch;

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
        public ArtifactConfiguration GetArtifactType(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!Types.Contains(name))
                throw new NotImplementedException("Artifact type was not found.");

            if (Stopwatch != null)
            {
                Stopwatch.Restart();
            }

            // Either read from library, or generate.
            if (!Library.ContainsKey(name))
            {
                // Generate artifact object freshly.
                Library[name] = null;// new ArtifactConfiguration(artifactName: name);

                foreach (string imageFile in GetImageFilesForArtifactType(name))
                {
                    ProcessedImage image = ArtifactDetector.ExtractFeatures(imageFile);
                    if (image != null)
                    {
                        DataChanged = true;
                        Library[name].ReferenceImages.Add(image);
                    }
                }
            }

            if (Stopwatch != null)
            {
                Stopwatch.Stop("artifacttype_retrieved");
                Logger.LogDebug("Retrieved artifact type in {0} ms.", Stopwatch.ElapsedMilliseconds);
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
        public static ArtifactReferenceImageCache FromFile(string fileName, IVisualFeatureExtractor artifactDetector, AADStopwatch stopwatch = null, ILoggerFactory loggerFactory = null)
        {
            ILogger logger = loggerFactory.CreateLogger("ArtifactLibrary");

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            if (stopwatch != null)
                stopwatch.Restart();

            ArtifactReferenceImageCache artifactLibrary = null;

            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();

                artifactLibrary = (ArtifactReferenceImageCache) binaryFormatter.Deserialize(stream);
                artifactLibrary.FilePath = FileHelper.AddDirectorySeparator(Path.GetDirectoryName(fileName));
                artifactLibrary.ArtifactDetector = artifactDetector;
                artifactLibrary.Logger = logger;
                artifactLibrary.Stopwatch = stopwatch;
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
