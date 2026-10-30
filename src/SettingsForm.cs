using System.Drawing;
using Timer = System.Windows.Forms.Timer;

namespace ShakeCursor;

/// <summary>Settings window: changes apply while dragging (so you can shake to try them) and are saved to config.json 300 ms later.</summary>
sealed class SettingsForm : Form
{
    sealed record Row(
        string Name, string Hint, double Min, double Max, double Step,
        Func<Config, double> Get, Action<Config, double> Set, Func<double, string> Format);

    static string Ms(double v) => $"{v:0} ms";

    static Strings S => Strings.Current;

    static readonly Row[] Rows =
    [
        new(S.MaxScale, S.MaxScaleHint, 1.5, 12, 0.5,
            c => c.MaxScale, (c, v) => c.MaxScale = v, v => $"{v:0.#}{S.ScaleUnit}"),
        new(S.MinSpeed, S.MinSpeedHint, 300, 3000, 50,
            c => c.MinSpeed, (c, v) => c.MinSpeed = v, v => $"{v:0} px/s"),
        new(S.MinReversals, S.MinReversalsHint, 1, 6, 1,
            c => c.MinReversals, (c, v) => c.MinReversals = (int)v, v => $"{v:0}{S.TimesUnit}"),
        new(S.WindowMs, S.WindowMsHint, 300, 2000, 50,
            c => c.WindowMs, (c, v) => c.WindowMs = v, Ms),
        new(S.MinStroke, S.MinStrokeHint, 10, 200, 5,
            c => c.MinStroke, (c, v) => c.MinStroke = v, v => $"{v:0} px"),
        new(S.GrowPerSwing, S.GrowPerSwingHint, 0.05, 1, 0.05,
            c => c.GrowPerSwing, (c, v) => c.GrowPerSwing = v, v => $"{v * 100:0}%"),
        new(S.ReleaseDelay, S.ReleaseDelayHint, 0, 1500, 50,
            c => c.ReleaseDelayMs, (c, v) => c.ReleaseDelayMs = v, Ms),
        new(S.Shrink, S.ShrinkHint, 50, 1500, 50,
            c => c.ShrinkMs, (c, v) => c.ShrinkMs = v, Ms),
    ];

    readonly Action<Config> apply;
    readonly Config working;
    readonly Timer saveTimer = new() { Interval = 300 };
    readonly List<(Row Row, TrackBar Bar)> bars = new();

    public SettingsForm(Config current, Action<Config> apply)
    {
        this.apply = apply;
        working = current.Clone();

        Text = S.SettingsTitle;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ShowIcon = false;

        saveTimer.Tick += (_, _) =>
        {
            saveTimer.Stop();
            working.Save();
        };

        var hintColor = SystemColors.GrayText;
        var hintFont = new Font(Font.FontFamily, Font.Size * 0.9f);
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(LogicalToDeviceUnits(12)),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(260)));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(80)));

        foreach (var row in Rows)
        {
            var value = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            var bar = new TrackBar
            {
                Minimum = 0,
                Maximum = (int)Math.Round((row.Max - row.Min) / row.Step),
                TickStyle = TickStyle.None,
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = LogicalToDeviceUnits(30),
            };
            bar.Value = MathEx.Clamp((int)Math.Round((row.Get(working) - row.Min) / row.Step), bar.Minimum, bar.Maximum);
            value.Text = row.Format(row.Min + bar.Value * row.Step);
            bar.ValueChanged += (_, _) =>
            {
                var v = Math.Round(row.Min + bar.Value * row.Step, 4);
                row.Set(working, v);
                value.Text = row.Format(v);
                Changed();
            };
            bars.Add((row, bar));

            grid.Controls.Add(new Label { Text = row.Name, AutoSize = true, Anchor = AnchorStyles.Left });
            grid.Controls.Add(bar);
            grid.Controls.Add(value);
            var hint = new Label
            {
                Text = row.Hint,
                AutoSize = true,
                ForeColor = hintColor,
                Font = hintFont,
                Margin = new Padding(LogicalToDeviceUnits(6), 0, 0, LogicalToDeviceUnits(8)),
            };
            grid.Controls.Add(new Label());
            grid.Controls.Add(hint);
            grid.SetColumnSpan(hint, 2); // hint spans the slider and value columns to avoid wrapping
        }

        var startup = new CheckBox { Text = S.StartWithWindows, AutoSize = true, Checked = Startup.Enabled };
        startup.CheckedChanged += (_, _) => Startup.Enabled = startup.Checked;

        var reset = new Button { Text = S.RestoreDefaults, AutoSize = true };
        reset.Click += (_, _) => ResetToDefaults();
        var openFile = new Button { Text = S.OpenConfigFile, AutoSize = true };
        openFile.Click += (_, _) => TrayApp.OpenConfigFile();
        var close = new Button { Text = S.Close, AutoSize = true, DialogResult = DialogResult.OK };
        CancelButton = close;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, LogicalToDeviceUnits(8), 0, 0),
        };
        startup.Margin = new Padding(0, LogicalToDeviceUnits(6), LogicalToDeviceUnits(24), 0);
        buttons.Controls.AddRange([startup, reset, openFile, close]);
        grid.Controls.Add(buttons);
        grid.SetColumnSpan(buttons, 3);

        Controls.Add(grid);
        FormClosed += (_, _) =>
        {
            if (saveTimer.Enabled)
            {
                saveTimer.Stop();
                working.Save();
            }
        };
    }

    void Changed()
    {
        apply(working.Clone());
        saveTimer.Stop();
        saveTimer.Start();
    }

    void ResetToDefaults()
    {
        var defaults = new Config();
        foreach (var (row, bar) in bars)
            bar.Value = MathEx.Clamp((int)Math.Round((row.Get(defaults) - row.Min) / row.Step), bar.Minimum, bar.Maximum);
    }
}
