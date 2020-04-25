using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.DetectorConditions;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Detectors
{
    /// <summary>
    /// A base class to provide common functions for all detectors.
    /// </summary>
    internal abstract class BaseDetector : Debuggable, IDetector
    {
        /// <summary>
        /// Conditions that have to be fulfilled before this detector should be run.
        /// </summary>
        protected IDetectorCondition<ArtifactRuntimeInformation> PreConditions { get; set; }

        /// <summary>
        /// Conditions that have to be fulfilled to yield "match" after calling FindArtifact.
        /// </summary>
        protected IDetectorCondition<DetectorResponse> TargetConditions { get; set; }

        /// <summary>
        /// Find the artifact defined in the artifactConfiguration given some runtime information and a previous detector's response.
        /// </summary>
        /// <param name="runtimeInformation">Information about the artifact.</param>
        ///
        /// <returns>A response object containing information whether the artifact has been found.</returns>
        public abstract DetectorResponse FindArtifact(ref ArtifactRuntimeInformation runtimeInformation);

        /// <summary>
        /// Tells whether this detector has preconditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasPreConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return PreConditions != null
                && (PreConditions.GetType() != typeof(DetectorConditionSet<ArtifactRuntimeInformation>) || ((DetectorConditionSet<ArtifactRuntimeInformation>)PreConditions).NotEmpty());
        }

        /// <summary>
        /// Tells whether this detector has target conditions.
        /// </summary>
        /// <returns>True if it does.</returns>
        public bool HasTargetConditions()
        {
            // Either the variable is "just not null" or if it is a set it is not empty.
            return TargetConditions != null
                && (TargetConditions.GetType() != typeof(DetectorConditionSet<DetectorResponse>) || ((DetectorConditionSet<DetectorResponse>)TargetConditions).NotEmpty());
        }

        /// <summary>
        /// Test prerequisites: Is the session active?
        /// </summary>
        /// <returns>True, if the session is not active.</returns>
        public bool IsScreenActive(ref ArtifactRuntimeInformation runtimeInformation)
        {
            return true;
            if (runtimeInformation.ActiveScreenChecked)
            {
                return true;
            }

            runtimeInformation.ActiveScreenChecked = true;

            return IsScreenUnlocked();
        }

        /// <summary>
        /// Checks whether the current setup and previous response match the conditions for execution of this detector.
        /// </summary>
        /// <param name="runtimeInformation">Information from other detectors run before.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool PreConditionsMatch(ref ArtifactRuntimeInformation runtimeInformation)
        {
            return PreConditions.ObjectMatchesConditions(runtimeInformation);
        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetPreConditions(IDetectorCondition<ArtifactRuntimeInformation> conditions)
        {
            PreConditions = conditions;
        }

        /// <summary>
        /// Explicit setter function for the interface.
        /// </summary>
        /// <param name="conditions">Value to set.</param>
        public void SetTargetConditions(IDetectorCondition<DetectorResponse> conditions)
        {
            TargetConditions = conditions;
        }

        /// <summary>
        /// Checks whether the response matches the conditions for evaluating to "artifact found".
        /// </summary>
        /// <param name="previousResponse">Response from this detector.</param>
        /// <returns>True if the conditions are met.</returns>
        public bool TargetConditionsMatch(ref DetectorResponse response)
        {
            return TargetConditions.ObjectMatchesConditions(response);
        }

        /// <summary>
        /// Calculates how much (percentage) of the queriedWindow is visible other windows above.
        /// </summary>
        /// <param name="queriedWindow">Queried window.</param>
        /// <param name="windowsAbove">The windows above (z-index) the queried window.</param>
        /// <returns>The percentage of how much of the window is visible.</returns>
        protected float CalculateWindowVisibility(Rectangle queriedWindow, ICollection<Rectangle> windowsAbove)
        {
            // If there are no windows above: Return immediately.
            if (windowsAbove.Count < 1)
            {
                return 100f;
            }

            // If there is no area of the window, return "no visibility".
            if (queriedWindow.Area < 1)
            {
                return 0f;
            }

            int subtractArea = new RectangleUnionCalculator().CalculateRectangleUnion(queriedWindow, windowsAbove);

            return (float)(queriedWindow.Area - subtractArea) / queriedWindow.Area * 100f;
        }

        /// <summary>
        /// Method to create a process as a user (in the user's context).
        /// </summary>
        /// <param name="executablePath">Executable for the process.</param>
        protected int CreateProcessAsUser(string executablePath)
        {
            var userToken = IntPtr.Zero;
            var userTokenDuplicate = IntPtr.Zero;

            // Get session_id of currently logged in user.
            int sessionId = LoginUserUtility.GetUserSessionID();
            if (sessionId < 0 || (uint)sessionId == 0xFFFFFFFF)
            {
                Logger.LogError("No session attached to the physical console. Cannot execute.");
                return -1;
            }
            else
            {
                Logger.LogDebug("Session ID is '{0}'.", sessionId);
            }

            // Get security token for that session.
            NativeMethods.WTSQueryUserToken((uint)sessionId, out userToken);
            int error = Marshal.GetLastWin32Error();
            Logger.LogDebug("User Token is '{0}'.", userToken);

            // If there is an error: Log error and return.
            if (error > 0)
            {
                switch (error)
                {
                    case 1314:
                        Logger.LogError("The caller does not have the SE_TCB_NAME privilege.");
                        break;

                    case 87:
                        Logger.LogError("Incorrect parameter in WTSQueryUserToken()");
                        break;

                    case 2:
                        Logger.LogError("The token query is for a session that does not exist.");
                        break;

                    case 1008:
                        Logger.LogError("The token query is for a session in which no user is logged-on. This occurs, for example, when the session is in the idle state or SessionId is zero.");
                        break;
                }

                return -1;
            }

            var processInformation = new NativeMethods.ProcessInformation();
            int processId = -1;
            try
            {
                var securityAttributes = new NativeMethods.SecurityAttributes
                {
                    bInheritHandle = false,
                    lpSecurityDescriptor = IntPtr.Zero
                };
                securityAttributes.Length = Marshal.SizeOf(securityAttributes);

                if (!NativeMethods.DuplicateTokenEx(
                      userToken,
                      NativeMethods.TokenGeneralAccess,
                      ref securityAttributes,
                      NativeMethods.SecurityImpersionationLevel.SecurityIdentification,
                      NativeMethods.TokenType.TokenPrimary,
                      ref userTokenDuplicate
                   ))
                {
                    Logger.LogError("Could not duplicate tokens.");
                    return -1;
                }

                var startupInformation = new NativeMethods.Startupinfo
                {
                    lpDesktop = @"WinSta0\Default"
                };
                startupInformation.cb = Marshal.SizeOf(startupInformation);

                if (!NativeMethods.CreateProcessAsUserW(
                    userTokenDuplicate, null, executablePath, ref securityAttributes,
                    ref securityAttributes, false, 0x00000010, IntPtr.Zero, null,
                    ref startupInformation, ref processInformation))
                {
                    error = Marshal.GetLastWin32Error();
                    string message = string.Format("Could not CreateProcessAsUser: {0}", error);
                    Logger.LogError(message);
                    return -1;
                }
                processId = processInformation.dwProcessID;
            }
            catch (Exception)
            {
                Logger.LogError("Could not create process.");
                return -1;
            }
            finally
            {
                NativeMethods.CloseHandle(userToken);
                NativeMethods.CloseHandle(userTokenDuplicate);
            }

            Logger.LogDebug("Created process with ID {0}.", processId);

            return processId;
        }

        /// <summary>
        /// Test to see if the current session is active (everything counts as "locked" or something in between.
        /// </summary>
        /// <returns></returns>
        private bool IsScreenUnlocked()
        {
            var buffer = IntPtr.Zero;

            try
            {
                if (NativeMethods.WTSQuerySessionInformation(IntPtr.Zero, NativeMethods.WtsCurrentSession, NativeMethods.WtsInfoClass.WTSSessionInfo, out buffer, out var bytesReturned))
                {
                    var testSize = Marshal.SizeOf(typeof(NativeMethods.WtsSessionInfo));
                    var sessionInfo = Marshal.PtrToStructure<NativeMethods.WtsSessionInfo>(buffer);
                    return sessionInfo.State == NativeMethods.WtsConnectedState.WTSActive;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                NativeMethods.WTSFreeMemory(buffer);
            }
        }
    }
}
