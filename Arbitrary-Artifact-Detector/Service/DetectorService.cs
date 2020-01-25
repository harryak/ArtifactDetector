using ArbitraryArtifactDetector.DebugUtility;
using ArbitraryArtifactDetector.Model;
using ArbitraryArtifactDetector.Parser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        public DetectorService(Setup setup, AADStopwatch stopwach = null)
        {
            InitializeComponent();

            Setup = setup;
            Logger = Setup.GetLogger("DetectorService");
            ArtifactConfigurationParser = new ArtifactConfigurationParser(Setup);
        }

        /// <summary>
        /// Destructor of service.
        /// </summary>
        ~DetectorService()
        {
            detectorResponsesAccess.Dispose();
        }

        /// <summary>
        /// Instance of the artifact configuration parser.
        /// </summary>
        private ArtifactConfigurationParser ArtifactConfigurationParser { get; set; }

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
        public void StartWatch(string artifactType, string artifactConfigurationString, string referenceImagesPath, int intervalLength)
        {
            // Set flag of this to "isRunning" to only start one watch task at a time.
            if (!isRunning)
            {
                isRunning = true;
            }
            else
            {
                Logger.LogError("Can't start watch since it is already running.");
                return;
            }

            if (stopwatch != null)
            {
                stopwatch.Restart();
            }

            // Check parameters for validity.
            if (artifactType == "" || artifactConfigurationString == "" || intervalLength < 1)
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");

                // Stop execution.
                return;
            }

            // Determine appropriate artifact detector(s) and runtime information by getting the artifact's configuration from arguments.
            try
            {
                artifactConfiguration = ArtifactConfigurationParser.ParseConfigurationString(artifactConfigurationString);
            }
            catch (IOException exception)
            {
                Logger.LogError("Can not read the artifact's recipe with error: {0}. Not going to execute watch task.", exception.Message);

                // Stop execution.
                return;
            }

            // If there is a path to reference images given: Try to get it and extract images from it to the artifact configuration.
            if (referenceImagesPath != "")
            {
                try
                {
                    DirectoryInfo referenceImagesPathObj = new DirectoryInfo(referenceImagesPath);
                    artifactConfiguration.RuntimeInformation.ReferenceImages.ProcessImagesInPath(referenceImagesPathObj);
                }
                catch (Exception exception)
                {
                    Logger.LogError("Can not access the supplied reference image path with error: {0}. Using possibly empty reference image cache.", exception.Message);
                }
            }

            responsesPath = Path.Combine(Setup.WorkingDirectory.FullName, artifactConfiguration.RuntimeInformation.ArtifactName, "-raw-", DateTime.Now.ToString(), ".csv");
            compiledResponsesPath = Path.Combine(Setup.WorkingDirectory.FullName, artifactConfiguration.RuntimeInformation.ArtifactName, "-results-", DateTime.Now.ToString(), ".csv");
            detectorResponses = new StreamWriter(responsesPath);

            if (stopwatch != null)
            {
                stopwatch.Stop("watch_start");
                Logger.LogDebug("Finished setup of watch task in {0}ms.", stopwatch.ElapsedMilliseconds);
            }

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", intervalLength);
            detectionTimer.Interval = intervalLength;
            detectionTimer.Start();
        }

        /// <summary>
        ///
        /// </summary>
        public void StopWatch()
        {
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
        }

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
                        writer.WriteLine("{0},present", errorWindowValues.Keys.ElementAt(thresholdSum));
                    }
                    else if (artifactCurrentlyFound && currentSum < thresholdSum)
                    {
                        // Artifact no longer detected.
                        artifactCurrentlyFound = false;
                        writer.WriteLine("{0},absent", errorWindowValues.Keys.ElementAt(thresholdSum));
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
            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();

            // Setup detection timer to call detection in loop.
            detectionTimer = new System.Timers.Timer();
            detectionTimer.Elapsed += DetectionEventHandler;
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