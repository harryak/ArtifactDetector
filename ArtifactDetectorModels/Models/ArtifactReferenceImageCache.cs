using ItsApe.ArtifactDetector.Detectors.VisualFeatureExtractor;
using ItsApe.ArtifactDetector.Helpers;
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ItsApe.ArtifactDetector.Models
{
    /// <summary>
    /// This class manages type info of artifacts.
    /// It has an internal storage map to extract features for an artifact only once and can be saved to and retrieved
    /// from a binary data file.
    /// </summary>
    [Serializable]
    public class ArtifactReferenceImageCache
    {
        /// <summary>
        /// The name of the artifact type this cache is for.
        /// </summary>
        public string _artifactType;

        /// <summary>
        /// Configuration: File extension for persistence file.
        /// </summary>
        [NonSerialized]
        [IgnoreMember]
        private const string PersistentFileExtension = ".bin";

        /// <summary>
        /// Logger for telling what is done.
        /// </summary>
        [NonSerialized]
        [IgnoreMember]
        private ILogger _logger;

        /// <summary>
        /// File path to get raw images from.
        /// </summary>
        [NonSerialized]
        [IgnoreMember]
        private FileInfo _persistentFilePath;

        /// <summary>
        /// Instance of a feature extractor to process the raw images.
        /// </summary>
        [NonSerialized]
        [IgnoreMember]
        private IVisualFeatureExtractor _visualFeatureExtractor;

        /// <summary>
        /// Needs a file path to operate in and an artifact artifactDetector.
        /// </summary>
        /// <param name="artifactType">Name of the artifact type this cache is for.</param>
        /// <param name="workingDirectoryPath">Where to work in.</param>
        /// <param name="visualFeatureExtrator">A visual feature extractor instance to get features from images.</param>
        /// <param name="logger">Logger to use for logging.</param>
        private ArtifactReferenceImageCache(string artifactType, string workingDirectoryPath, IVisualFeatureExtractor visualFeatureExtrator, ILogger logger)
        {
            _artifactType = artifactType;

            VisualFeatureExtractor = visualFeatureExtrator ?? throw new ArgumentNullException(nameof(visualFeatureExtrator));

            // Make sure the path is set right.
            PersistentFilePath = new FileInfo(Path.Combine(workingDirectoryPath, ArtifactType + PersistentFileExtension));

            // Instantiate library.
            ProcessedImages = new Dictionary<string, ProcessedImage>();

            Logger = logger;

            DataChanged = false;
        }

        /// <summary>
        /// Getter for the artifact type name.
        /// </summary>
        public string ArtifactType { get => _artifactType; }

        /// <summary>
        /// Accessor for count of processed images.
        /// </summary>
        [IgnoreMember]
        public int ImagesCount => ProcessedImages != null ? ProcessedImages.Count : 0;

        /// <summary>
        /// Accessors for the file path. Need to be explicit to omit property from serialization.
        /// </summary>
        public FileInfo PersistentFilePath { get => _persistentFilePath; private set => _persistentFilePath = value; }

        /// <summary>
        /// Accessors for artifact detector. Need to be explicit to omit property from serialization.
        /// </summary>
        public IVisualFeatureExtractor VisualFeatureExtractor { get => _visualFeatureExtractor; set => _visualFeatureExtractor = value; }

        /// <summary>
        /// Flag to tell whether the data in this cache has changed.
        /// </summary>
        private bool DataChanged { get; set; }

        /// <summary>
        /// Accessors for the logger. Need to be explicit to omit property from serialization.
        /// </summary>
        private ILogger Logger { get => _logger; set => _logger = value; }

        /// <summary>
        /// This map is a storage for already processed features.
        /// </summary>
        private Dictionary<string, ProcessedImage> ProcessedImages { get; }

        /// <summary>
        /// If the filename is known directly one can deserialize it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="setup"></param>
        /// <param name="artifactDetector"></param>
        /// <returns></returns>
        public static ArtifactReferenceImageCache FromFile(string fileName, ILogger logger, IVisualFeatureExtractor artifactDetector)
        {
            ArtifactReferenceImageCache artifactLibrary = null;

            using (Stream stream = File.Open(fileName, FileMode.Open))
            {
                var binaryFormatter = new BinaryFormatter();

                artifactLibrary = (ArtifactReferenceImageCache) binaryFormatter.Deserialize(stream);
                artifactLibrary.PersistentFilePath = new FileInfo(fileName);
                artifactLibrary.VisualFeatureExtractor = artifactDetector;
                artifactLibrary.Logger = logger;
                artifactLibrary.DataChanged = false;

                logger.LogInformation("Got processed image cache from file.");
            }

            return artifactLibrary;
        }

        /// <summary>
        /// Extracts a saved library from the given file.
        /// </summary>
        /// <param name="artifactType">Name of the artifact type this cache is for.</param>
        /// <param name="setup">Setup of this application.</param>
        /// <param name="visualFeatureExtrator">A visual feature extractor instance to get features from images.</param>
        /// <param name="logger">Logger to use for logging.</param>
        /// <returns>A cached or new instance of the cache.</returns>
        public static ArtifactReferenceImageCache GetInstance(string artifactType, ILogger logger, string workingDirectoryName, IVisualFeatureExtractor artifactDetector)
        {
            // Build filename from working directory and artifact target.
            string fileName = Uri.UnescapeDataString(
                Path.Combine(workingDirectoryName,
                artifactType + PersistentFileExtension));

            // New instance of this class to be filled.
            ArtifactReferenceImageCache artifactLibrary = null;

            // Try to read from file, fail if it doesn't exist or we don't have access rights.
            try
            {
                artifactLibrary = FromFile(fileName, logger, artifactDetector);
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException || exception is DirectoryNotFoundException || exception is UnauthorizedAccessException)
                {
                    logger.LogInformation("No processed image cache existing or not accessible, creating new instance.");
                    artifactLibrary = new ArtifactReferenceImageCache(artifactType, workingDirectoryName, artifactDetector, logger)
                    {
                        DataChanged = true
                    };
                    artifactLibrary.Save();
                }
                else
                {
                    throw;
                }
            }

            return artifactLibrary;
        }

        /// <summary>
        /// Returns the processed image for the image stored at the given file path.
        /// Looks up in cache before doing real extraction.
        /// </summary>
        /// <param name="filePath">Path to image to process.</param>
        /// <param name="save">Flag if the image should be saved to cache after extraction.</param>
        /// <returns>The retrieved artifact type.</returns>
        public ProcessedImage GetProcessedImage(string filePath, bool save = true)
        {
            if (!ProcessedImages.ContainsKey(filePath))
            {
                ProcessImage(filePath, save);
            }

            return ProcessedImages[filePath];
        }

        /// <summary>
        /// Returns all previously processed images as collection.
        /// </summary>
        /// <returns>Collection of processed images.</returns>
        public ICollection<ProcessedImage> GetProcessedImages()
        {
            return ProcessedImages.Values;
        }

        /// <summary>
        /// Process the image stored at the given file path and add it to the cache.
        /// Looks up in cache before doing real extraction.
        /// </summary>
        /// <param name="filePath">Path to image to process.</param>
        /// <param name="save">Flag if the image should be saved to cache after extraction.</param>
        /// <returns>The retrieved artifact type.</returns>
        public void ProcessImage(string filePath, bool save = true)
        {
            // Either read from library, or generate.
            if (!ProcessedImages.ContainsKey(filePath))
            {
                // Generate artifact object freshly.
                var image = VisualFeatureExtractor.ExtractFeatures(filePath);

                if (image != null)
                {
                    // Add to cache list.
                    ProcessedImages.Add(filePath, image);
                    DataChanged = true;

                    if (save)
                    {
                        // Save this to cache file.
                        Save();
                    }
                }
            }
        }

        /// <summary>
        /// Fills the cache with all images in given path.
        /// </summary>
        /// <param name="path">Directory to scan for files.</param>
        public void ProcessImagesInPath(DirectoryInfo path)
        {
            // Go through all .png files in given path and subdirectories.
            foreach (var imageFile in path.GetFiles("*.png", SearchOption.AllDirectories))
            {
                // Process image for the current path. Don't save object to cache, we will do that later.
                ProcessImage(imageFile.FullName, false);
            }

            // Save object to cache file.
            Save();
        }

        /// <summary>
        /// Exports this library to a file.
        /// </summary>
        private void Save()
        {
            // We do not need to export anything, if nothing's changed.
            if (!DataChanged) return;

            // Set flag to false just in case.
            DataChanged = false;

            var binaryFormatter = new BinaryFormatter();
            FileHelper.WriteToFile(Uri.UnescapeDataString(PersistentFilePath.FullName),
                stream => binaryFormatter.Serialize(stream, this), FileMode.Create);
        }
    }
}