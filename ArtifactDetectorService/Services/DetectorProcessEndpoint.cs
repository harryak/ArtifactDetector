﻿using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using ItsApe.ArtifactDetector.DebugUtilities;
using ItsApe.ArtifactDetector.Models;
using ItsApe.ArtifactDetector.Utilities;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace ItsApe.ArtifactDetector.Services
{
    internal class DetectorProcessEndpoint : HasLogger, IDisposable
    {
        /// <summary>
        /// Size of the memory mapped file. Using MapPack this is more than enough.
        /// </summary>
        private const long MemoryMappedFileSize = 512;

        /// <summary>
        /// Name of the memory mapped file.
        /// </summary>
        private readonly string mmfName;

        /// <summary>
        /// Name of the semaphore protecting the memory mapped file.
        /// </summary>
        private readonly string semaphoreName;

        /// <summary>
        /// The session the process runs in.
        /// </summary>
        private readonly int sessionId;

        /// <summary>
        /// ID of the process in the session.
        /// </summary>
        private int processId;

        /// <summary>
        /// Memory region for sharing data with other processes.
        /// </summary>
        private MemoryMappedFile sharedMemory;

        /// <summary>
        /// Semaphore for sharedMemory, used across processes.
        ///
        /// WARNING: This gets acquired and released in independent threads!
        /// </summary>
        private Semaphore sharedMemoryLock;

        /// <summary>
        /// Initialize the endpoint for the process by starting the process and storing its ID.
        /// </summary>
        /// <param name="_sessionId"></param>
        public DetectorProcessEndpoint(int _sessionId)
        {
            sessionId = _sessionId;
            // TODO: Remove "000-"
            mmfName = @"Global\" + "000-" + ApplicationSetup.GetInstance().ApplicationGuid + "-" + sessionId;
            semaphoreName = mmfName + "-access";

            SetupMemoryMappedFile();

            if (!StartSessionProcess())
            {
                throw new Exception("Could not start process in session " + sessionId + ".");
            }
        }

        /// <summary>
        /// Call external process with runtime information.
        /// </summary>
        /// <param name="runtimeInformation">The runtime information to pass to and get from the process.</param>
        /// <returns>True on success.</returns>
        public bool CallProcess(ref ArtifactRuntimeInformation runtimeInformation)
        {
            // Backup non-serialized property.
            var referenceImageBackup = runtimeInformation.ReferenceImages;

            // Use memory stream to call process.
            using (var memoryStream = sharedMemory.CreateViewStream())
            {
                MessagePackSerializer.Serialize(memoryStream, runtimeInformation);

                // Release mutex for short time to let process get it.
                sharedMemoryLock.Release();

                // Wait for Mutex but do not release it to let the process wait for next call.
                if (sharedMemoryLock.WaitOne())
                {
                    memoryStream.Position = 0;
                    runtimeInformation = MessagePackSerializer.Deserialize<ArtifactRuntimeInformation>(memoryStream);
                }
            }

            // Restore backed up non-serialized property.
            runtimeInformation.ReferenceImages = referenceImageBackup;
            return true;
        }

        /// <summary>
        /// Get security identifier for a memory mapped file which allows authenticated local users full control.
        /// </summary>
        /// <param name="fileSecurity">The security object.</param>
        private void FillMMFSecurityIdentifier(out MemoryMappedFileSecurity fileSecurity)
        {
            fileSecurity = new MemoryMappedFileSecurity();
            fileSecurity.AddAccessRule(
                new AccessRule<MemoryMappedFileRights>(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Translate(typeof(NTAccount)),
                MemoryMappedFileRights.FullControl,
                AccessControlType.Allow));
        }

        /// <summary>
        /// Get security attributes for creation of session processes.
        /// </summary>
        /// <param name="securityAttributes">The struct to be filled.</param>
        private void FillProcessSecurityAttributes(out NativeMethods.SecurityAttributes securityAttributes)
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
        private void FillProcessStartupInformation(out NativeMethods.Startupinfo startupInformation)
        {
            // There is only one possibility for lpDesktop (bravo, Microsoft).
            startupInformation = new NativeMethods.Startupinfo
            {
                lpDesktop = @"WinSta0\Default"
            };
            startupInformation.cb = Marshal.SizeOf(startupInformation);
        }

        /// <summary>
        /// Get security identifier for a mutex which allows authenticated local users full control.
        /// </summary>
        /// <param name="mutexSecurity">The security object.</param>
        private void FillSemaphoreSecurityIdentifier(out SemaphoreSecurity mutexSecurity)
        {
            mutexSecurity = new SemaphoreSecurity();
            mutexSecurity.AddAccessRule(new SemaphoreAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Translate(typeof(NTAccount)),
                SemaphoreRights.FullControl,
                AccessControlType.Allow));
        }

        /// <summary>
        /// Get the full executable path (in quotes for space characters) of the external process to start.
        /// </summary>
        /// <returns>The full path.</returns>
        private string GetDetectorProcessName()
        {
            var ProcessDirectory = ApplicationSetup.GetInstance().GetExecutingDirectory().FullName;
            return "\"" + Uri.UnescapeDataString(Path.Combine(ProcessDirectory, ApplicationConfiguration.UserSessionApplicationName)) + "\" " + mmfName + " " + semaphoreName;
        }

        /// <summary>
        /// Get a duplicate of a user token for creation of session processes.
        /// </summary>
        /// <param name="userToken">The token to duplicate.</param>
        /// <param name="securityAttributes">Suitable security attributes.</param>
        /// <returns>The token or IntPtr.Zero on failure.</returns>
        private IntPtr GetUserTokenDuplicate(IntPtr userToken, ref NativeMethods.SecurityAttributes securityAttributes)
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
        /// Adds a memory mapped file protected by a mutex for the process.
        /// </summary>
        private void SetupMemoryMappedFile()
        {
            if (sharedMemoryLock == null)
            {
                // Aquire this mutex directly to make process wait for its release.
                FillSemaphoreSecurityIdentifier(out var semaphoreSecurity);
                sharedMemoryLock = new Semaphore(0, 1, semaphoreName, out bool _, semaphoreSecurity);
            }
            // Prepare shared memory via memory mapped file.
            if (sharedMemory == null)
            {
                FillMMFSecurityIdentifier(out var fileSecurity);

                sharedMemory = MemoryMappedFile.CreateOrOpen(
                   mmfName, MemoryMappedFileSize,
                   MemoryMappedFileAccess.ReadWrite,
                   MemoryMappedFileOptions.None,
                   fileSecurity, HandleInheritability.Inheritable);
            }
        }

        /// <summary>
        /// Start the detection process in the given session.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private bool StartSessionProcess()
        {
            if (processId > 0)
            {
                return true;
            }

            // Get security token for that session.
            var userToken = IntPtr.Zero;
            if (!NativeMethods.WTSQueryUserToken((uint)sessionId, out userToken))
            {
                Logger.LogError("Could not get user token for session {0}.", sessionId);
                return false;
            }

            var processInformation = new NativeMethods.ProcessInformation();
            FillProcessStartupInformation(out var startupInformation);
            FillProcessSecurityAttributes(out var securityAttributes);

            var userTokenDuplicate = GetUserTokenDuplicate(userToken, ref securityAttributes);
            if (userTokenDuplicate == IntPtr.Zero)
            {
                Logger.LogError("Could not duplicate tokens for session {0}.", sessionId);
                return false;
            }

            if (!NativeMethods.CreateProcessAsUserW(
                userTokenDuplicate, null, GetDetectorProcessName(), ref securityAttributes,
                ref securityAttributes, false, 0x00000010, IntPtr.Zero, null,
                ref startupInformation, ref processInformation))
            {
                Logger.LogError("Could not CreateProcessAsUser in session {0}.", sessionId);
                return false;
            }

            NativeMethods.CloseHandle(userToken);
            NativeMethods.CloseHandle(userTokenDuplicate);

            processId = processInformation.dwProcessID;
            Logger.LogDebug("Created process with ID {0} in session {1}.", processInformation.dwProcessID, sessionId);

            return true;
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    GC.KeepAlive(sharedMemoryLock);

                    if (sharedMemoryLock != null)
                        sharedMemoryLock.Close();
                    if (sharedMemory != null)
                        sharedMemory.Dispose();
                }

                // No need for disposing the process, it gets killed automatically when the session terminates.
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}