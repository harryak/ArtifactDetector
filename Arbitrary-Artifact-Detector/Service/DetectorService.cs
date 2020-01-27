using ArbitraryArtifactDetector.DebugUtility;
using ArbitraryArtifactDetector.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

namespace ArbitraryArtifactDetector.Service
{
    /// <summary>
    /// Detector service class that waits for another process to call "StartWatch", then tries to detect
    /// the configured artifact until "StopWatch" is called.
    /// </summary>
    partial class DetectorService : ServiceBase, IDetectorService
    {
        /// <summary>
        /// The current configuration of artifact to be looked for.
        /// </summary>
        private ArtifactConfiguration artifactConfiguration = null;

        /// <summary>
        /// Timer to call detection in interval.
        /// </summary>
        private System.Timers.Timer detectionTimer = null;

        private string responsesPath;
        private string compiledResponsesPath;

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
        /// Either null or instance of a stopwatch to evaluate this service.
        /// </summary>
        private AADStopwatch stopwatch = null;

        private List<Task> DetectingTasks = new List<Task>();

        private bool isRunning = false;

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
                Setup = Setup.GetInstance();
            }
            catch (SetupError)
            {
                return;
            }
            
            Logger = Setup.GetLogger("DetectorService");
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
        private Setup Setup { get; }

        /// <summary>
        /// Start watching an artifact in an interval of configured length.
        /// </summary>
        public bool StartWatch(string jsonEncodedParameters)
        {
            // Set flag of this to "isRunning" early to only start one watch task at a time.
            if (!isRunning)
            {
                isRunning = true;
            }
            else
            {
                Logger.LogError("Can't start watch since it is already running.");
                return false;
            }

            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            // Check parameters for validity.
            if (jsonEncodedParameters == null || jsonEncodedParameters == "")
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");
                isRunning = false;

                // Stop execution.
                return false;
            }
            
            try
            {
                artifactConfiguration = JsonConvert.DeserializeObject<ArtifactConfiguration>(jsonEncodedParameters);
            }
            catch (Exception e)
            {
                Logger.LogError("Exception while deserializing JSON parameters: {0}.", e.Message);
                isRunning = false;

                // Stop execution.
                return false;
            }

            responsesPath = Path.Combine(Setup.WorkingDirectory.FullName, artifactConfiguration.RuntimeInformation.ArtifactName + "-raw-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv");
            compiledResponsesPath = Path.Combine(Setup.WorkingDirectory.FullName, artifactConfiguration.RuntimeInformation.ArtifactName + "-results-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv");
            detectorResponses = new StreamWriter(responsesPath);

            if (stopwatch != null)
            {
                stopwatch.Stop("watch_start");
                Logger.LogDebug("Finished setup of watch task in {0}ms.", stopwatch.ElapsedMilliseconds);
            }

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", artifactConfiguration.DetectionInterval);
            
            detectionTimer = new System.Timers.Timer
            {
                Interval = artifactConfiguration.DetectionInterval
            };

            detectionTimer.Start();
            return true;
        }

        /// <summary>
        /// Stop watching the artifact currently watched.
        /// </summary>
        public bool StopWatch()
        {
            if (!isRunning)
            {
                return false;
            }

            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            // Wait for all threads to finish and compile detectorResponses then.
            Task.WaitAll(DetectingTasks.ToArray());

            // Wait for writing stream to finish via mutex and release both them to be safe.
            if (detectorResponsesAccess.WaitOne())
            {
                detectorResponses.Dispose();
                detectorResponsesAccess.Dispose();
            }

            CompileResponses();

            // Set configuration to null to be empty on next run.
            artifactConfiguration = null;

            // Make ready for next watch task.
            isRunning = false;

            return true;
        }

        /// <summary>
        /// Compile all responses previously written to file at responsesPath and write to compiledResponsesPath.
        /// </summary>
        private void CompileResponses()
        {
            int errorWindowSize = 5;
            Dictionary<long, int> errorWindowValues = new Dictionary<long, int>();
            // Integer division always floors value, so add one.
            int thresholdSum = errorWindowSize + 1 / 2;
            int currentSum;
            bool artifactCurrentlyFound = false;

            using (StreamReader reader = new StreamReader(responsesPath))
            using (StreamWriter writer = new StreamWriter(compiledResponsesPath))
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
        /// Starts this service by opening a service host.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Uncomment this to debug.
            Debugger.Launch();

            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();
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

            // Dispose detection timer just in case.
            detectionTimer.Dispose();
        }

        /// <summary>
        /// Method to detect the currently given artifact and write the response to the responses dictionary with time.
        /// </summary>
        private void Detect()
        {
            int certaintyThreshold = 60;

            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = (ArtifactRuntimeInformation) artifactConfiguration.RuntimeInformation.Clone();
            var response = artifactConfiguration.Detector.FindArtifact(ref artifactRuntimeInformation);
            var responseTime = DateTime.Now;

            // Save response to timetable.
            if (detectorResponsesAccess.WaitOne())
            {
                // Write response prepended with time to responses file and flush.
                // Use sortable and millisecond-precise timestamp for entry.
                int artifactPresent = response.ArtifactPresent && response.Certainty >= certaintyThreshold ? 1 : 0 ;
                detectorResponses.WriteLine("{0:yyMMddHHmmssfff},{1}", responseTime, artifactPresent);
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
            // Clean up already completed tasks from list to save memory.
            DetectingTasks.RemoveAll(x => x.IsCompleted);

            // Fire detection and add task to list.
            var newTask = Task.Factory.StartNew(() => Detect());
            DetectingTasks.Add(newTask);
        }
    }
}