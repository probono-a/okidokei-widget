namespace OkidokeiWidget.Core.Settings;

public sealed class MonitorPlacement
{
    public bool IsVisible { get; set; } = true;

    public int X { get; set; }

    public int Y { get; set; }

    // null は自由配置 (X/Y を使う)。既存の settings.json にはこのフィールドがないため、
    // 読み込むと null になり、アップデートしても表示位置は変わらない (FR-034)
    public AnchorPosition? Anchor { get; set; }

    public AnchorMargin AnchorMargin { get; set; } = AnchorMargin.Narrow;

    // このモニタの見た目 (FR-041)。以前のバージョンの設定ファイルにはないため、読み込むと既定値になる。
    // 以前のバージョンの共通の見た目は、MonitorSettingsReconciler.Reconcile が各モニタへ引き継ぐ (research.md #22)
    public AppearanceSettings Appearance { get; set; } = new();

    // このモニタの位置ロック・最前面表示 (FR-041)。扱いは Appearance と同じ
    public WindowBehaviorSettings WindowBehavior { get; set; } = new();
}
