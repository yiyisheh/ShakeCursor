using Microsoft.Win32;

namespace ShakeCursor;

/// <summary>
/// Start with Windows, in two modes:
/// - Installed: the MSI writes "ShakeCursor.exe --autostart" to HKLM\…\Run for every user.
///   A user who opts out gets a flag in their own HKCU\Software\ShakeCursor, and an --autostart launch then quits. No admin rights needed.
/// - Portable: writes the current user's HKCU\…\Run.
/// </summary>
static class Startup
{
    public const string AutostartArg = "--autostart";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValue = "ShakeCursor";
    const string AppKey = @"Software\ShakeCursor";
    const string OptOutValue = "DisableAutostart";

    static bool InstalledForAllUsers
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(RunKey);
            return key?.GetValue(RunValue) is string;
        }
    }

    public static bool Enabled
    {
        get
        {
            if (InstalledForAllUsers)
            {
                using var app = Registry.CurrentUser.OpenSubKey(AppKey);
                return app?.GetValue(OptOutValue) is not 1;
            }
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            return run?.GetValue(RunValue) is string;
        }
        set
        {
            if (InstalledForAllUsers)
            {
                using var app = Registry.CurrentUser.CreateSubKey(AppKey);
                if (value)
                    app.DeleteValue(OptOutValue, throwOnMissingValue: false);
                else
                    app.SetValue(OptOutValue, 1, RegistryValueKind.DWord);
                return;
            }

            using var run = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value)
                run.SetValue(RunValue, $"\"{Application.ExecutablePath}\" {AutostartArg}");
            else
                run.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }
}
