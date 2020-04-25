using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Services
{
    internal class SessionManager : HasLogger
    {
        /// <summary>
        /// Counters for active sessions.
        /// Key is the session ID and value a counter for currently active users in the session.
        /// </summary>
        private Dictionary<int, int> activeSessionIds = new Dictionary<int, int>();

        /// <summary>
        /// Storage for all detector process ids per session.
        /// </summary>
        private Dictionary<int, int> detectorProcessIds = new Dictionary<int, int>();

        /// <summary>
        /// Get new session manager which automatically gets the current sessions.
        /// </summary>
        public SessionManager()
        {
            DetectActiveSessions();
        }

        ~SessionManager()
        {
            // Somehow tell the processes to shut down.
        }

        /// <summary>
        /// Use this if a user has logged off from a session.
        /// </summary>
        /// <param name="sessionId">Global session ID.</param>
        public void DecreaseSessionCounter(int sessionId)
        {
            if (activeSessionIds.ContainsKey(sessionId))
            {
                if (activeSessionIds[sessionId] <= 1)
                {
                    activeSessionIds.Remove(sessionId);
                    detectorProcessIds.Remove(sessionId);
                }
                else
                {
                    activeSessionIds[sessionId]--;
                }
            }
        }

        /// <summary>
        /// Tells whether there are active sessions on the PC.
        /// </summary>
        /// <returns>True if there are.</returns>
        public bool HasActiveSessions()
        {
            return activeSessionIds.Count > 0;
        }

        /// <summary>
        /// Use this if a user has logged in to a session.
        /// </summary>
        /// <param name="sessionId">Global session ID.</param>
        public void IncreaseSessionCounter(int sessionId)
        {
            if (!activeSessionIds.ContainsKey(sessionId))
            {
                activeSessionIds.Add(sessionId, 1);
                StartSessionProcess(sessionId);
            }
            else
            {
                activeSessionIds[sessionId]++;
            }
        }

        /// <summary>
        /// Initialize the activeSessionId-list with currently active sessions.
        /// </summary>
        private void DetectActiveSessions()
        {
            var sessionInfoPointer = IntPtr.Zero;
            var sessionCount = 0;

            // Enumerate session infos via Win32 API.
            var returnValue = NativeMethods.WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref sessionInfoPointer, ref sessionCount);
            var sessionInfoSize = Marshal.SizeOf(typeof(NativeMethods.WtsSessionInfo));
            long currentOffset = (int)sessionInfoPointer;

            if (returnValue != 0)
            {
                for (int i = 0; i < sessionCount; i++)
                {
                    var sessionInfo = (NativeMethods.WtsSessionInfo)Marshal.PtrToStructure((IntPtr)currentOffset, typeof(NativeMethods.WtsSessionInfo));
                    currentOffset += sessionInfoSize;

                    // Only active state counts as "logged in".
                    if (sessionInfo.State == NativeMethods.WtsConnectedState.WTSActive)
                    {
                        if (!activeSessionIds.ContainsKey(sessionInfo.SessionId))
                        {
                            activeSessionIds.Add(sessionInfo.SessionId, 1);
                            StartSessionProcess(sessionInfo.SessionId);
                        }
                        activeSessionIds[sessionInfo.SessionId]++;
                    }
                }

                NativeMethods.WTSFreeMemory(sessionInfoPointer);
            }
        }

        /// <summary>
        /// Get security attributes for creation of session processes.
        /// </summary>
        /// <param name="securityAttributes">The struct to be filled.</param>
        private void FillSecurityAttributes(out NativeMethods.SecurityAttributes securityAttributes)
        {
            securityAttributes = new NativeMethods.SecurityAttributes
            {
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };
            securityAttributes.Length = Marshal.SizeOf(securityAttributes);
        }

        /// <summary>
        /// Get startup information for creation of session processes.
        /// </summary>
        /// <param name="startupInformation">The struct to be filled.</param>
        private void FillStartupInformation(out NativeMethods.Startupinfo startupInformation)
        {
            startupInformation = new NativeMethods.Startupinfo
            {
                lpDesktop = @"WinSta0\Default"
            };
            startupInformation.cb = Marshal.SizeOf(startupInformation);
        }

        /// <summary>
        /// Get the full executable path (in quotes for space characters) of the external process to start.
        /// </summary>
        /// <returns>The full path.</returns>
        private string GetExternalProcessName()
        {
            var ProcessDirectory = ApplicationSetup.GetInstance().GetExecutingDirectory().FullName;
            return "\"" + Uri.UnescapeDataString(Path.Combine(ProcessDirectory, ApplicationConfiguration.UserSessionApplicationName)) + "\"";
        }

        /// <summary>
        /// Get a duplicate of a user token for creation of session processes.
        /// </summary>
        /// <param name="userToken">The token to duplicate.</param>
        /// <param name="securityAttributes">Suitable security attributes.</param>
        /// <returns>The token or IntPtr.Zero on failure.</returns>
        private IntPtr GetTokenDuplicate(IntPtr userToken, ref NativeMethods.SecurityAttributes securityAttributes)
        {
            var userTokenDuplicate = IntPtr.Zero;
            if (!NativeMethods.DuplicateTokenEx(userToken, NativeMethods.TokenGeneralAccess,
                    ref securityAttributes, NativeMethods.SecurityImpersionationLevel.SecurityIdentification,
                    NativeMethods.TokenType.TokenPrimary, ref userTokenDuplicate
                ))
            {
                return IntPtr.Zero;
            }

            return userTokenDuplicate;
        }

        /// <summary>
        /// Start the detection process in the given session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private bool StartSessionProcess(int sessionId)
        {
            // Get security token for that session.
            var userToken = IntPtr.Zero;
            if (!NativeMethods.WTSQueryUserToken((uint)sessionId, out userToken))
            {
                Logger.LogError("Could not get user token for session {0}.", sessionId);
                return false;
            }

            var processInformation = new NativeMethods.ProcessInformation();
            FillStartupInformation(out var startupInformation);
            FillSecurityAttributes(out var securityAttributes);

            var userTokenDuplicate = GetTokenDuplicate(userToken, ref securityAttributes);
            if (userTokenDuplicate == IntPtr.Zero)
            {
                Logger.LogError("Could not duplicate tokens for session {0}.", sessionId);
                return false;
            }

            if (!NativeMethods.CreateProcessAsUserW(
                userTokenDuplicate, null, GetExternalProcessName(), ref securityAttributes,
                ref securityAttributes, false, 0x00000010, IntPtr.Zero, null,
                ref startupInformation, ref processInformation))
            {
                Logger.LogError("Could not CreateProcessAsUser in session {0}.", sessionId);
                return false;
            }

            NativeMethods.CloseHandle(userToken);
            NativeMethods.CloseHandle(userTokenDuplicate);

            detectorProcessIds.Add(sessionId, processInformation.dwProcessID);
            Logger.LogDebug("Created process with ID {0} in session {1}.", processInformation.dwProcessID, sessionId);

            return true;
        }
    }
}
