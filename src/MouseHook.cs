using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShakeCursor;

/// <summary>
/// Installs a global low-level mouse hook on its own thread and feeds every move, in logical pixels, to the detector.
/// The separate thread keeps cursor rendering on the UI thread from delaying the hook (Windows silently removes hooks that time out).
/// </summary>
sealed class MouseHook : IDisposable
{
    readonly ShakeDetector detector;
    readonly Action onShakeStart;
    readonly Native.HookProc proc; // keep a reference so the GC does not collect it
    readonly Thread thread;
    uint threadId;

    IntPtr lastMonitor;
    double lastScale = 1;

    public MouseHook(ShakeDetector detector, Action onShakeStart)
    {
        this.detector = detector;
        this.onShakeStart = onShakeStart;
        proc = HookCallback;

        using var ready = new ManualResetEventSlim();
        Exception? error = null;
        thread = new Thread(() =>
        {
            threadId = Native.GetCurrentThreadId();
            var hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, proc, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero)
                error = new InvalidOperationException($"SetWindowsHookEx failed (error {Marshal.GetLastWin32Error()})");
            ready.Set();
            if (hook == IntPtr.Zero)
                return;

            while (Native.GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }
            Native.UnhookWindowsHookEx(hook);
        })
        {
            IsBackground = true,
            Name = "MouseHook",
            Priority = ThreadPriority.AboveNormal,
        };
        thread.Start();
        ready.Wait();
        if (error != null)
            throw error;
    }

    public static double NowMs => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

    IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = (int)wParam;
            if (msg == Native.WM_MOUSEMOVE)
            {
                var info = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                var scale = ScaleAt(info.pt);
                if (detector.OnMove(info.pt.X / scale, info.pt.Y / scale, NowMs))
                    onShakeStart();
            }
            else if (msg is Native.WM_LBUTTONDOWN or Native.WM_RBUTTONDOWN or Native.WM_MBUTTONDOWN or Native.WM_XBUTTONDOWN)
            {
                detector.OnButton();
            }
        }
        return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    /// <summary>Scale factor of the monitor under the pointer (1.0 = 96 DPI).</summary>
    double ScaleAt(Native.POINT pt)
    {
        var monitor = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
        if (monitor != lastMonitor)
        {
            lastMonitor = monitor;
            lastScale = Native.GetDpiForMonitor(monitor, Native.MDT_EFFECTIVE_DPI, out var dpi, out _) == 0 ? dpi / 96.0 : 1;
        }
        return lastScale;
    }

    public void Dispose()
    {
        if (threadId != 0)
            Native.PostThreadMessage(threadId, Native.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        thread.Join(1000);
    }
}
