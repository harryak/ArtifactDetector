using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ItsApe.ArtifactDetector.Converters;
using ItsApe.ArtifactDetector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
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
        /// Timer to call detection in interval.
        /// </summary>
        private System.Timers.Timer detectionTimer = null;

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
        /// Memory region for sharing data with other processes.
        /// </summary>
        private SafeMemoryMappedFileHandle sharedMemoryHandle;

        /// <summary>
        /// Mutex for sharedMemory, used across processes.
        /// </summary>
        private Mutex sharedMemoryMutex;

        /// <summary>
        /// Instantiate service with setup.
        /// </summary>
        /// <param name="setup">Setup of this application.</param>
        public DetectorService()
        {
            InitializeComponent();

            // Get setup of service for the first time.
            Setup = ApplicationSetup.GetInstance();

            // Get service state from file.
            EnsureServiceState();

            // Get an initial status of active sessions.
            detectionLogWriter = new DetectionLogWriter(
                Setup.WorkingDirectory.FullName, serviceState.ArtifactConfiguration.RuntimeInformation.ArtifactName);
            sessionManager = new SessionManager();

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
            if ((jsonEncodedParameters == null || jsonEncodedParameters == "") && serviceState.ArtifactConfiguration == null)
            {
                Logger.LogError("Invalid or empty argument for StartWatch. Not going to execute watch task.");
                serviceState.IsRunning = false;
                PersistServiceState();
                return false;
            }

            // Only have to do this if we got parameters.
            try
            {
                serviceState.ArtifactConfiguration = JsonConvert.DeserializeObject<ArtifactConfiguration>(jsonEncodedParameters);
            }
            catch (Exception e)
            {
                Logger.LogError("Exception while deserializing JSON parameters: {0}.", e.Message);
                serviceState.IsRunning = false;
                PersistServiceState();
                return false;
            }
            
            SetupSharedMemory();

            // Start detection loop.
            Logger.LogInformation("Starting watch task now with interval of {0}ms.", serviceState.ArtifactConfiguration.DetectionInterval);
            detectionTimer = new System.Timers.Timer
            {
                Interval = serviceState.ArtifactConfiguration.DetectionInterval,
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
            // Get the state as fields are not persisted.
            EnsureServiceState();

            if (!serviceState.IsRunning)
            {
                return "";
            }

            // Stop detection loop, wait for finishing and collect results.
            detectionTimer.Stop();

            //TODO: Wait for writing stream to finish via mutex and release both them to be safe.

            if (sharedMemoryHandle != null)
            {
                sharedMemoryHandle.Dispose();
            }

            var parameters = JsonConvert.DeserializeObject<StopWatchParameters>(jsonEncodedParameters);

            // Set configuration to null to be empty on next run.
            serviceState.ArtifactConfiguration = null;

            // Make ready for next watch task.
            serviceState.IsRunning = false;

            PersistServiceState();

            return detectionLogWriter.CompileResponses(parameters.ErrorWindowSize);
        }

        protected override void OnContinue()
        {
            RestartSavedState();
            Logger.LogInformation("Detector service continued.");
        }

        protected override void OnPause()
        {
            PauseCurrentState();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            switch (powerStatus)
            {
                case PowerBroadcastStatus.QuerySuspend:
                    PauseCurrentState();
                    break;

                case PowerBroadcastStatus.QuerySuspendFailed:
                    RestartSavedState();
                    break;

                case PowerBroadcastStatus.ResumeAutomatic:
                case PowerBroadcastStatus.ResumeCritical:
                case PowerBroadcastStatus.ResumeSuspend:
                    RestartSavedState();
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
            //Debugger.Launch();

            RestartSavedState();
            Logger.LogInformation("Detector service started.");
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
            // Fire detection and forget the task.
            Task.Factory.StartNew(() => TriggerDetection());
            detectionTimer.Elapsed -= DetectionEventHandler;
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
        /// Get security identifier for a memory mapped file which allows authenticated local users full control.
        /// </summary>
        /// <param name="fileSecurity">The security object.</param>
        private void GetSecurityIdentifier(out MemoryMappedFileSecurity fileSecurity)
        {
            fileSecurity = new MemoryMappedFileSecurity();
            fileSecurity.AddAccessRule(
                new AccessRule<MemoryMappedFileRights>(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Translate(typeof(NTAccount)),
                MemoryMappedFileRights.FullControl,
                AccessControlType.Allow));
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
            new DetectorConverter());

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
            if (serviceHost == null)
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

        private void SetupSharedMemory()
        {
            // Prepare shared memory via memory mapped file.
            if (sharedMemoryHandle == null)
            {
                GetSecurityIdentifier(out var fileSecurity);

                var sharedMemory = MemoryMappedFile.CreateOrOpen(
                   @"Global\" + ApplicationConfiguration.MemoryMappedFileName,
                   2048,
                   MemoryMappedFileAccess.ReadWrite,
                   MemoryMappedFileOptions.None,
                   fileSecurity, HandleInheritability.Inheritable);

                sharedMemoryHandle = sharedMemory.SafeMemoryMappedFileHandle;
            }

            if (sharedMemoryMutex == null)
            {
                sharedMemoryMutex = new Mutex(false, ApplicationConfiguration.MemoryMappedMutexName);
            }
        }

        /// <summary>
        /// Method to detect the currently given artifact and write the response to the responses log file.
        /// </summary>
        private void TriggerDetection()
        {
            var queryTime = DateTime.Now;
            if (!sessionManager.HasActiveSessions())
            {
                // No active user sessions, no artifacts can be present.
                detectionLogWriter.LogDetectionResult(
                    queryTime, queryTime, new DetectorResponse { ArtifactPresent = DetectorResponse.ArtifactPresence.Impossible });
                return;
            }

            // Call artifact detector (may be a compound detector) from artifact configuration.
            var artifactRuntimeInformation = (ArtifactRuntimeInformation) serviceState.ArtifactConfiguration.RuntimeInformation.Clone();
            DetectorResponse detectorResponse = null;

            try
            {
                detectorResponse = serviceState.ArtifactConfiguration.Detector.FindArtifact(ref artifactRuntimeInformation);
            }
            catch (Exception e)
            {
                Logger.LogError("Problem calling the detector: {0}.", e.Message);
            }
            var responseTime = DateTime.Now;

            detectionLogWriter.LogDetectionResult(queryTime, responseTime, detectorResponse);
        }

        private DetectionLogWriter detectionLogWriter;
    }
}
