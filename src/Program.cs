namespace ShakeCursor;

static class Program
{
    static string LogPath => Path.Combine(Config.Dir, "ShakeCursor.log");

    [STAThread]
    static int Main(string[] args)
    {
        // The MSI starts us at sign-in for every user; quit if this user opted out
        if (args.Contains(Startup.AutostartArg) && !Startup.Enabled)
            return 0;

        // Started from a service or background session, the process would run but could neither see nor change the user's cursor
        var station = Native.WindowStationName();
        if (!string.Equals(station, "WinSta0", StringComparison.OrdinalIgnoreCase))
        {
            Log($"Not on the interactive desktop (window station {station}); exiting. Start ShakeCursor from the desktop.");
            return 2;
        }

        using var mutex = new Mutex(true, @"Local\ShakeCursor", out var isFirst);
        if (!isFirst)
            return 0;

        // Restore the system cursors however we exit (and on start, in case a previous run crashed while enlarged)
        RestoreSystemCursors();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreSystemCursors();
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Fail(e.ExceptionObject);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Fail(e.Exception);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
        return 0;
    }

    public static void RestoreSystemCursors() => Native.SystemParametersInfo(Native.SPI_SETCURSORS, 0, IntPtr.Zero, 0);

    static void Fail(object error)
    {
        RestoreSystemCursors();
        Log(error.ToString() ?? "Unknown error");
        Environment.Exit(1);
    }

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Config.Dir);
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            // Failing to log must not stop us from exiting
        }
    }
}
