using System;
using System.Runtime.InteropServices;

namespace CodexUsageBar
{
    static class Native
    {
        internal const int WindowStyleIndex = -16;
        internal const int ExtendedStyleIndex = -20;
        internal const long ChildStyle = 0x40000000L;
        internal const int TopmostStyle = 0x00000008;
        internal const uint OwnerWindow = 4;
        internal const int NoActivatePosition = 0x10;
        internal const int NoResizePosition = 0x1;
        internal const int NoMovePosition = 0x2;
        internal const uint AlphaAttribute = 0x2;
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
        public static bool AnimationsEnabled
        {
            get {
                bool enabled;
                return !SystemParametersInfo(0x1042, 0, out enabled, 0) || enabled;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left, Top, Right, Bottom;
        }
        public delegate bool EnumProc(IntPtr hwnd, IntPtr lparam);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDpiAwarenessContext(IntPtr h);
        [DllImport("user32.dll")]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumProc callback, IntPtr lparam);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr h);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        public static extern IntPtr GetWindowLongPtr(IntPtr h, int index);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr h, out Rect r);
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr h, out Rect r);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height,
                                               uint flags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetLayeredWindowAttributes(IntPtr h, uint colorKey, byte alpha, uint flags);
    }

}
