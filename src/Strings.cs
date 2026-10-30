using System.Globalization;

namespace ShakeCursor;

/// <summary>UI text, picked from the Windows display language: Simplified Chinese, Traditional Chinese, Japanese, otherwise English.</summary>
sealed class Strings
{
    static Strings? current;

    // Not a static field initializer: the language tables are declared below and would still be null
    public static Strings Current => current ??= Pick(CultureInfo.CurrentUICulture);

    public string AppName = "", Pause = "", StartWithWindows = "", Settings = "", Exit = "";
    public string SettingsTitle = "", RestoreDefaults = "", OpenConfigFile = "", Close = "";
    public string MaxScale = "", MaxScaleHint = "", MinSpeed = "", MinSpeedHint = "";
    public string MinReversals = "", MinReversalsHint = "", WindowMs = "", WindowMsHint = "";
    public string MinStroke = "", MinStrokeHint = "", GrowPerSwing = "", GrowPerSwingHint = "";
    public string ReleaseDelay = "", ReleaseDelayHint = "", Shrink = "", ShrinkHint = "";
    public string TimesUnit = "", ScaleUnit = "";

    static Strings Pick(CultureInfo culture)
    {
        var name = culture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            var traditional = name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0
                              || name.EndsWith("TW", StringComparison.OrdinalIgnoreCase)
                              || name.EndsWith("HK", StringComparison.OrdinalIgnoreCase)
                              || name.EndsWith("MO", StringComparison.OrdinalIgnoreCase);
            return traditional ? TraditionalChinese : SimplifiedChinese;
        }
        return culture.TwoLetterISOLanguageName == "ja" ? Japanese : English;
    }

    static readonly Strings English = new()
    {
        AppName = "ShakeCursor",
        Pause = "Pause",
        StartWithWindows = "Start with Windows",
        Settings = "Settings…",
        Exit = "Exit",
        SettingsTitle = "ShakeCursor Settings",
        RestoreDefaults = "Restore defaults",
        OpenConfigFile = "Open config file",
        Close = "Close",
        MaxScale = "Maximum size",
        MaxScaleHint = "How many times the normal arrow it can grow",
        MinSpeed = "Trigger speed",
        MinSpeedHint = "How fast each swing must be; lower triggers more easily",
        MinReversals = "Swings to start",
        MinReversalsHint = "Back-and-forth swings needed before it starts growing",
        WindowMs = "Time window",
        WindowMsHint = "Those swings must happen within this time",
        MinStroke = "Shortest swing",
        MinStrokeHint = "Shorter movements are treated as hand jitter",
        GrowPerSwing = "Growth per swing",
        GrowPerSwingHint = "How much it grows with each swing (share of maximum size)",
        ReleaseDelay = "Hold after stop",
        ReleaseDelayHint = "How long it stays big after you stop shaking",
        Shrink = "Shrink duration",
        ShrinkHint = "How long it takes to shrink back to normal",
        TimesUnit = "",
        ScaleUnit = "×",
    };

    static readonly Strings SimplifiedChinese = new()
    {
        AppName = "晃动鼠标放大指针",
        Pause = "暂停",
        StartWithWindows = "开机自启",
        Settings = "设置…",
        Exit = "退出",
        SettingsTitle = "晃动鼠标放大指针 · 设置",
        RestoreDefaults = "恢复默认",
        OpenConfigFile = "打开配置文件",
        Close = "关闭",
        MaxScale = "最大尺寸",
        MaxScaleHint = "放大到系统箭头的几倍",
        MinSpeed = "触发速度",
        MinSpeedHint = "每一笔至少要多快；越小越容易触发",
        MinReversals = "起步次数",
        MinReversalsHint = "多少次来回之后开始放大",
        WindowMs = "统计时间窗",
        WindowMsHint = "起步次数要在这段时间内完成",
        MinStroke = "最短一笔",
        MinStrokeHint = "短于这个距离的移动当作手抖",
        GrowPerSwing = "长大速度",
        GrowPerSwingHint = "每甩一次长大多少（占最大尺寸的比例）",
        ReleaseDelay = "停手后保持",
        ReleaseDelayHint = "停下来后先保持多久再缩小",
        Shrink = "缩回时长",
        ShrinkHint = "从最大缩回原样用多久",
        TimesUnit = " 次",
        ScaleUnit = " 倍",
    };

    static readonly Strings TraditionalChinese = new()
    {
        AppName = "晃動滑鼠放大指標",
        Pause = "暫停",
        StartWithWindows = "開機時啟動",
        Settings = "設定…",
        Exit = "結束",
        SettingsTitle = "晃動滑鼠放大指標 · 設定",
        RestoreDefaults = "還原預設值",
        OpenConfigFile = "開啟設定檔",
        Close = "關閉",
        MaxScale = "最大尺寸",
        MaxScaleHint = "放大到系統箭頭的幾倍",
        MinSpeed = "觸發速度",
        MinSpeedHint = "每一下至少要多快；越小越容易觸發",
        MinReversals = "起始次數",
        MinReversalsHint = "來回幾次之後開始放大",
        WindowMs = "統計時間窗",
        WindowMsHint = "起始次數須在這段時間內完成",
        MinStroke = "最短一下",
        MinStrokeHint = "短於這個距離的移動視為手抖",
        GrowPerSwing = "放大速度",
        GrowPerSwingHint = "每甩一次放大多少（占最大尺寸的比例）",
        ReleaseDelay = "停手後保持",
        ReleaseDelayHint = "停下來後先保持多久再縮小",
        Shrink = "縮回時間",
        ShrinkHint = "從最大縮回原狀需要多久",
        TimesUnit = " 次",
        ScaleUnit = " 倍",
    };

    static readonly Strings Japanese = new()
    {
        AppName = "ShakeCursor",
        Pause = "一時停止",
        StartWithWindows = "Windows 起動時に開始",
        Settings = "設定…",
        Exit = "終了",
        SettingsTitle = "ShakeCursor の設定",
        RestoreDefaults = "既定値に戻す",
        OpenConfigFile = "設定ファイルを開く",
        Close = "閉じる",
        MaxScale = "最大サイズ",
        MaxScaleHint = "通常の矢印の何倍まで大きくするか",
        MinSpeed = "反応する速さ",
        MinSpeedHint = "1 回の振りに必要な速さ（小さいほど反応しやすい）",
        MinReversals = "開始までの回数",
        MinReversalsHint = "何往復で拡大を始めるか",
        WindowMs = "判定時間",
        WindowMsHint = "この時間内に上の回数を振る必要があります",
        MinStroke = "最短の振り幅",
        MinStrokeHint = "これより短い動きは手ぶれとして無視します",
        GrowPerSwing = "拡大の速さ",
        GrowPerSwingHint = "1 回振るごとに大きくなる量（最大サイズに対する割合）",
        ReleaseDelay = "停止後の保持",
        ReleaseDelayHint = "振るのをやめてから縮み始めるまでの時間",
        Shrink = "縮む時間",
        ShrinkHint = "最大から元の大きさに戻るまでの時間",
        TimesUnit = " 回",
        ScaleUnit = " 倍",
    };
}
