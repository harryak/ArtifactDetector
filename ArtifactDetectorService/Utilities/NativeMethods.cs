using System;
using System.Runtime.InteropServices;
using static ItsApe.ArtifactDetector.Utilities.NativeStructures;

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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, ref RectangularOutline rect);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, [Out] out uint dwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UIntPtr nSize, ref uint vNumberOfBytesRead);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UIntPtr nSize, ref uint vNumberOfBytesRead);

        [DllImport("Wtsapi32.dll")]
        internal static extern void WTSFreeMemory(IntPtr pointer);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern int WTSEnumerateSessions(IntPtr hServer, int Reserved, int Version, ref IntPtr ppSessionInfo, ref int pCount);

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
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecurityAttributes
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Startupinfo
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

        /// <summary>
        /// This is the structure for a list view item, such as the icons on the desktop.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct ListViewItem
        {
            /// <summary>
            /// Set of flags that specify which members of this structure contain data to be set or which members are being requested. This member can have one or more of the following flags set:
            /// LVIF_COLFMT      The piColFmt member is valid or must be set. If this flag is used, the cColumns member is valid or must be set.
            /// LVIF_COLUMNS     The cColumns member is valid or must be set.
            /// LVIF_DI_SETITEM  The operating system should store the requested list item information and not ask for it again. This flag is used only with the LVN_GETDISPINFO notification code.
            /// LVIF_GROUPID     The iGroupId member is valid or must be set. If this flag is not set when an LVM_INSERTITEM message is sent, the value of iGroupId is assumed to be I_GROUPIDCALLBACK.
            /// LVIF_IMAGE  	 The iImage member is valid or must be filled in.
            /// LVIF_INDENT      The iIndent member is valid or must be filled in.
            /// LVIF_NORECOMPUTE The control will not generate a LVN_GETDISPINFO message to retrieve text information if it receives a LVM_GETITEM message. Instead, the pszText member will contain LPSTR_TEXTCALLBACK.
            /// LVIF_PARAM       The lParam member is valid or must be filled in.
            /// LVIF_STATE 	     The state member is valid or must be filled in.
            /// LVIF_TEXT        The pszText member is valid or must be filled in.
            /// </summary>
            public int mask;

            /// <summary>
            /// Zero-based index of the item to which this structure refers.
            /// </summary>
            public int iItem;

            /// <summary>
            /// One-based index of the subitem to which this structure refers, or zero if this structure refers to an item rather than a subitem.
            /// </summary>
            public int iSubItem;

            /// <summary>
            /// Indicates the item's state, state image, and overlay image. The stateMask member indicates the valid bits of this member.
            ///
            /// Bits 0 through 7 of this member contain the item state flags. This can be one or more of the item state values.
            ///
            /// Bits 8 through 11 of this member specify the one-based overlay image index. Both the full-sized icon image list and the small icon image list can have overlay images.
            /// The overlay image is superimposed over the item's icon image. If these bits are zero, the item has no overlay image. To isolate these bits, use the LVIS_OVERLAYMASK mask.
            /// To set the overlay image index in this member, you should use the INDEXTOOVERLAYMASK macro. The image list's overlay images are set with the ImageList_SetOverlayImage function.
            ///
            /// Bits 12 through 15 of this member specify the state image index. The state image is displayed next to an item's icon to indicate an application-defined state.
            /// If these bits are zero, the item has no state image. To isolate these bits, use the LVIS_STATEIMAGEMASK mask. To set the state image index, use the INDEXTOSTATEIMAGEMASK macro.
            /// The state image index specifies the index of the image in the state image list that should be drawn. The state image list is specified with the LVM_SETIMAGELIST message.
            /// </summary>
            public int state;

            /// <summary>
            /// Value specifying which bits of the state member will be retrieved or modified. For example, setting this member to LVIS_SELECTED will cause only the item's selection state to be retrieved.
            ///
            /// This member allows you to modify one or more item states without having to retrieve all of the item states first.
            /// For example, setting this member to LVIS_SELECTED and state to zero will cause the item's selection state to be cleared, but none of the other states will be affected.
            ///
            /// To retrieve or modify all of the states, set this member to(UINT)-1.
            ///
            /// You can use the macro ListView_SetItemState both to set and to clear bits.
            /// </summary>
            public int stateMask;

            /// <summary>
            /// If the structure specifies item attributes, pszText is a pointer to a null-terminated string containing the item text.
            /// When responding to an LVN_GETDISPINFO notification, be sure that this pointer remains valid until after the next notification has been received.
            ///
            /// If the structure receives item attributes, pszText is a pointer to a buffer that receives the item text.
            /// Note that although the list-view control allows any length string to be stored as item text, only the first 260 TCHARs are displayed.
            ///
            /// If the value of pszText is LPSTR_TEXTCALLBACK, the item is a callback item.
            /// If the callback text changes, you must explicitly set pszText to LPSTR_TEXTCALLBACK and notify the list-view control of the change by sending an LVM_SETITEM or LVM_SETITEMTEXT message.
            ///
            /// Do not set pszText to LPSTR_TEXTCALLBACK if the list-view control has the LVS_SORTASCENDING or LVS_SORTDESCENDING style.
            /// </summary>
            public IntPtr pszText;

            /// <summary>
            /// Number of TCHARs in the buffer pointed to by pszText, including the terminating NULL.
            ///
            /// This member is only used when the structure receives item attributes. It is ignored when the structure specifies item attributes.
            /// For example, cchTextMax is ignored during LVM_SETITEM and LVM_INSERTITEM. It is read-only during LVN_GETDISPINFO and other LVN_ notifications.
            /// </summary>
            public int cchTextMax;

            /// <summary>
            /// Index of the list-view item's icon in the icon list and the small icon image list.
            ///
            /// If this member is the I_IMAGECALLBACK value, the parent window is responsible for storing the index.
            /// In this case, the list-view control sends the parent an LVN_GETDISPINFO message to get the index when it needs to display the image.
            /// </summary>
            public int iImage;

            /// <summary>
            /// Specifies the 32-bit value of the item. If you use the LVM_SORTITEMS message, the list-view control passes this value to the application-defined comparison function.
            /// You can also use the LVM_FINDITEM message to search a list-view control for an item with a specified lParam value.
            /// </summary>
            public IntPtr lParam;

            /// <summary>
            /// Number of image widths to indent the item. A single indentation equals the width of an item image.
            /// Therefore, the value 1 indents the item by the width of one image, the value 2 indents by two images, and so on.
            /// Note that this member is supported only for items. Attempting to set subitem indentation will cause the calling function to fail.
            /// </summary>
            public int iIndent;

            /// <summary>
            /// Identifier (ID) of the group. It can be one of the following values:
            /// I_GROUPIDCALLBACK  The listview control sends the parent an LVN_GETDISPINFO notification message to retrieve the index of the group.
            /// I_GROUPIDNONE      The listview control has no group.
            /// </summary>
            public uint iGroupId;

            /// <summary>
            /// A pointer to an array of column indices, specifying which columns are displayed for this item, and the order of those columns.
            /// </summary>
            public UIntPtr puColumns;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TaskBarButton
        {
            /// <summary>
            /// Zero-based index of the button image.
            /// </summary>
            public int iBitmap;

            /// <summary>
            /// Command identifier associated with the button.
            /// </summary>
            public int idCommand;

            [StructLayout(LayoutKind.Explicit)]
            private struct TBBUTTON_U
            {
                [FieldOffset(0)] public byte fsState;
                [FieldOffset(1)] public byte fsStyle;
                [FieldOffset(0)] private readonly IntPtr bReserved;
            }

            private TBBUTTON_U union;
            public byte fsState { get { return union.fsState; } set { union.fsState = value; } }
            public byte fsStyle { get { return union.fsStyle; } set { union.fsStyle = value; } }
            public UIntPtr dwData;
            public IntPtr iString;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct WtsSessionInfo
        {
            public const int WinStationNameLength = 32;
            public const int DomainLength = 17;
            public const int UserNameLength = 20;

            public WtsConnectedState State;
            public int SessionId;
            public int IncomingBytes;
            public int OutgoingBytes;
            public int IncomingFrames;
            public int OutgoingFrames;
            public int IncomingCompressedBytes;
            public int OutgoingCompressedBytes;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = WinStationNameLength)]
            public byte[] WinStationNameRaw;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = DomainLength)]
            public byte[] DomainRaw;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = UserNameLength + 1)]
            public byte[] UserNameRaw;

            public long ConnectTimeUTC;
            public long DisconnectTimeUTC;
            public long LastInputTimeUTC;
            public long LogonTimeUTC;
            public long CurrentTimeUTC;
        }

        #endregion structs

        #region Windows Messages

        public abstract class WindowStyles
        {
            public const uint WS_BORDER               = 0x00800000;
            public const uint WS_CAPTION              = 0x00C00000;
            public const uint WS_CHILD                = 0x40000000;
            public const uint WS_CHILDWINDOW          = WS_CHILD;
            public const uint WS_CLIPCHILDREN         = 0x02000000;
            public const uint WS_CLIPSIBLINGS         = 0x04000000;
            public const uint WS_DISABLED             = 0x08000000;
            public const uint WS_DLGFRAME             = 0x00400000;
            public const uint WS_EX_ACCEPTFILES       = 0x00000010;
            public const uint WS_EX_APPWINDOW         = 0x00040000;
            public const uint WS_EX_CLIENTEDGE        = 0x00000200;
            public const uint WS_EX_COMPOSITED        = 0x02000000;
            public const uint WS_EX_CONTEXTHELP       = 0x00000400;
            public const uint WS_EX_CONTROLPARENT     = 0x00010000;
            public const uint WS_EX_DLGMODALFRAME     = 0x00000001;
            public const uint WS_EX_LAYERED           = 0x00080000;
            public const uint WS_EX_LAYOUTRTL         = 0x00400000;

            //Extended Window Styles
            public const uint WS_EX_LEFT              = 0x00000000;

            public const uint WS_EX_LEFTSCROLLBAR     = 0x00004000;
            public const uint WS_EX_LTRREADING        = 0x00000000;
            public const uint WS_EX_MDICHILD          = 0x00000040;
            public const uint WS_EX_NOACTIVATE        = 0x08000000;
            public const uint WS_EX_NOINHERITLAYOUT   = 0x00100000;
            public const uint WS_EX_NOPARENTNOTIFY    = 0x00000004;
            public const uint WS_EX_OVERLAPPEDWINDOW  = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
            public const uint WS_EX_PALETTEWINDOW     = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST);
            public const uint WS_EX_RIGHT             = 0x00001000;
            public const uint WS_EX_RIGHTSCROLLBAR    = 0x00000000;
            public const uint WS_EX_RTLREADING        = 0x00002000;
            public const uint WS_EX_STATICEDGE        = 0x00020000;
            public const uint WS_EX_TOOLWINDOW        = 0x00000080;
            public const uint WS_EX_TOPMOST           = 0x00000008;
            public const uint WS_EX_TRANSPARENT       = 0x00000020;
            public const uint WS_EX_WINDOWEDGE        = 0x00000100;
            public const uint WS_GROUP                = 0x00020000;
            public const uint WS_HSCROLL              = 0x00100000;
            public const uint WS_ICONIC               = WS_MINIMIZE;
            public const uint WS_MAXIMIZE             = 0x01000000;
            public const uint WS_MAXIMIZEBOX          = 0x00010000;
            public const uint WS_MINIMIZE             = 0x20000000;
            public const uint WS_MINIMIZEBOX          = 0x00020000;
            public const uint WS_OVERLAPPED           = 0x00000000;

            public const uint WS_OVERLAPPEDWINDOW     =
            ( WS_OVERLAPPED  |
              WS_CAPTION     |
              WS_SYSMENU     |
              WS_THICKFRAME  |
              WS_MINIMIZEBOX |
              WS_MAXIMIZEBOX );

            public const uint WS_POPUP                = 0x80000000;

            public const uint WS_POPUPWINDOW =
            ( WS_POPUP   |
              WS_BORDER  |
              WS_SYSMENU );

            public const uint WS_SIZEBOX              = WS_THICKFRAME;
            public const uint WS_SYSMENU              = 0x00080000;
            public const uint WS_TABSTOP              = 0x00010000;
            public const uint WS_THICKFRAME           = 0x00040000;
            public const uint WS_TILED                = WS_OVERLAPPED;
            public const uint WS_TILEDWINDOW          = WS_OVERLAPPEDWINDOW;
            public const uint WS_VISIBLE              = 0x10000000;
            public const uint WS_VSCROLL              = 0x00200000;
        }

        internal class LVIF
        {
            public const int TEXT = 0x0001;
        }

        internal class LVM
        {
            public const uint FIRST = 0x1000;
            public const uint GETITEMCOUNT    = FIRST + 4;
            public const uint GETITEMRECT     = FIRST + 14;
            public const uint GETITEMTEXTW    = FIRST + 45;
            public const uint GETITEMW        = FIRST + 75;
        }

        internal class MEM
        {
            public const uint COMMIT  = 0x1000;
            public const uint FREE    = 0x10000;
            public const uint RELEASE = 0x8000;
            public const uint RESERVE = 0x2000;
        }

        internal class PAGE
        {
            public const uint READWRITE = 4;
        }

        internal class PROCESS_VM
        {
            public const uint OPERATION = 0x0008;
            public const uint READ      = 0x0010;
            public const uint WRITE     = 0x0020;
        }

        internal class TB
        {
            public const uint BUTTONCOUNT    = WM.USER + 24;
            public const uint GETBUTTON      = WM.USER + 23;
            public const uint GETBUTTONTEXTW = WM.USER + 75;
        }

        internal class WM
        {
            public const uint USER = 0x0400;
        }

        #endregion Windows Messages
    }
}
