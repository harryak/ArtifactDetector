using System;
using System.Runtime.InteropServices;

namespace ItsApe.ArtifactDetector.Utilities
{
    internal static class NativeMethods
    {
        internal const int TokenGeneralAccess = 0x10000000;
        internal const int WtsCurrentSession = -1;

        internal enum SecurityImpersionationLevel
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        internal enum TokenType
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true,
                CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcessAsUserW(
            IntPtr hToken, string lpApplicationName, string lpCommandLine,
            ref SecurityAttributes lpProcessAttributes, ref SecurityAttributes lpThreadAttributes,
            bool bInheritHandle, uint dwCreationFlags, IntPtr lpEnvrionment,
            string lpCurrentDirectory, ref Startupinfo lpStartupInfo,
            ref ProcessInformation lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateTokenEx(
            IntPtr hExistingToken, uint dwDesiredAccess,
            ref SecurityAttributes lpThreadAttributes,
            SecurityImpersionationLevel ImpersonationLevel, TokenType dwTokenType,
            ref IntPtr phNewToken);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

        [DllImport("Wtsapi32.dll")]
        internal static extern void WTSFreeMemory(IntPtr pointer);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        #region enums

        internal enum WtsConnectedState
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        internal enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
            WTSSessionInfoEx,
            WTSConfigInfo,
            WTSValidationInfo,
            WTSSessionAddressV4,
            WTSIsRemoteSession
        }

        #endregion enums

        #region structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecurityAttributes
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Startupinfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WtsSessionInfo
        {
            public int SessionId;

            [MarshalAs(UnmanagedType.LPStr)]
            public string pWinStationName;

            public WtsConnectedState State;
        }

        #endregion structs
    }
}
