namespace OkidokeiWidget.Core.Settings;

public sealed class AppearanceSettings
{
    public bool ShowDate { get; set; } = true;

    public bool ShowDayOfWeek { get; set; } = true;

    public bool ShowSeconds { get; set; } = true;

    public string FontFamily { get; set; } = string.Empty;

    public double TimeFontSize { get; set; } = 24.0;

    public string TimeFontColor { get; set; } = "#FFFFFFFF";

    public double DateFontSize { get; set; } = 14.0;

    public string DateFontColor { get; set; } = "#FFFFFFFF";

    // 設定ファイルを手で書き換えると null が入り、そのまま保存し直される (FR-026、data-model.md)
    public string? DateSeparator { get; set; } = "/";

    public DayOfWeekFormat DayOfWeekFormat { get; set; } = DayOfWeekFormat.LongKanji;

    public RelativePosition DateDayOfWeekPosition { get; set; } = RelativePosition.Below;

    public string BackgroundColor { get; set; } = "#FF000000";

    public double BackgroundOpacity { get; set; } = 30.0;

    /// <summary>
    /// 複製を返す。以前のバージョンのアプリ全体で共通の設定を各モニタへ引き継ぐときに、
    /// モニタごとに別のインスタンスにするためのもの (SC-009、research.md #22)。
    /// 項目はすべて値型か文字列なので、浅い複製で足りる。
    /// </summary>
    public AppearanceSettings Clone() => (AppearanceSettings)MemberwiseClone();
}
