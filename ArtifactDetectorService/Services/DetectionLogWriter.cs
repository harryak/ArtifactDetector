using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Services
{
    internal class DetectionLogWriter : HasLogger
    {
        /// <summary>
        /// File name of the compiled timetable (in the log files directory).
        /// </summary>
        private const string CompiledTimetableFile = "compilation.csv";

        /// <summary>
        /// Name of the current artifact to use for file names.
        /// </summary>
        private readonly string artifactName;

        private readonly object fileLock = new object();

        /// <summary>
        /// File path of the log to write to.
        /// </summary>
        private readonly string logFilesDirectory;

        /// <summary>
        /// The log file to write to (currently).
        /// </summary>
        private string currentLogFile;

        /// <summary>
        /// List of all responses during the watch task.
        /// </summary>
        private StreamWriter logFileWriter;

        /// <summary>
        /// Instantiate this writer by supplying the working directory and artifact name.
        /// Will create a subfolder with the artifact name in the working directory.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="_artifactName"></param>
        public DetectionLogWriter(string workingDirectory, string _artifactName)
        {
            artifactName = _artifactName;
            Logger.LogDebug("Setting up logging in path {0}.", workingDirectory);
            logFilesDirectory = SetupFilePath(workingDirectory);

            Logger.LogDebug("Creating mutex.");
        }

        /// <summary>
        /// Free the used resources.
        /// </summary>
        ~DetectionLogWriter()
        {
            if (logFileWriter != null)
            {
                logFileWriter.Close();
            }
        }

        /// <summary>
        /// Compile all responses previously written to logs in logFilesDirectory and return the path..
        /// </summary>
        /// <param name="errorWindowSize">Make sure this is an odd integer.</param>
        /// <returns>The path of the compiled responses.</returns>
        public string CompileResponses(int errorWindowSize)
        {
            var timetableFile = File.Create(Uri.UnescapeDataString(Path.Combine(logFilesDirectory, CompiledTimetableFile)));
            var timetableFileName = timetableFile.Name;
            timetableFile.Close();

            // Buffer the values inside the current error window.
            var errorWindowValues = new SortedDictionary<long, int>();
            int currentMajority = -1, previousMajority = -1;
            long changeTimestamp = 0;
            string[] currentValues;

            lock (fileLock)
            {
                using (var writer = new StreamWriter(timetableFileName, true))
                {
                    foreach (var logFile in Directory.EnumerateFiles(logFilesDirectory, "*.csv"))
                    {
                        if (!logFile.StartsWith("raw"))
                        {
                            continue;
                        }

                        using (var reader = new StreamReader(logFile))
                        {
                            while (!reader.EndOfStream)
                            {
                                // First: Keep window at right size. We add one value now, so greater equal is the right choice here.
                                if (errorWindowValues.Count >= errorWindowSize)
                                {
                                    errorWindowValues.Remove(errorWindowValues.Keys.First());
                                }

                                // Then: Add next value to window.
                                currentValues = reader.ReadLine().Split(',');
                                errorWindowValues.Add(Convert.ToInt64(currentValues[1]), Convert.ToInt32(currentValues[2]));

                                // See if the average of the window changes.
                                currentMajority = GetMajorityItem(ref errorWindowValues);

                                if (currentMajority != previousMajority)
                                {
                                    // Artifact detection changed.
                                    changeTimestamp = errorWindowValues.SkipWhile(entry => entry.Value != currentMajority).First().Key;
                                    writer.WriteLine("{0},{1}", changeTimestamp, currentMajority);
                                    previousMajority = currentMajority;
                                }
                            }
                        }

                        // Start with fresh values for each log file.
                        errorWindowValues.Clear();
                    }
                }
            }

            return timetableFileName;
        }

        /// <summary>
        /// Write out detection result to current log file.
        /// </summary>
        /// <param name="queryTime">Time when the detection was queried.</param>
        /// <param name="responseTime">Time when the detection was finished.</param>
        /// <param name="detectorResponse">The response to log.</param>
        public void LogDetectionResult(DateTime queryTime, DateTime responseTime, DetectorResponse detectorResponse)
        {
            // Save response to timetable.
            lock (fileLock)
            {
                // Write response prepended with time to responses file and flush.
                // Use sortable and tenth-millisecond-precise timestamp for entry.
                using (logFileWriter = new StreamWriter(currentLogFile, true))
                {
                    logFileWriter.WriteLine("{0:yyMMddHHmmssffff},{1:yyMMddHHmmssffff},{2}",
                        queryTime, responseTime, (int)detectorResponse.ArtifactPresent);
                    logFileWriter.Flush();
                }
            }
        }

        /// <summary>
        /// Boyer-Moore majority vote algorithm.
        /// </summary>
        /// <param name="dictionary">Get majority of this dictionaries entries.</param>
        private int GetMajorityItem([In] ref SortedDictionary<long, int> dictionary)
        {
            int majorityItem = -1, counter = 0;

            for (var i = 0; i < dictionary.Count; i++)
            {
                if (counter == 0)
                {
                    majorityItem = dictionary[i];
                    counter++;
                }
                else if (dictionary[i] == majorityItem)
                {
                    counter++;
                }
                else
                {
                    counter--;
                }
            }

            return majorityItem;
        }

        /// <summary>
        /// Create directory for artifact, ignore if it exists.
        /// </summary>
        /// <param name="artifactName"></param>
        /// <returns>The full path of the (new) directory.</returns>
        private string SetupFilePath(string workingDirectory)
        {
            // Create directory, will silently fail if exists.
            var filePath = Directory.CreateDirectory(
                Uri.UnescapeDataString(Path.Combine(workingDirectory, artifactName))).FullName;

            // Create new log file to write to.
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            currentLogFile = Uri.UnescapeDataString(Path.Combine(filePath, "raw-" + timestamp + ".csv"));
            File.Create(currentLogFile).Close();

            return filePath;
        }
    }
}
