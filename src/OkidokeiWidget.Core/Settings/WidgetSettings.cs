using System.Text.Json.Serialization;

namespace OkidokeiWidget.Core.Settings;

public sealed class WidgetSettings
{
    /// <summary>
    /// 以前のバージョン (2026-10-01 より前) の設定ファイルにある、アプリ全体で共通の表示設定。
    /// 読み込んだ後、<c>MonitorSettingsReconciler.Reconcile</c> で各モニタへ引き継いでから null にする。
    /// null のときは書き出さない (data-model.md、research.md #22)。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppearanceSettings? Appearance { get; set; }

    /// <summary>
    /// 以前のバージョンの設定ファイルにある、アプリ全体で共通のウィンドウ挙動設定。扱いは <see cref="Appearance"/> と同じ。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WindowBehaviorSettings? WindowBehavior { get; set; }

    public Dictionary<string, MonitorPlacement> Monitors { get; set; } = new();

    public bool AutoStartEnabled { get; set; }

    public static WidgetSettings CreateDefault() => new();
}
