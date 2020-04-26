using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Services
{
    internal class SessionManager : HasLogger
    {
        /// <summary>
        /// Singleton instance of class.
        /// </summary>
        private static SessionManager instance = null;

        /// <summary>
        /// Counters for active sessions.
        /// Key is the session ID and value a counter for currently active users in the session.
        /// </summary>
        private Dictionary<int, int> sessionActivityCounter = new Dictionary<int, int>();

        /// <summary>
        /// Get new session manager which automatically gets the current sessions.
        /// </summary>
        private SessionManager()
        {
            Logger.LogDebug("Creating new instance of session manager.");
            DetectActiveSessions();
        }

        /// <summary>
        /// Disposes all session processes.
        /// </summary>
        ~SessionManager()
        {
            foreach (var entry in DetectorProcesses)
            {
                entry.Value.Dispose();
            }
        }

        /// <summary>
        /// Storage for all detector process ids per session.
        /// </summary>
        public Dictionary<int, DetectorProcessEndpoint> DetectorProcesses { get; private set; } = new Dictionary<int, DetectorProcessEndpoint>();

        /// <summary>
        /// Get singleton instance.
        /// </summary>
        /// <returns>The singleton instance.</returns>
        public static SessionManager GetInstance()
        {
            if (instance == null)
            {
                instance = new SessionManager();
            }

            return instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="runtimeInformation"></param>
        /// <returns></returns>
        public bool CallDetectorProcess(int sessionId, ref ArtifactRuntimeInformation runtimeInformation)
        {
            if (!DetectorProcesses.ContainsKey(sessionId))
            {
                Logger.LogError("Session with id {0} not active, can not call detector process.", sessionId);
                return false;
            }

            return DetectorProcesses[sessionId].CallProcess(ref runtimeInformation);
        }

        /// <summary>
        /// Add a session to the pool by its ID. Does nothing if the session ID is already active.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        public void AddActiveSession(int sessionId)
        {
            if (!sessionActivityCounter.ContainsKey(sessionId))
            {
                Logger.LogInformation("Creating session endpoint for new session {0}.", sessionId);

                try
                {
                    var detectorProcessEndpoint = new DetectorProcessEndpoint(sessionId);
                    sessionActivityCounter.Add(sessionId, 1);
                    DetectorProcesses.Add(sessionId, detectorProcessEndpoint);
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Use this if a user has logged off from a session.
        /// </summary>
        /// <param name="sessionId">Global session ID.</param>
        public void DecreaseSessionCounter(int sessionId)
        {
            if (sessionActivityCounter.ContainsKey(sessionId))
            {
                if (sessionActivityCounter[sessionId] <= 1)
                {
                    Logger.LogInformation("Removing session endpoint for closed session {0}.", sessionId);
                    sessionActivityCounter.Remove(sessionId);
                    DetectorProcesses[sessionId].Dispose();
                    DetectorProcesses.Remove(sessionId);
                }
                else
                {
                    sessionActivityCounter[sessionId]--;
                }
            }
        }

        /// <summary>
        /// Tells whether there are active sessions on the PC.
        /// </summary>
        /// <returns>True if there are.</returns>
        public bool HasActiveSessions()
        {
            return sessionActivityCounter.Count > 0;
        }

        /// <summary>
        /// Use this if a user has logged in to a session.
        /// </summary>
        /// <param name="sessionId">Global session ID.</param>
        public void IncreaseSessionCounter(int sessionId)
        {
            AddActiveSession(sessionId);
            sessionActivityCounter[sessionId]++;
        }

        /// <summary>
        /// Initialize the activeSessionId-list with currently active sessions.
        /// </summary>
        private void DetectActiveSessions()
        {
            Logger.LogDebug("Detecting active sessions.");

            var sessionInfoPointer = IntPtr.Zero;
            var sessionCount = 0;

            // Enumerate session infos via Win32 API.
            var returnValue = NativeMethods.WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref sessionInfoPointer, ref sessionCount);
            var sessionInfoSize = Marshal.SizeOf(typeof(NativeMethods.WtsSessionInfo));
            long currentOffset = (long)sessionInfoPointer;

            if (returnValue != 0)
            {
                for (int i = 0; i < sessionCount; i++)
                {
                    var sessionInfo = (NativeMethods.WtsSessionInfo)Marshal.PtrToStructure((IntPtr)currentOffset, typeof(NativeMethods.WtsSessionInfo));
                    currentOffset += sessionInfoSize;

                    // Only session number higher than zero and active state counts as "logged in".
                    if (sessionInfo.SessionId > 0 && sessionInfo.State == NativeMethods.WtsConnectedState.WTSActive)
                    {
                        AddActiveSession(sessionInfo.SessionId);
                        sessionActivityCounter[sessionInfo.SessionId]++;
                    }
                }

                NativeMethods.WTSFreeMemory(sessionInfoPointer);
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
            }
        }
    }
}
