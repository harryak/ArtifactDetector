using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        private System.Timers.Timer detectionTimer = null;

        /// <summary>
        /// List of all responses during the watch task.
        /// </summary>
        private StreamWriter detectorResponses;

        /// <summary>
        /// Mutex for detector response list.
        /// </summary>
        private Mutex detectorResponsesAccess = new Mutex();

        /// <summary>
        /// Host for this service to be callable.
        /// </summary>
        private ServiceHost serviceHost = null;

        /// <summary>
        /// Variables describing the current state of the service, to be serialized and saved in the configuration file.
        /// </summary>
        private ServiceState serviceState;

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
        /// Destructor of service.
        /// </summary>
        ~DetectorService()
        {
            detectorResponsesAccess.Dispose();
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
            Debugger.Launch();

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

                serviceState.ResponsesPath = Path.Combine(Setup.WorkingDirectory.FullName, serviceState.ArtifactConfiguration.RuntimeInformation.ArtifactName + "-raw-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv");
                serviceState.CompiledResponsesPath = Path.Combine(Setup.WorkingDirectory.FullName, serviceState.ArtifactConfiguration.RuntimeInformation.ArtifactName + "-results-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv");
            }

            detectorResponses = new StreamWriter(serviceState.ResponsesPath);

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", serviceState.ArtifactConfiguration.DetectionInterval);

            detectionTimer = new System.Timers.Timer
            {
                Interval = serviceState.ArtifactConfiguration.DetectionInterval
            };

            detectionTimer.Start();
            return true;
        }

        /// <summary>
        /// Stop watching the artifact currently watched.
        /// </summary>
        public string StopWatch(string jsonEncodedParameters)
        {
            if (!serviceState.IsRunning)
            {
                return "";
            }

            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            // Wait for writing stream to finish via mutex and release both them to be safe.
            if (detectorResponsesAccess.WaitOne())
            {
                detectorResponses.Dispose();
                detectorResponsesAccess.Dispose();
            }

            var parameters = JsonConvert.DeserializeObject<StopWatchParameters>(jsonEncodedParameters);

            CompileResponses(parameters.ErrorWindowSize);

            // Set configuration to null to be empty on next run.
            serviceState.ArtifactConfiguration = null;

            // Make ready for next watch task.
            serviceState.IsRunning = false;

            return serviceState.CompiledResponsesPath;
        }

        /// <summary>
        /// Starts this service by opening a service host.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Uncomment this to debug.
            Debugger.Launch();

            if (serviceHost != null)
            {
                Logger.LogWarning("Service host was still running.");
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();

            // Restore configuration from file, if present.
            var configurationFileInfo = new FileInfo(ConfigurationFile);
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

            Logger.LogInformation("Detector service started.");

            // Check if the detection must be started.
            if (serviceState.IsRunning)
            {
                Logger.LogInformation("Watch task should be running, start it.");
                serviceState.IsRunning = false;
                StartWatch();
            }
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
            string jsonEncodedConfig = JsonConvert.SerializeObject(serviceState);
            using (var writer = new StreamWriter(ConfigurationFile))
            {
                Logger.LogInformation("Saving configuration to file");
                writer.Write(jsonEncodedConfig);
            }

            // Dispose detection timer just in case.
            detectionTimer.Dispose();

            Logger.LogInformation("Detector service stopped. Bye bye!");
        }

        /// <summary>
        /// Compile all responses previously written to file at responsesPath and write to compiledResponsesPath.
        /// </summary>
        private void CompileResponses(int errorWindowSize)
        {
            // Buffer the values inside the current error window.
            var errorWindowValues = new Dictionary<long, int>();

            // Integer division always floors value, so add one.
            int thresholdSum = errorWindowSize + 1 / 2;
            int currentSum;
            bool artifactCurrentlyFound = false;

            using (var reader = new StreamReader(serviceState.ResponsesPath))
            using (var writer = new StreamWriter(serviceState.CompiledResponsesPath))
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
                    errorWindowValues.Add(Convert.ToInt64(currentValues[0]), Convert.ToInt32(currentValues[1]));

                    // See if the average of the window changes.
                    currentSum = errorWindowValues.Values.Sum();
                    if (!artifactCurrentlyFound && currentSum >= thresholdSum)
                    {
                        // Artifact detected now.
                        artifactCurrentlyFound = true;
                        writer.WriteLine("{0},1", errorWindowValues.Keys.ElementAt(thresholdSum));
                    }
                    else if (artifactCurrentlyFound && currentSum < thresholdSum)
                    {
                        // Artifact no longer detected.
                        artifactCurrentlyFound = false;
                        writer.WriteLine("{0},0", errorWindowValues.Keys.ElementAt(thresholdSum));
                    }
                }
            }
        }

        /// <summary>
        /// Method to detect the currently given artifact and write the response to the responses dictionary with time.
        /// </summary>
        private void Detect()
        {
            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = (ArtifactRuntimeInformation) serviceState.ArtifactConfiguration.RuntimeInformation.Clone();
            var response = serviceState.ArtifactConfiguration.Detector.FindArtifact(ref artifactRuntimeInformation);
            var responseTime = DateTime.Now;

            // Save response to timetable.
            if (detectorResponsesAccess.WaitOne())
            {
                // Write response prepended with time to responses file and flush.
                // Use sortable and millisecond-precise timestamp for entry.
                var artifactPresent = response.ArtifactPresent;
                detectorResponses.WriteLine("{0:yyMMddHHmmss},{1}", responseTime, artifactPresent);
                detectorResponses.Flush();

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
    }
}
