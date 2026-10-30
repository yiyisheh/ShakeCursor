using System.Runtime.InteropServices;

namespace ShakeCursor;

static class Native
{
    public const int WH_MOUSE_LL = 14;
    public const int WM_QUIT = 0x0012;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_XBUTTONDOWN = 0x020B;

    public const int VK_LBUTTON = 0x01, VK_RBUTTON = 0x02, VK_MBUTTON = 0x04, VK_XBUTTON1 = 0x05, VK_XBUTTON2 = 0x06;

    public const uint SPI_SETCURSORS = 0x0057;
    public const int IDC_ARROW = 32512;
    public const int UOI_NAME = 2;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int MDT_EFFECTIVE_DPI = 0;

    /// <summary>System cursors temporarily replaced by the big arrow (normal, text, hand, resize and so on).</summary>
    public static readonly uint[] ReplacedCursorIds =
        [32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650, 32651];

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public POINT pt; }

    [StructLayout(LayoutKind.Sequential)]
    public struct ICONINFO { [MarshalAs(UnmanagedType.Bool)] public bool fIcon; public int xHotspot, yHotspot; public IntPtr hbmMask, hbmColor; }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPV5HEADER
    {
        public uint bV5Size; public int bV5Width, bV5Height; public ushort bV5Planes, bV5BitCount;
        public uint bV5Compression, bV5SizeImage; public int bV5XPelsPerMeter, bV5YPelsPerMeter;
        public uint bV5ClrUsed, bV5ClrImportant, bV5RedMask, bV5GreenMask, bV5BlueMask, bV5AlphaMask, bV5CSType;
        public int ep0, ep1, ep2, ep3, ep4, ep5, ep6, ep7, ep8;
        public uint bV5GammaRed, bV5GammaGreen, bV5GammaBlue, bV5Intent, bV5ProfileData, bV5ProfileSize, bV5Reserved;
    }

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max);
    [DllImport("user32.dll")] public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string? name);

    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
    [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetSystemCursor(IntPtr hcur, uint id);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SystemParametersInfo(uint action, uint param, IntPtr pv, uint winIni);
    [DllImport("user32.dll")] public static extern IntPtr LoadCursor(IntPtr hInstance, int name);
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr CopyIcon(IntPtr hIcon);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr CreateIconIndirect(ref ICONINFO info);
    [DllImport("user32.dll")]
    public static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr hIcon, int cx, int cy, uint step, IntPtr brush, uint flags);
    public const uint DI_NORMAL = 0x0003;

    [DllImport("gdi32.dll")] public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPV5HEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateBitmap(int w, int h, uint planes, uint bpp, IntPtr bits);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);

    [DllImport("user32.dll")] public static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetUserObjectInformation(IntPtr obj, int index, char[] info, int length, out int needed);

    public static bool IsAnyMouseButtonDown() =>
        Down(VK_LBUTTON) || Down(VK_RBUTTON) || Down(VK_MBUTTON) || Down(VK_XBUTTON1) || Down(VK_XBUTTON2);

    static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    public static string WindowStationName()
    {
        var buf = new char[256];
        return GetUserObjectInformation(GetProcessWindowStation(), UOI_NAME, buf, buf.Length * 2, out _)
            ? new string(buf).TrimEnd('\0')
            : "";
    }
}
