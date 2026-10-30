using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace ShakeCursor;

/// <summary>Tray app: once a shake starts, swaps the system cursor frame by frame for an arrow of the current size, and restores it when back to normal.</summary>
sealed class TrayApp : ApplicationContext
{
    const int MaxArrowHeight = 512;
    const double FollowTauMs = 40;   // smoothing time constant for the shown size
    const int SizeStep = 2;          // size quantisation; fewer cursor swaps and cached frames

    // Hidden top-level window: marshals hook-thread notifications to the UI thread and receives the MSI's WM_CLOSE on upgrade/uninstall.
    // Do not set ShowInTaskbar = false: WinForms would give it a hidden owner window; when the MSI sends WM_CLOSE to every window,
    // the owner dies first and takes this window with it without FormClosing, so the app never exits. It is never shown anyway.
    readonly Form invoker = new() { Text = "ShakeCursor" };
    readonly Timer frameTimer = new() { Interval = 15 };
    readonly Timer reloadTimer = new() { Interval = 200 };
    readonly ArrowCursor arrows = new();
    readonly ShakeDetector detector;
    readonly NotifyIcon tray;
    readonly FileSystemWatcher watcher;
    MouseHook? hook;
    SettingsForm? settingsForm;

    Config config;
    int baseHeight;
    double shownHeight;
    double lastFrameMs;
    int appliedHeight;   // 0 = the normal system cursor is showing

    public TrayApp()
    {
        _ = invoker.Handle;
        invoker.FormClosing += (_, _) => ExitThread();
        config = Config.Load();
        detector = new ShakeDetector(config);

        frameTimer.Tick += (_, _) => OnFrame();
        reloadTimer.Tick += (_, _) => ReloadConfig();

        Directory.CreateDirectory(Config.Dir);
        watcher = new FileSystemWatcher(Config.Dir, Path.GetFileName(Config.FilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        watcher.Changed += (_, _) => invoker.BeginInvoke(reloadTimer.Start);
        watcher.Created += (_, _) => invoker.BeginInvoke(reloadTimer.Start);
        watcher.Renamed += (_, _) => invoker.BeginInvoke(reloadTimer.Start);

        SystemEvents.SessionEnding += (_, _) => RestoreCursor();

        tray = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = Strings.Current.AppName,
            ContextMenuStrip = CreateMenu(),
            Visible = true,
        };
        tray.DoubleClick += (_, _) => ShowSettings();

        SetPaused(false);
    }

    void StartAnimation()
    {
        if (frameTimer.Enabled)
            return;
        if (appliedHeight == 0)
        {
            baseHeight = ArrowCursor.MeasureSystemArrowHeight();
            shownHeight = baseHeight;
        }
        lastFrameMs = MouseHook.NowMs;
        frameTimer.Start();
    }

    void OnFrame()
    {
        var now = MouseHook.NowMs;
        var dt = MathEx.Clamp(now - lastFrameMs, 1, 100);
        lastFrameMs = now;

        var level = detector.Tick(now, dt);
        var maxHeight = Math.Min(MaxArrowHeight, baseHeight * config.MaxScale);
        var eased = 1 - (1 - level) * (1 - level); // grow fast at first, then ease off
        var target = baseHeight + (maxHeight - baseHeight) * eased;
        shownHeight += (target - shownHeight) * (1 - Math.Exp(-dt / FollowTauMs));

        // quantise relative to the base height so an odd base still lands exactly back on it
        var steps = (int)Math.Round((shownHeight - baseHeight) / SizeStep);
        if (level <= 0 && steps <= 0)
        {
            RestoreCursor();
            frameTimer.Stop();
            return;
        }
        if (steps > 0)
            ApplyCursor(baseHeight + steps * SizeStep);
    }

    void ApplyCursor(int height)
    {
        if (height == appliedHeight)
            return;
        var source = arrows.Get(height);
        foreach (var id in Native.ReplacedCursorIds)
        {
            var copy = Native.CopyIcon(source); // SetSystemCursor takes ownership of the handle and destroys it
            if (copy != IntPtr.Zero && !Native.SetSystemCursor(copy, id))
                Native.DestroyIcon(copy);
        }
        appliedHeight = height;
    }

    void RestoreCursor()
    {
        if (appliedHeight == 0)
            return;
        Program.RestoreSystemCursors();
        appliedHeight = 0;
    }

    void ReloadConfig()
    {
        reloadTimer.Stop();
        ApplyConfig(Config.Load());
    }

    void SetPaused(bool paused)
    {
        if (paused)
        {
            hook?.Dispose();
            hook = null;
            frameTimer.Stop();
            RestoreCursor();
        }
        else
        {
            hook ??= new MouseHook(detector, () => invoker.BeginInvoke(StartAnimation));
        }
    }

    ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem(Strings.Current.AppName) { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        var pause = new ToolStripMenuItem(Strings.Current.Pause) { CheckOnClick = true };
        pause.CheckedChanged += (_, _) => SetPaused(pause.Checked);
        menu.Items.Add(pause);

        var startup = new ToolStripMenuItem(Strings.Current.StartWithWindows);
        startup.Click += (_, _) => Startup.Enabled = !Startup.Enabled;
        menu.Items.Add(startup);
        menu.Opening += (_, _) => startup.Checked = Startup.Enabled; // it can also be changed in the settings window

        menu.Items.Add(Strings.Current.Settings, null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Strings.Current.Exit, null, (_, _) => ExitThread());
        return menu;
    }

    void ShowSettings()
    {
        if (settingsForm is { IsDisposed: false })
        {
            settingsForm.Activate();
            return;
        }
        settingsForm = new SettingsForm(config, ApplyConfig);
        settingsForm.FormClosed += (_, _) => settingsForm = null;
        settingsForm.Show();
    }

    void ApplyConfig(Config updated)
    {
        config = updated;
        detector.Config = updated;
    }

    public static void OpenConfigFile() =>
        Process.Start(new ProcessStartInfo(Config.FilePath) { UseShellExecute = true });

    static Icon CreateTrayIcon()
    {
        const int size = 32;
        using var arrow = ArrowCursor.Render(26, out var tip);
        using var canvas = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(canvas))
            g.DrawImage(arrow, (size - arrow.Width) / 2 + tip.X / 2, 0);
        return Icon.FromHandle(canvas.GetHicon());
    }

    protected override void ExitThreadCore()
    {
        tray.Visible = false;
        settingsForm?.Close();
        hook?.Dispose();
        frameTimer.Stop();
        RestoreCursor();
        arrows.Dispose();
        watcher.Dispose();
        tray.Dispose();
        base.ExitThreadCore();
    }
}
