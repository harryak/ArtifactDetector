using ArbitraryArtifactDetector.DebugUtility;
using ArbitraryArtifactDetector.Detector.VisualFeatureExtractor;
using ArbitraryArtifactDetector.Helper;
using ArbitraryArtifactDetector.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArbitraryArtifactDetector.Detector
{
    /// <summary>
    /// This class manages type info of artifacts.
    /// It has an internal storage map to extract features for an artifact only once and can be saved to and retrieved
    /// from a binary data file.
    /// </summary>
    [Serializable]
    internal class ArtifactReferenceImageCache
    {
        /// <summary>
        /// The name of the artifact type this cache is for.
        /// </summary>
        public string _artifactType;

        /// <summary>
        /// Configuration: File extension for persistence file.
        /// </summary>
        [NonSerialized]
        private const string persistentFileExtension = ".bin";

        /// <summary>
        /// Logger for telling what is done.
        /// </summary>
        [NonSerialized]
        private ILogger _logger;

        /// <summary>
        /// File path to get raw images from.
        /// </summary>
        [NonSerialized]
        private FileInfo _persistentFilePath;

        /// <summary>
        /// Stopwatch for evaluation, might be null.
        /// </summary>
        [NonSerialized]
        private AADStopwatch _stopwatch;

        /// <summary>
        /// Instance of a feature extractor to process the raw images.
        /// </summary>
        [NonSerialized]
        private IVisualFeatureExtractor _visualFeatureExtractor;

        /// <summary>
        /// Needs a file path to operate in and an artifact artifactDetector.
        /// </summary>
        /// <param name="artifactType">Name of the artifact type this cache is for.</param>
        /// <param name="persistentFilePath">The path of all library resources.</param>
        /// <param name="visualFeatureExtrator">A visual feature extractor instance to get features from images.</param>
        /// <param name="logger">Logger to use for logging.</param>
        /// <param name="stopwatch">Optional, stopwatch for evaluation.</param>
        private ArtifactReferenceImageCache(Setup setup, IVisualFeatureExtractor visualFeatureExtrator, ILogger logger, AADStopwatch stopwatch = null)
        {
            _artifactType = setup.ArtifactTarget;

            VisualFeatureExtractor = visualFeatureExtrator ?? throw new ArgumentNullException(nameof(visualFeatureExtrator));

            // Make sure the path is set right.
            PersistentFilePath = new FileInfo(Path.Combine(setup.WorkingDirectory.FullName, ArtifactType + persistentFileExtension));

            // Instantiate library.
            ProcessedImages = new Dictionary<string, ProcessedImage>();

            Logger = logger;
            Stopwatch = stopwatch;

            DataChanged = false;
        }

        /// <summary>
        /// Getter for the artifact type name.
        /// </summary>
        public string ArtifactType { get => _artifactType; }

        /// <summary>
        /// Flag to tell whether the data in this cache has changed.
        /// </summary>
        public bool DataChanged { get; set; }

        /// <summary>
        /// Accessors for the logger. Need to be explicit to omit property from serialization.
        /// </summary>
        public ILogger Logger { get => _logger; private set => _logger = value; }

        /// <summary>
        /// Accessors for the file path. Need to be explicit to omit property from serialization.
        /// </summary>
        public FileInfo PersistentFilePath { get => _persistentFilePath; set => _persistentFilePath = value; }

        /// <summary>
        /// Accessors for the stopwatch. Need to be explicit to omit property from serialization.
        /// </summary>
        public AADStopwatch Stopwatch { get => _stopwatch; private set => _stopwatch = value; }

        /// <summary>
        /// Accessors for artifact detector. Need to be explicit to omit property from serialization.
        /// </summary>
        public IVisualFeatureExtractor VisualFeatureExtractor { get => _visualFeatureExtractor; set => _visualFeatureExtractor = value; }

        /// <summary>
        /// This map is a storage for already processed features.
        /// </summary>
        private Dictionary<string, ProcessedImage> ProcessedImages { get; }

        public ICollection<ProcessedImage> GetProcessedImages() {
            return ProcessedImages.Values;
        }

        /// <summary>
        /// Extracts a saved library from the given file.
        /// </summary>
        /// <param name="fileName">Filename to load the library from.</param>
        /// <param name="artifactDetector">A new artifact artifactDetector for the loaded instance.</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>The read artifact library.</returns>
        public static ArtifactReferenceImageCache GetInstance(Setup setup, IVisualFeatureExtractor artifactDetector, ILogger logger, AADStopwatch stopwatch = null)
        {
            // Start stopwatch if there is one.
            if (stopwatch != null)
                stopwatch.Restart();

            // Build filename from working directory and artifact target.
            string fileName = Path.Combine(setup.WorkingDirectory.FullName, setup.ArtifactTarget + persistentFileExtension);

            // New instance of this class to be filled.
            ArtifactReferenceImageCache artifactLibrary = null;

            // Try to read from file, fail if it doesn't exist or we don't have access rights.
            try
            {
                using (Stream stream = File.Open(fileName, FileMode.Open))
                {
                    var binaryFormatter = new BinaryFormatter();

                    artifactLibrary = (ArtifactReferenceImageCache) binaryFormatter.Deserialize(stream);
                    artifactLibrary.PersistentFilePath = new FileInfo(fileName);
                    artifactLibrary.VisualFeatureExtractor = artifactDetector;
                    artifactLibrary.Logger = logger;
                    artifactLibrary.Stopwatch = stopwatch;
                    artifactLibrary.DataChanged = false;

                    logger.LogInformation("Got processed image cache from file.");
                }
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException || exception is DirectoryNotFoundException || exception is UnauthorizedAccessException)
                {
                    logger.LogInformation("No processed image cache existing or not accessible, creating new instance.");
                    artifactLibrary = new ArtifactReferenceImageCache(setup, artifactDetector, logger, stopwatch);
                }
                throw;
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
        public void Save()
        {
            // We do not need to export anything, if nothing's changed.
            if (!DataChanged) return;

            // Set flag to false just in case.
            DataChanged = false;

            var binaryFormatter = new BinaryFormatter();
            FileHelper.WriteToFile(PersistentFilePath.FullName, stream => binaryFormatter.Serialize(stream, this), FileMode.Create);
        }

        /// <summary>
        /// Returns the artifact type given by its name.
        /// If the name is not found in the library, a new feature
        /// extraction is done.
        /// </summary>
        /// <param name="name">Name of the artifact</param>
        /// <param name="stopwatch">An optional stopwatch for evaluation.</param>
        /// <returns>The retrieved artifact type.</returns>
        public ProcessedImage GetProcessedImage(string filePath)
        {
            if (Stopwatch != null)
            {
                Stopwatch.Restart();
            }

            // Either read from library, or generate.
            if (!ProcessedImages.ContainsKey(filePath))
            {
                // Generate artifact object freshly.
                ProcessedImage image = VisualFeatureExtractor.ExtractFeatures(filePath);

                if (image != null)
                {
                    // Add to cache list.
                    ProcessedImages.Add(filePath, image);
                    // Save this to cache file.
                    Save();
                }
            }

            if (Stopwatch != null)
            {
                Stopwatch.Stop("artifacttype_retrieved");
                Logger.LogDebug("Retrieved artifact type in {0} ms.", Stopwatch.ElapsedMilliseconds);
            }

            return ProcessedImages[filePath];
        }
    }
}