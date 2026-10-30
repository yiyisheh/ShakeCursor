using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ShakeCursor;

/// <summary>
/// %APPDATA%\ShakeCursor\config.json, one per user. Distances are logical pixels (divided by display scaling); times are milliseconds.
/// The file is a flat key/number object, read with a regex and written by hand, so no JSON library is needed.
/// </summary>
sealed class Config
{
    /// <summary>Largest size = system arrow height × MaxScale.</summary>
    public double MaxScale { get; set; } = 6;

    /// <summary>Minimum speed of a swing (px/s). Lower = easier to trigger.</summary>
    public double MinSpeed { get; set; } = 900;

    /// <summary>Minimum length of a swing; filters out hand jitter.</summary>
    public double MinStroke { get; set; } = 40;

    /// <summary>Swings needed within WindowMs before the cursor starts growing.</summary>
    public int MinReversals { get; set; } = 3;

    public double WindowMs { get; set; } = 700;

    /// <summary>Growth added per swing (0–1, where 1 is the maximum size). Higher grows faster.</summary>
    public double GrowPerSwing { get; set; } = 0.2;

    /// <summary>How long the cursor stays big after shaking stops.</summary>
    public double ReleaseDelayMs { get; set; } = 150;

    /// <summary>Time to shrink from the maximum size back to normal.</summary>
    public double ShrinkMs { get; set; } = 200;

    public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShakeCursor");

    public static string FilePath => Path.Combine(Dir, "config.json");

    static readonly PropertyInfo[] Properties = typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    static readonly Regex Entry = new(@"""(\w+)""\s*:\s*(-?[\d.]+(?:[eE][-+]?\d+)?|true|false)", RegexOptions.IgnoreCase);

    public static Config Load()
    {
        var config = new Config();
        try
        {
            if (!File.Exists(FilePath))
            {
                config.Save();
                return config;
            }

            foreach (Match m in Entry.Matches(File.ReadAllText(FilePath)))
            {
                var p = Array.Find(Properties, x => string.Equals(x.Name, m.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
                if (p == null)
                    continue;
                var raw = m.Groups[2].Value;
                object value = p.PropertyType == typeof(bool)
                    ? bool.Parse(raw)
                    : Convert.ChangeType(double.Parse(raw, CultureInfo.InvariantCulture), p.PropertyType, CultureInfo.InvariantCulture);
                p.SetValue(config, value);
            }
        }
        catch
        {
            // A broken file falls back to whatever was read plus defaults; never crash over it
        }
        return config.Clamped();
    }

    public void Save()
    {
        var sb = new StringBuilder("{\n");
        for (var i = 0; i < Properties.Length; i++)
        {
            var v = Properties[i].GetValue(this);
            var text = v is bool b ? (b ? "true" : "false") : Convert.ToString(v, CultureInfo.InvariantCulture);
            sb.Append($"  \"{Properties[i].Name}\": {text}{(i < Properties.Length - 1 ? "," : "")}\n");
        }
        sb.Append("}\n");

        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, sb.ToString());
        }
        catch
        {
            // If the file cannot be written, settings last for this session only
        }
    }

    public Config Clone() => (Config)MemberwiseClone();

    Config Clamped()
    {
        MaxScale = MathEx.Clamp(MaxScale, 1.5, 16);
        MinSpeed = MathEx.Clamp(MinSpeed, 100, 10000);
        MinStroke = MathEx.Clamp(MinStroke, 5, 1000);
        MinReversals = (int)MathEx.Clamp(MinReversals, 1, 10);
        WindowMs = MathEx.Clamp(WindowMs, 100, 3000);
        GrowPerSwing = MathEx.Clamp(GrowPerSwing, 0.01, 1);
        ReleaseDelayMs = MathEx.Clamp(ReleaseDelayMs, 0, 3000);
        ShrinkMs = MathEx.Clamp(ShrinkMs, 1, 3000);
        return this;
    }
}
