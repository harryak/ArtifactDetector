using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ItsApe.ArtifactDetector.Converters;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ItsApe.ArtifactDetector.Services
{
    /// <summary>
    /// Detector service class that waits for another process to call "StartWatch", then tries to detect
    /// the configured artifact until "StopWatch" is called.
    /// </summary>
    partial class DetectorService : ServiceBase, IDetectorService
    {
        /// <summary>
        /// File name of the configuration file.
        /// </summary>
        private const string ConfigurationFile = "config.json";

        /// <summary>
        /// Timer to call detection in interval.
        /// </summary>
        private static System.Timers.Timer detectionTimer = null;

        /// <summary>
        /// Mutex for detector response list.
        /// </summary>
        private static Mutex detectorResponsesAccess = new Mutex();

        /// <summary>
        /// Variables describing the current state of the service, to be serialized and saved in the configuration file.
        /// </summary>
        private static ServiceState serviceState;

        /// <summary>
        /// List of all responses during the watch task.
        /// </summary>
        private StreamWriter detectorResponses;

        /// <summary>
        /// Host for this service to be callable.
        /// </summary>
        private ServiceHost serviceHost = null;

        /// <summary>
        /// Instantiate service with setup.
        /// </summary>
        /// <param name="setup">Setup of this application.</param>
        public DetectorService()
        {
            InitializeComponent();

            // Get setup of service for the first time.
            try
            {
                Setup = ApplicationSetup.GetInstance();
            }
            catch (ApplicationSetupError)
            {
                return;
            }

            Logger = Setup.GetLogger("DetectorService");
            Logger.LogInformation("Detector service initialized.");
        }

        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private ILogger Logger { get; }

        /// <summary>
        /// Setup for this run, holding arguments and other necessary objects.
        /// </summary>
        private ApplicationSetup Setup { get; }

        /// <summary>
        /// Start watching an artifact in an interval of configured length.
        /// </summary>
        /// <param name="jsonEncodedParameters">Optional paramaters, JSON encoded. Only optional if called in OnStart!</param>
        public bool StartWatch(string jsonEncodedParameters = "")
        {
            // Get the state as fields are not persisted.
            GetServiceState();

            // Set flag of this to "isRunning" early to only start one watch task at a time.
            if (!serviceState.IsRunning)
            {
                serviceState.IsRunning = true;
            }
            else
            {
                Logger.LogError("Can't start watch since it is already running.");
                return false;
            }

            // Check parameters for validity.
            if ((jsonEncodedParameters == null || jsonEncodedParameters == "") && serviceState.ArtifactConfiguration == null)
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");
                serviceState.IsRunning = false;

                // Stop execution.
                return false;
            }
            else
            {
                // Only have to do this if we got parameters.
                try
                {
                    serviceState.ArtifactConfiguration = JsonConvert.DeserializeObject<ArtifactConfiguration>(jsonEncodedParameters);
                }
                catch (Exception e)
                {
                    Logger.LogError("Exception while deserializing JSON parameters: {0}.", e.Message);
                    serviceState.IsRunning = false;

                    // Stop execution.
                    return false;
                }

                SetupFilePath(serviceState.ArtifactConfiguration.RuntimeInformation.ArtifactName);
            }

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", serviceState.ArtifactConfiguration.DetectionInterval);

            detectionTimer = new System.Timers.Timer
            {
                Interval = serviceState.ArtifactConfiguration.DetectionInterval,
            };
            detectionTimer.Elapsed += DetectionEventHandler;

            detectionTimer.Start();
            SaveServiceState();

            return true;
        }

        /// <summary>
        /// Stop watching the artifact currently watched.
        /// </summary>
        public string StopWatch(string jsonEncodedParameters)
        {
            // Get the state as fields are not persisted.
            GetServiceState();

            if (!serviceState.IsRunning)
            {
                return "";
            }

            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            // Wait for writing stream to finish via mutex and release both them to be safe.
            if (detectorResponsesAccess != null
                && !detectorResponsesAccess.SafeWaitHandle.IsClosed
                && detectorResponsesAccess.WaitOne())
            {
                detectorResponsesAccess.Dispose();
            }

            if (detectorResponses != null)
            {
                detectorResponses.Dispose();
            }

            var parameters = JsonConvert.DeserializeObject<StopWatchParameters>(jsonEncodedParameters);

            CompileResponses(parameters.ErrorWindowSize);

            // Set configuration to null to be empty on next run.
            serviceState.ArtifactConfiguration = null;

            // Make ready for next watch task.
            serviceState.IsRunning = false;

            SaveServiceState();

            return serviceState.CompiledResponsesPath;
        }

        /// <summary>
        /// Starts this service by opening a service host.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Uncomment this to debug.
            //Debugger.Launch();

            if (serviceHost != null)
            {
                Logger.LogWarning("Service host was still running.");
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();

            // Restore configuration from file, if present.
            GetServiceState();
            Logger.LogInformation("Detector service started.");

            // Check if the detection must be started.
            if (serviceState.IsRunning)
            {
                Logger.LogInformation("Watch task should be running, start it.");
                serviceState.IsRunning = false;
                Task.Run(() => StartWatch());
            }

            SaveServiceState();
        }

        /// <summary>
        /// Stops service by closing the service host.
        /// </summary>
        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }

            // Save configuration to file.
            SaveServiceState();

            // Dispose detection timer just in case.
            if (detectionTimer != null)
            {
                detectionTimer.Dispose();
            }

            Logger.LogInformation("Detector service stopped. Bye bye!");
        }

        /// <summary>
        /// Compile all responses previously written to file at responsesPath and write to compiledResponsesPath.
        /// </summary>
        /// <param name="errorWindowSize">Make sure this is an odd integer.</param>
        private void CompileResponses(int errorWindowSize)
        {
            // Buffer the values inside the current error window.
            var errorWindowValues = new SortedDictionary<long, int>();
            int currentMajority = -1, previousMajority = -1;
            long changeTimestamp = 0;

            using (var reader = new StreamReader(serviceState.ResponsesPath))
            using (var writer = new StreamWriter(serviceState.CompiledResponsesPath, true))
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

        /// <summary>
        /// Boyer-Moore majority vote algorithm.
        /// </summary>
        /// <param name="dictionary">Get majority of this dictionaries entries.</param>
        private int GetMajorityItem([In] ref SortedDictionary<long, int> dictionary)
        {
            int majorityItem = -1, counter = 0;

            foreach (var entry in dictionary.Values)
            {
                if (counter == 0)
                {
                    majorityItem = entry;
                    counter++;
                } else if (entry == majorityItem)
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
        /// Method to detect the currently given artifact and write the response to the responses dictionary with time.
        /// </summary>
        private void Detect()
        {
            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = (ArtifactRuntimeInformation) serviceState.ArtifactConfiguration.RuntimeInformation.Clone();
            var queryTime = DateTime.Now;
            var response = serviceState.ArtifactConfiguration.Detector.FindArtifact(ref artifactRuntimeInformation);
            var responseTime = DateTime.Now;

            if (detectorResponsesAccess == null || detectorResponsesAccess.SafeWaitHandle.IsClosed)
            {
                detectorResponsesAccess = new Mutex();
            }

            // Save response to timetable.
            if (detectorResponsesAccess.WaitOne())
            {
                // Write response prepended with time to responses file and flush.
                // Use sortable and tenth-millisecond-precise timestamp for entry.
                using (detectorResponses = new StreamWriter(serviceState.ResponsesPath, true))
                {
                    detectorResponses.WriteLine("{0:yyMMddHHmmssffff},{1:yyMMddHHmmssffff},{2}", queryTime, responseTime, (int)response.ArtifactPresent);
                    detectorResponses.Flush();
                }

                // Release mutex and finish.
                detectorResponsesAccess.ReleaseMutex();
            }
        }

        /// <summary>
        /// Method that gets continuously called by timer (in intervals) to detect the artifact.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        private void DetectionEventHandler(object source, ElapsedEventArgs eventArgs)
        {
            // Fire detection and forget the task.
            Task.Factory.StartNew(() => Detect());
        }

        /// <summary>
        /// Get state of the service, either from file or new object.
        /// </summary>
        private void GetServiceState()
        {
            string fileName = Uri.UnescapeDataString(
                Path.Combine(Setup.WorkingDirectory.FullName, ConfigurationFile));
            var configurationFileInfo = new FileInfo(fileName);
            if (configurationFileInfo.Exists)
            {
                Logger.LogInformation("Restoring configuration from file.");
                using (var reader = new StreamReader(configurationFileInfo.FullName))
                {
                    serviceState = JsonConvert.DeserializeObject<ServiceState>(reader.ReadToEnd());
                }
            }
            else
            {
                Logger.LogInformation("Creating new configuration.");
                serviceState = new ServiceState();
            }
        }

        /// <summary>
        /// Save service state to file.
        /// </summary>
        private void SaveServiceState()
        {
            // Save configuration to file.
            string jsonEncodedConfig = JsonConvert.SerializeObject(
                serviceState,
                new ArtifactRuntimeInformationConverter(),
                new DetectorConverter());

            string fileName = Uri.UnescapeDataString(
                Path.Combine(Setup.WorkingDirectory.FullName, ConfigurationFile));
            using (var writer = new StreamWriter(fileName))
            {
                Logger.LogInformation("Saving configuration to file");
                writer.Write(jsonEncodedConfig);
            }
        }

        /// <summary>
        /// Create directory for artifact, ignore if it exists.
        /// </summary>
        /// <param name="artifactName"></param>
        /// <returns>The full path of the (new) directory.</returns>
        private string SetupFilePath(string artifactName)
        {
            var filePath = Directory.CreateDirectory(
                Uri.UnescapeDataString(
                    Path.Combine(Setup.WorkingDirectory.FullName,
                    serviceState.ArtifactConfiguration.RuntimeInformation.ArtifactName)
                    )
                ).FullName;
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            serviceState.ResponsesPath = Path.Combine(
                filePath,
                "raw-" + timestamp + ".csv");
            serviceState.CompiledResponsesPath = Path.Combine(
                filePath,
                "results-" + timestamp + ".csv");

            File.Create(serviceState.ResponsesPath).Close();
            File.Create(serviceState.CompiledResponsesPath).Close();

            return filePath;
        }
    }
}
