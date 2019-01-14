/**
 * Written by Felix Rossmann, "rossmann@cs.uni-bonn.de".
 * 
 * For license, please see "License-LGPL.txt".
 */

using ArtifactDetector.ArtifactDetector;
using ArtifactDetector.Helper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArtifactDetector.Model
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

        /// <summary>
        /// This map is a storage for already processed features.
        /// </summary>
        private Dictionary<string, ArtifactType> Library { get; set; }

        public string FilePath { get => _filePath; set => _filePath = value; }
        public IArtifactDetector ArtifactDetector { get => _artifactDetector; set => _artifactDetector = value; }
        public ILogger Logger { get => _logger; set => _logger = value; }

        /// <summary>
        /// Needs a file path to operate in and an artifact detector.
        /// </summary>
        /// <param name="filePath">The path of all library resources.</param>
        /// <param name="artifactDetector">An artifact detector instance to extract features.</param>
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
        }

        /// <summary>
        /// Returns the artifact type given by its name.
        /// If the name is not found in the library, a new feature
        /// extraction is done.
        /// </summary>
        /// <param name="name">Name of the artifact</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <returns>The retrieved artifact type.</returns>
        public ArtifactType GetArtifactType(string name, Stopwatch stopwatch = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (stopwatch != null)
                stopwatch.Restart();

            // Either read from library, or generate.
            if (!Library.ContainsKey(name))
            {
                //TODO: Read from different file.
                Library[name] = new ArtifactType(ArtifactDetector.ExtractFeatures(FilePath + name + ".jpg"));
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                Logger.LogDebug("Retrieved artifact type in {0} ms.", stopwatch.ElapsedMilliseconds);
            }

            return Library[name];
        }

        /// <summary>
        /// Extracts a saved library from the given file.
        /// </summary>
        /// <param name="fileName">Filename to load the library from.</param>
        /// <param name="artifactDetector">A new artifact detector for the loaded instance.</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <param name="logger">A logging factory.</param>
        /// <returns>The read artifact library.</returns>
        public static ArtifactLibrary FromFile(string fileName, IArtifactDetector artifactDetector, Stopwatch stopwatch = null, ILoggerFactory loggerFactory = null)
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
                artifactLibrary.FilePath = Path.GetFullPath(fileName);
                artifactLibrary.ArtifactDetector = artifactDetector;
                artifactLibrary.Logger = logger;
            }

            if (stopwatch != null)
            {
                stopwatch.Stop();
                if (logger != null)
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
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            // Ensure we have a valid path.
            if (filePath == null)
                filePath = FilePath;
            else
                filePath = FileHelper.AddDirectorySeparator(filePath);

            using (Stream stream = File.Open(filePath + fileName, FileMode.Create))
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, this);
                stream.Close();
            }
        }
    }
}
