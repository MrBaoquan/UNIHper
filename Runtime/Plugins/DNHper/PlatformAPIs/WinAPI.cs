using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DNHper {

    public class WinAPI {

        // 调用命令
        public static string CALLCMD (string InParameter) {
            System.Diagnostics.Process _process = new System.Diagnostics.Process ();
            System.Diagnostics.ProcessStartInfo _startInfo = new System.Diagnostics.ProcessStartInfo ();

            _startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            _startInfo.FileName = "cmd.exe";
            _startInfo.Arguments = InParameter;
            _startInfo.CreateNoWindow = true;
            _startInfo.UseShellExecute = false;
            _startInfo.StandardOutputEncoding = Encoding.Default;
            _startInfo.RedirectStandardOutput = true;
            _process.StartInfo = _startInfo;
            _process.Start ();
            using (StreamReader _reader = _process.StandardOutput) {
                StreamReader s = _process.StandardOutput;
                _process.WaitForExit ();
                return s.ReadToEnd ().Trim ();
            }
        }

        public static Process FindProcess (string ProcessFileName) {
            try {
                if (Path.IsPathRooted (ProcessFileName)) {
                    return Process
                        .GetProcessesByName (Path.GetFileNameWithoutExtension (ProcessFileName)).ToList ().Where (_process =>
                            _process.GetMainModuleFileName () == ProcessFileName
                        ).FirstOrDefault ();
                }
                return Process.GetProcesses ().ToList ().Where (_process => _process.ProcessName == ProcessFileName).FirstOrDefault ();
            } catch (System.Exception e) {
                Console.WriteLine (e.Message);
                return default (Process);
            }

        }

        public static bool OpenProcess (string Path, string Args = "", bool runas = false) {
            try {
                Process _process = new Process ();
                _process.StartInfo.FileName = Path;
                if (runas)
                    _process.StartInfo.Verb = "runas";
                _process.StartInfo.Arguments = Args;
                return _process.Start ();
            } catch (System.Exception e) {
                return false;
            }
        }

        public static bool OpenProcessIfNotOpend (string Path, string Args = "", bool runas = false) {
            if (WinAPI.FindProcess (Path) != default (Process)) return false;
            return OpenProcess (Path, Args, runas);
        }

        // 窗口是否最大化
        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool IsIconic (IntPtr hWnd);

        // 窗口是否最小化

        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool IsZoomed (IntPtr hWnd);

        // 操作窗口
        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool ShowWindow (IntPtr hwnd, int nCmdShow);

        // 获取当前激活窗口

        [DllImport ("user32.dll")]
        public static extern IntPtr GetForegroundWindow ();

        //激活指定窗口
        [DllImport ("user32.dll")]
        public static extern bool SetForegroundWindow (IntPtr hWnd);

        [DllImport ("user32.dll")]
        public static extern IntPtr FindWindow (string lpClassName, string lpWindowName);

        [DllImport ("kernel32.dll")]
        public static extern uint GetLastError ();

        [DllImport ("user32.dll")]
        [
            return :MarshalAs (UnmanagedType.Bool)
        ]
        public static extern bool IsHungAppWindow (IntPtr hWnd);

        // 鼠标事件模拟
        [DllImport ("user32.dll", EntryPoint = "mouse_event")]
        public static extern void mouse_event (

            int dwFlags,

            int dx,

            int dy,

            int cButtons,

            int dwExtraInfo
        );

        [DllImport ("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        public static extern UInt32 GetWindowLong (IntPtr hWnd, int nIndex);

        [DllImport ("user32.dll", EntryPoint = "SetWindowLongA", CharSet = CharSet.Auto)]
        public static extern int SetWindowLong (IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowPos (IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, SetWindowPosFlags flags);

        public static IntPtr CurrentWindow () {
            string _process = Process.GetCurrentProcess ().ProcessName;
            return FindWindow (null, _process);
        }

        // public const int GWL_STYLE = (-16);
        // public const int WS_CAPTION = 0xC00000;
        // public const int WS_VISIBLE = 0x10000000;
        // public const int WS_HSCROLL = 0x00100000;
        // public const int WS_BORDER = 0x00800000;

        [DllImport ("User32.dll")]
        private static extern bool ShowWindowAsync (IntPtr hWnd, int cmdShow);

        [DllImport ("User32")]
        public extern static void SetCursorPos (int x, int y);

        [System.Runtime.InteropServices.DllImport ("user32.dll", EntryPoint = "ShowCursor")]
        public extern static bool ShowCursor (bool bShow);

        // Registers a hot key with Windows.
        [DllImport ("user32.dll")]
        public static extern bool RegisterHotKey (IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport ("user32.dll")]
        public static extern bool UnregisterHotKey (IntPtr hWnd, int id);

        // 常量
        public const int MOUSEEVENTF_LEFTDOWN = 0x2;
        public const int MOUSEEVENTF_LEFTUP = 0x4;

        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;

        public const int MOUSEEVENTF_MIDDLEUP = 0x40;

        public const int MOUSEEVENTF_MOVE = 0x1;

        public const int MOUSEEVENTF_RIGHTDOWN = 0x8;

        public const int MOUSEEVENTF_RIGHTUP = 0x10;
    }

    public enum CMDShow {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = SW_SHOWNORMAL,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3, //最大化  
        SW_SHOW = 5,
        SW_RESTORE = 9
    }

    public enum HWndInsertAfter {
        HWND_TOPMOST = -1,
        HWND_NOTOPMOST = -2,
        HWND_BOTTOM = 1,
        HWND_TOP = 0
    }

    public enum UFullScreenMode {
        //
        // 摘要:
        //     Exclusive Mode.
        ExclusiveFullScreen = 0,
        //
        // 摘要:
        //     Fullscreen window.
        FullScreenWindow = 1,
        //
        // 摘要:
        //     Maximized window.
        MaximizedWindow = 2,
        //
        // 摘要:
        //     Windowed.
        Windowed = 3,

        // Not Unity buildin value below
        MinimizedWindow = 11,
    }

    [Flags]
    public enum SetWindowPosFlags : uint {
        // ReSharper disable InconsistentNaming

        /// <summary>
        ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
        /// </summary>
        SWP_ASYNCWINDOWPOS = 0x4000,

        /// <summary>
        ///     Prevents generation of the WM_SYNCPAINT message.
        /// </summary>
        SWP_DEFERERASE = 0x2000,

        /// <summary>
        ///     Draws a frame (defined in the window's class description) around the window.
        /// </summary>
        SWP_DRAWFRAME = 0x0020,

        /// <summary>
        ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
        /// </summary>
        SWP_FRAMECHANGED = 0x0020,

        /// <summary>
        ///     Hides the window.
        /// </summary>
        SWP_HIDEWINDOW = 0x0080,

        /// <summary>
        ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOACTIVATE = 0x0010,

        /// <summary>
        ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
        /// </summary>
        SWP_NOCOPYBITS = 0x0100,

        /// <summary>
        ///     Retains the current position (ignores X and Y parameters).
        /// </summary>
        SWP_NOMOVE = 0x0002,

        /// <summary>
        ///     Does not change the owner window's position in the Z order.
        /// </summary>
        SWP_NOOWNERZORDER = 0x0200,

        /// <summary>
        ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
        /// </summary>
        SWP_NOREDRAW = 0x0008,

        /// <summary>
        ///     Same as the SWP_NOOWNERZORDER flag.
        /// </summary>
        SWP_NOREPOSITION = 0x0200,

        /// <summary>
        ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// </summary>
        SWP_NOSENDCHANGING = 0x0400,

        /// <summary>
        ///     Retains the current size (ignores the cx and cy parameters).
        /// </summary>
        SWP_NOSIZE = 0x0001,

        /// <summary>
        ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOZORDER = 0x0004,

        /// <summary>
        ///     Displays the window.
        /// </summary>
        SWP_SHOWWINDOW = 0x0040,

        // ReSharper restore InconsistentNaming
    }

    [Flags]
    public enum GWL_STYLE : long {
        WS_BORDER = 0x00800000L,
        //The window has a thin - line border.
        WS_CAPTION = 0x00C00000L,
        //The window has a title bar (includes the WS_BORDER style).
        WS_CHILD = 0x40000000L,
        //The window is a child window.A window with this style cannot have a menu bar.This style cannot be used with the WS_POPUP style.
        WS_CHILDWINDOW = 0x40000000L,
        //Same as the WS_CHILD style.
        WS_CLIPCHILDREN = 0x02000000L,
        //Excludes the area occupied by child windows when drawing occurs within the parent window.This style is used when creating the parent window.
        WS_CLIPSIBLINGS = 0x04000000L,
        //Clips child windows relative to each other;
        //that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated.If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window.
        WS_DISABLED = 0x08000000L,
        //The window is initially disabled.A disabled window cannot receive input from the user.To change this after a window has been created, use the EnableWindow function.
        WS_DLGFRAME = 0x00400000L,
        //The window has a border of a style typically used with dialog boxes.A window with this style cannot have a title bar.
        WS_GROUP = 0x00020000L,
        //The window is the first control of a group of controls.The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style.The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group.The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys.
        //You can turn this style on and off to change dialog box navigation.To change this style after a window has been created, use the SetWindowLong function.
        WS_HSCROLL = 0x00100000L,
        //The window has a horizontal scroll bar.
        WS_ICONIC = 0x20000000L,
        //The window is initially minimized.Same as the WS_MINIMIZE style.
        WS_MAXIMIZE = 0x01000000L,
        //The window is initially maximized.
        WS_MAXIMIZEBOX = 0x00010000L,
        //The window has a maximize button.Cannot be combined with the WS_EX_CONTEXTHELP style.The WS_SYSMENU style must also be specified.
        WS_MINIMIZE = 0x20000000L,
        //The window is initially minimized.Same as the WS_ICONIC style.
        WS_MINIMIZEBOX = 0x00020000L,
        //The window has a minimize button.Cannot be combined with the WS_EX_CONTEXTHELP style.The WS_SYSMENU style must also be specified.
        WS_OVERLAPPED = 0x00000000L,
        //The window is an overlapped window.An overlapped window has a title bar and a border.Same as the WS_TILED style.
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        //The window is an overlapped window.Same as the WS_TILEDWINDOW style.
        WS_POPUP = 0x80000000L,
        //The window is a pop - up window.This style cannot be used with the WS_CHILD style.
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        //The window is a pop - up window.The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.
        WS_SIZEBOX = 0x00040000L,
        //The window has a sizing border.Same as the WS_THICKFRAME style.
        WS_SYSMENU = 0x00080000L,
        //The window has a window menu on its title bar.The WS_CAPTION style must also be specified.
        WS_TABSTOP = 0x00010000L,
        //The window is a control that can receive the keyboard focus when the user presses the TAB key.Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style.
        //You can turn this style on and off to change dialog box navigation.To change this style after a window has been created, use the SetWindowLong function.For user - created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.
        WS_THICKFRAME = 0x00040000L,
        //The window has a sizing border.Same as the WS_SIZEBOX style.
        WS_TILED = 0x00000000L,
        //The window is an overlapped window.An overlapped window has a title bar and a border.Same as the WS_OVERLAPPED style.
        WS_TILEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        //The window is an overlapped window.Same as the WS_OVERLAPPEDWINDOW style.
        WS_VISIBLE = 0x10000000L,
        //The window is initially visible.
        //This style can be turned on and off by using the ShowWindow or SetWindowPos function.
        WS_VSCROLL = 0x00200000L,
    }

    [Flags]
    public enum GWL_EXSTYLE : long {
        WS_EX_ACCEPTFILES = 0x00000010L,
        // The window accepts drag - drop files.
        WS_EX_APPWINDOW = 0x00040000L,
        // Forces a top - level window onto the taskbar when the window is visible.
        WS_EX_CLIENTEDGE = 0x00000200L,
        // The window has a border with a sunken edge.
        WS_EX_COMPOSITED = 0x02000000L,
        // Paints all descendants of a window in bottom - to - top painting order using double - buffering.Bottom - to - top painting order allows a descendent window to have translucency (alpha) and transparency (color - key) effects, but only
        // if the descendent window also has the WS_EX_TRANSPARENT bit set.Double - buffering allows the window and its descendents to be painted without flicker.This cannot be used
        // if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        // Windows 2000 : This style is not supported.
        WS_EX_CONTEXTHELP = 0x00000400L,
        // The title bar of the window includes a question mark.When the user clicks the question mark, the cursor changes to a question mark with a pointer.If the user then clicks a child window, the child receives a WM_HELP message.The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command.The Help application displays a pop - up window that typically contains help
        // for the child window.
        // WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
        WS_EX_CONTROLPARENT = 0x00010000L,
        // The window itself contains child windows that should take part in dialog box navigation.If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
        WS_EX_DLGMODALFRAME = 0x00000001L,
        // The window has a double border;
        // the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
        WS_EX_LAYERED = 0x00080000,
        // The window is a layered window.This style cannot be used
        // if the window has a class style of either CS_OWNDC or CS_CLASSDC.
        // Windows 8 : The WS_EX_LAYERED style is supported
        // for top - level windows and child windows.Previous Windows versions support WS_EX_LAYERED only
        // for top - level windows.
        WS_EX_LAYOUTRTL = 0x00400000L,
        // If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the horizontal origin of the window is on the right edge.Increasing horizontal values advance to the left.
        WS_EX_LEFT = 0x00000000L,
        // The window has generic left - aligned properties.This is the default.
        WS_EX_LEFTSCROLLBAR = 0x00004000L,
        // If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (
        // if present) is to the left of the client area.For other languages, the style is ignored.
        WS_EX_LTRREADING = 0x00000000L,
        // The window text is displayed using left - to - right reading - order properties.This is the default.
        WS_EX_MDICHILD = 0x00000040L,
        // The window is a MDI child window.
        WS_EX_NOACTIVATE = 0x08000000L,
        // A top - level window created with this style does not become the foreground window when the user clicks it.The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        // The window should not be activated through programmatic access or via keyboard navigation by accessible technology, such as Narrator.
        // To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        // The window does not appear on the taskbar by default.To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        WS_EX_NOINHERITLAYOUT = 0x00100000L,
        // The window does not pass its window layout to its child windows.
        WS_EX_NOPARENTNOTIFY = 0x00000004L,
        // The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000L,
        // The window does not render to a redirection surface.This is
        // for windows that do not have visible content or that use mechanisms other than surfaces to provide their visual.
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        // The window is an overlapped window.
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        // The window is palette window, which is a modeless dialog box that presents an array of commands.
        WS_EX_RIGHT = 0x00001000L,
        // The window has generic "right-aligned"
        // properties.This depends on the window class.This style has an effect only
        // if the shell language is Hebrew, Arabic, or another language that supports reading - order alignment;
        // otherwise, the style is ignored.
        // Using the WS_EX_RIGHT style
        // for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively.Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles.
        WS_EX_RIGHTSCROLLBAR = 0x00000000L,
        // The vertical scroll bar (
        // if present) is to the right of the client area.This is the default.
        WS_EX_RTLREADING = 0x00002000L,
        // If the shell language is Hebrew, Arabic, or another language that supports reading - order alignment, the window text is displayed using right - to - left reading - order properties.For other languages, the style is ignored.
        WS_EX_STATICEDGE = 0x00020000L,
        // The window has a three - dimensional border style intended to be used
        // for items that do not accept user input.
        WS_EX_TOOLWINDOW = 0x00000080L,
        // The window is intended to be used as a floating toolbar.A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font.A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT + TAB.If a tool window has a system menu, its icon is not displayed on the title bar.However, you can display the system menu by right - clicking or by typing ALT + SPACE.
        WS_EX_TOPMOST = 0x00000008L,
        // The window should be placed above all non - topmost windows and should stay above them, even when the window is deactivated.To add or remove this style, use the SetWindowPos function.
        WS_EX_TRANSPARENT = 0x00000020L,
        // The window should not be painted until siblings beneath the window (that were created by the same thread) have been painted.The window appears transparent because the bits of underlying sibling windows have already been painted.
        // To achieve transparency without these restrictions, use the SetWindowRgn function.
        WS_EX_WINDOWEDGE = 0x00000100L,
        // The window has a border with a raised edge.
    }

    [Flags]
    public enum SetWindowLongIndex : int {
        GWL_EXSTYLE = -20,
        GWL_HINSTANCE = -6,
        GWL_ID = -12,
        GWL_STYLE = -16,
        GWL_USERDATA = -21,
        GWL_WNDPROC = -4
    }

    // 热键相关
    //定义了辅助键的名称（将数字转变为字符以便于记忆，也可去除此枚举而直接使用数值）
    [Flags ()]
    public enum KeyModifiers {
        None = 0,
        Alt = 1,
        Ctrl = 2,
        Shift = 4,
        WindowsKey = 8
    }

}