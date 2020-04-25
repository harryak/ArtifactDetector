using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ItsApe.ArtifactDetector.Models;

namespace ItsApe.ArtifactDetector.Services
{
    internal class DetectionLogWriter
    {
        private const string CompiledTimetableFile = "compilation.csv";
        private readonly string artifactName;

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
        /// Mutex for detector response list.
        /// </summary>
        private Mutex logFileWriterMutex;

        public DetectionLogWriter(string workingDirectory, string _artifactName)
        {
            artifactName = _artifactName;
            logFilesDirectory = SetupFilePath(workingDirectory);

            logFileWriterMutex = new Mutex(false, "DetectionLogWriter-" + artifactName);
        }

        ~DetectionLogWriter()
        {
            if (logFileWriterMutex.WaitOne())
                logFileWriter.Close();

            logFileWriterMutex.Close();
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
            if (logFileWriterMutex.WaitOne())
            {
                // Write response prepended with time to responses file and flush.
                // Use sortable and tenth-millisecond-precise timestamp for entry.
                using (logFileWriter = new StreamWriter(currentLogFile, true))
                {
                    logFileWriter.WriteLine("{0:yyMMddHHmmssffff},{1:yyMMddHHmmssffff},{2}", queryTime, responseTime, (int)detectorResponse.ArtifactPresent);
                    logFileWriter.Flush();
                }

                // Release mutex and finish.
                logFileWriterMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Compile all responses previously written to logs in logFilesDirectory and return the path..
        /// </summary>
        /// <param name="errorWindowSize">Make sure this is an odd integer.</param>
        /// <returns>The path of the compiled responses.</returns>
        public string CompileResponses(int errorWindowSize)
        {
            var timetableFile = File.Create(Path.Combine(logFilesDirectory, CompiledTimetableFile));
            var timetableFileName = timetableFile.Name;
            timetableFile.Close();

            // Buffer the values inside the current error window.
            var errorWindowValues = new SortedDictionary<long, int>();
            int currentMajority = -1, previousMajority = -1;
            long changeTimestamp = 0;

            foreach (var logFile in Directory.EnumerateFiles(logFilesDirectory, "*.csv"))
            {
                using (var reader = new StreamReader(logFile))
                using (var writer = new StreamWriter(timetableFileName, true))
                {
                    string[] currentValues;

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
            }

            return timetableFileName;
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
            currentLogFile = Path.Combine(filePath, "raw-" + timestamp + ".csv");
            File.Create(currentLogFile).Close();

            return filePath;
        }
    }
}
