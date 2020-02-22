using ItsApe.ArtifactDetector.Models;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace ItsApe.ArtifactDetector.Utilities
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Delegate function to loop over windows.
        /// </summary>
        /// <param name="hWnd">Input window handle.</param>
        /// <param name="lParam">Parameters for the current window.</param>
        /// <returns>Can be disregarded.</returns>
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, [MarshalAs(UnmanagedType.SysInt)] int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowVisualInformation pwi);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, [MarshalAs(UnmanagedType.U4)] int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint dwProcessId);

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

        #region structs

#pragma warning disable IDE1006
        [StructLayout(LayoutKind.Sequential)]
        internal struct TBBUTTON
        {
            public int iBitmap;
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

#pragma warning restore IDE1006

#pragma warning disable CS0649
        /// <summary>
        /// This is the structure for a list view item, such as the icons on the desktop.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct LVITEMA
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
#pragma warning restore CS0649

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rectangle
        {
            public int Left, Top, Right, Bottom;

            public Rectangle(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public Rectangle(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public int Area
            {
                get { return Size.Width * Size.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(Rectangle r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator Rectangle(System.Drawing.Rectangle r)
            {
                return new Rectangle(r);
            }

            public static bool operator ==(Rectangle r1, Rectangle r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(Rectangle r1, Rectangle r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(Rectangle r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is Rectangle)
                    return Equals((Rectangle) obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new Rectangle((System.Drawing.Rectangle) obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle) this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowPlacement
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommands ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public WindowPosition MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public WindowPosition MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public Rectangle NormalPosition;

            /// <summary>
            /// Gets the default (empty) value.
            /// </summary>
            public static WindowPlacement Default
            {
                get
                {
                    WindowPlacement result = new WindowPlacement();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        /// <summary>
        /// A two-dimensional point for window positions.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowPosition
        {
            public int X;
            public int Y;

            public WindowPosition(int x, int y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Point(WindowPosition p)
            {
                return new Point(p.X, p.Y);
            }

            public static implicit operator WindowPosition(Point p)
            {
                return new WindowPosition(p.X, p.Y);
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowVisualInformation
        {
            public uint cbSize;
            public Rectangle rcWindow;
            public Rectangle rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WindowVisualInformation(bool? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (uint) (Marshal.SizeOf(typeof(WindowVisualInformation)));
            }
        }

        #endregion structs

        #region enums
        
        internal enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
                          /// <summary>
                          /// Activates the window and displays it as a maximized window.
                          /// </summary>      
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
            /// that owns the window is not responding. This flag should only be
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        #endregion enums

        #region Windows Messages

        internal class LVIF
        {
            public const int TEXT = 0x0001;
        }

        internal class LVM
        {
            public const uint FIRST = 0x1000;
            public const uint GETITEMCOUNT    = FIRST + 4;
            public const uint GETITEMPOSITION = FIRST + 16;
            public const uint GETITEMTEXTW    = FIRST + 45;
            public const uint GETITEMW        = FIRST + 75;
            public const uint SETITEMPOSITION = FIRST + 15;
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