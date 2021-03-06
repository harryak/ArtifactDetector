using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using ItsApe.ArtifactDetector.Converters;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Detectors;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ItsApe.ArtifactDetector.Services
{
    /// <summary>
    /// Detector service class that waits for another process to call "StartWatch", then tries to detect
    /// the configured artifact until "StopWatch" is called.
    /// </summary>
    partial class DetectorService : ServiceBase, IDetectorService, IDisposable
    {
        /// <summary>
        /// File name of the configuration file.
        /// </summary>
        private const string ConfigurationFile = "config.json";

        /// <summary>
        /// A log writer for detection results.
        /// </summary>
        private DetectionLogWriter detectionLogWriter;

        /// <summary>
        /// Timer to call detection in interval.
        /// </summary>
        private Timer detectionTimer = null;

        /// <summary>
        /// Timer to do health checks for process.
        /// </summary>
        private Timer healthCheckTimer = null;

        /// <summary>
        /// Host for this service to be callable.
        /// </summary>
        private ServiceHost serviceHost = null;

        /// <summary>
        /// Variables describing the current state of the service, to be serialized and saved in the configuration file.
        /// </summary>
        private ServiceState serviceState;

        /// <summary>
        /// Manager instance for user sessions.
        /// </summary>
        private SessionManager sessionManager;

        /// <summary>
        /// Instantiate service.
        /// </summary>
        public DetectorService()
        {
            InitializeComponent();

            // Get setup of service for the first time.
            Setup = ApplicationSetup.GetInstance();
            Logger = Setup.GetLogger("ArtifactDetectorService");
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
            // Get the state as fields might not be persisted.
            EnsureServiceState();

            // Test the current service state to be sure.
            if (serviceState.IsRunning)
            {
                Logger.LogError("Can't start watch since it is already running.");
                return false;
            }

            // Set flag of this to "isRunning" early to only start one watch task at a time.
            serviceState.IsRunning = true;
            PersistServiceState();

            // Check parameters for validity.
            if ((jsonEncodedParameters == null || jsonEncodedParameters == "") && serviceState.DetectorConfiguration == null)
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");
                serviceState.IsRunning = false;
                PersistServiceState();
                return false;
            }

            // Only have to do this if we got parameters.
            try
            {
                serviceState.DetectorConfiguration = JsonConvert.DeserializeObject<DetectorConfiguration>(jsonEncodedParameters);
            }
            catch (Exception e)
            {
                Logger.LogError("Exception while deserializing JSON parameters: {0}.", e.Message);
                serviceState.IsRunning = false;
                PersistServiceState();
                return false;
            }

            Logger.LogDebug("Creating new detection log writer.");
            detectionLogWriter = new DetectionLogWriter(
                Setup.WorkingDirectory.FullName, serviceState.DetectorConfiguration.RuntimeInformation.ArtifactName);

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", serviceState.DetectorConfiguration.DetectionInterval);
            detectionTimer = new Timer
            {
                Interval = serviceState.DetectorConfiguration.DetectionInterval,
            };
            detectionTimer.Elapsed += DetectionEventHandler;
            detectionTimer.Start();
            PersistServiceState();

            return true;
        }

        /// <summary>
        /// Stop watching the artifact currently watched.
        /// </summary>
        public string StopWatch(string jsonEncodedParameters)
        {
            StopWatchParameters parameters;

            try
            {
                parameters = JsonConvert.DeserializeObject<StopWatchParameters>(jsonEncodedParameters);
            }
            catch (Exception)
            {
                return "";
            }

            // Get the state as fields are not persisted.
            EnsureServiceState();

            if (!serviceState.IsRunning)
            {
                return "";
            }

            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            // Set configuration to null to be empty on next run.
            serviceState.DetectorConfiguration = null;

            // Make ready for next watch task.
            serviceState.IsRunning = false;

            PersistServiceState();

            return detectionLogWriter.CompileResponses(parameters.ErrorWindowSize);
        }

        /// <summary>
        /// Continueing when the service was paused by the OS.
        /// </summary>
        protected override void OnContinue()
        {
            RestartSavedState();
            Logger.LogInformation("Detector service continued.");
        }

        /// <summary>
        /// Pause signal from the OS.
        /// </summary>
        protected override void OnPause()
        {
            PauseCurrentState();
            Logger.LogInformation("Detector service paused.");
        }

        /// <summary>
        /// Power event from the OS.
        /// </summary>
        /// <param name="powerStatus">Information about the new status.</param>
        /// <returns>base return value.</returns>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            switch (powerStatus)
            {
                case PowerBroadcastStatus.QuerySuspend:
                    PauseCurrentState();
                    Logger.LogInformation("Received suspend signal, paused service state.");
                    break;

                case PowerBroadcastStatus.QuerySuspendFailed:
                    RestartSavedState();
                    Logger.LogInformation("Received suspend signal failed, restarted saved service state.");
                    break;

                case PowerBroadcastStatus.ResumeAutomatic:
                case PowerBroadcastStatus.ResumeCritical:
                case PowerBroadcastStatus.ResumeSuspend:
                    RestartSavedState();
                    Logger.LogInformation("Received resume signal, restarted saved service state.");
                    break;
            }

            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// Method is called by system whenever a session is changed.
        /// </summary>
        /// <param name="changeDescription">Struct with further info about the change.</param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            // Either count up for a session or count down.
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.ConsoleConnect:
                case SessionChangeReason.RemoteConnect:
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                    sessionManager.IncreaseSessionCounter(changeDescription.SessionId);
                    break;

                case SessionChangeReason.ConsoleDisconnect:
                case SessionChangeReason.RemoteDisconnect:
                case SessionChangeReason.SessionLock:
                case SessionChangeReason.SessionLogoff:
                    sessionManager.DecreaseSessionCounter(changeDescription.SessionId);
                    break;
            }
            base.OnSessionChange(changeDescription);
        }

        /// <summary>
        /// Handle shutdown of service by system.
        /// </summary>
        protected override void OnShutdown()
        {
            PauseCurrentState();
        }

        /// <summary>
        /// Starts this service by opening a service host.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            // Uncomment this to debug.
            // Debugger.Launch();

            // Get service state from file.
            Logger.LogDebug("Retrieving saved service state.");
            EnsureServiceState();

            // Get an initial status of active sessions.
            Logger.LogDebug("Get session manager.");
            sessionManager = SessionManager.GetInstance();

            RestartSavedState();
            Logger.LogInformation("Detector service started.");

            // Check if process is running every 60 seconds.
            healthCheckTimer = new Timer(30 * 1000);
            healthCheckTimer.Elapsed += HealthCheckProcesses;
            healthCheckTimer.Start();
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
            PersistServiceState();

            // Dispose detection timer just in case.
            if (detectionTimer != null)
            {
                detectionTimer.Dispose();
            }

            Logger.LogInformation("Detector service stopped. Bye bye!");
        }

        /// <summary>
        /// Method that gets continuously called by timer (in intervals) to detect the artifact.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        private void DetectionEventHandler(object source, ElapsedEventArgs eventArgs)
        {
            // Fire detection with a new runtime information copy to work with and forget the task.
            Task.Factory.StartNew(() => TriggerDetection(
                serviceState.DetectorConfiguration.Detector,
                (ArtifactRuntimeInformation)serviceState.DetectorConfiguration.RuntimeInformation.Clone(),
                serviceState.DetectorConfiguration.MatchConditions,
                ref detectionLogWriter));

            // Uncomment for debugging purposes, if needed.
            // detectionTimer.Elapsed -= DetectionEventHandler;
        }

        /// <summary>
        /// Get state of the service, either from file or new object.
        /// </summary>
        private void EnsureServiceState()
        {
            // Do not do unnecessary work.
            if (serviceState != null)
            {
                return;
            }

            // Get file and read from it, if exists.
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
        /// Ensure all processes are running, called by Timer.Elapsed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HealthCheckProcesses(object sender, ElapsedEventArgs e)
        {
            sessionManager.HealthCheckProcesses();
        }

        /// <summary>
        /// "Pause" the service state: Either temporarily or for a shutdown etc.
        /// </summary>
        private void PauseCurrentState()
        {
            // Close service host to not receive any new tasks.
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }

            // Dispose detection timer.
            if (detectionTimer != null)
            {
                detectionTimer.Dispose();
                detectionTimer = null;
            }

            serviceState.IsRunning = false;

            // Save configuration to file.
            PersistServiceState();
            Logger.LogInformation("Detector service stopped. Bye bye!");
        }

        /// <summary>
        /// Save service state to file.
        /// </summary>
        private void PersistServiceState()
        {
            // Save configuration to file.
            string jsonEncodedConfig = JsonConvert.SerializeObject(
                serviceState,
                new ArtifactRuntimeInformationConverter(),
                new DetectorConverter(),
                new DetectorConditionConverter<ArtifactRuntimeInformation>());

            string fileName = Uri.UnescapeDataString(
            Path.Combine(Setup.WorkingDirectory.FullName, ConfigurationFile));
            using (var writer = new StreamWriter(fileName))
            {
                Logger.LogInformation("Saving configuration to file");
                writer.Write(jsonEncodedConfig);
            }
        }

        private void RestartSavedState()
        {
            // First: Make sure we have the service state.
            EnsureServiceState();

            // The service host should be null, otherwise there is something wrong. Better close it and start new one.
            if (serviceHost != null)
            {
                Logger.LogWarning("Service host was still running.");
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(typeof(DetectorService));
            serviceHost.Open();

            // Check in which state the service was paused.
            if (serviceState.IsRunning)
            {
                Logger.LogInformation("Watch task should be running, start it.");
                serviceState.IsRunning = false;
                StartWatch();
            }
        }

        /// <summary>
        /// Method to detect the currently given artifact and write the response to the responses log file.
        ///
        /// WARNING: This gets executed in a new instance!
        /// WARNING: Almost no try-catch is done in here to save resources!
        /// </summary>
        private void TriggerDetection(IDetector detector, ArtifactRuntimeInformation runtimeInformation, IDetectorCondition<ArtifactRuntimeInformation> matchConditions, ref DetectionLogWriter detectionLogWriter)
        {
            sessionManager = SessionManager.GetInstance();

            var queryTime = DateTime.Now;
            if (!sessionManager.HasActiveSessions())
            {
                // No active user sessions, no artifacts can be present.
                Logger.LogInformation("No session active, no detection necessary.");
                detectionLogWriter.LogDetectionResult(
                    queryTime, new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible });
                return;
            }

            // Loop through all active sessions and detect separately.
            DetectorResponse detectorResponse = null;
            foreach (var sessionEntry in sessionManager.DetectorProcesses)
            {
                try
                {
                    detectorResponse = detector.FindArtifact(ref runtimeInformation, matchConditions, sessionEntry.Key);
                    detectionLogWriter.LogDetectionResult(queryTime, detectorResponse);
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not execute FindArtifact: \"{0}\" \"{1}\"", e.Message, e.InnerException.Message);
                    detectionLogWriter.LogDetectionResult(
                        queryTime, new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Possible });
                }
            }
        }
    }
}
