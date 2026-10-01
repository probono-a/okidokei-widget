namespace OkidokeiWidget.Core.Settings;

public sealed class WindowBehaviorSettings
{
    public bool TopMost { get; set; } = true;

    public bool PositionLocked { get; set; }

    /// <summary>
    /// 複製を返す。以前のバージョンのアプリ全体で共通の設定を各モニタへ引き継ぐときに、
    /// モニタごとに別のインスタンスにするためのもの (SC-009、research.md #22)。
    /// </summary>
    public WindowBehaviorSettings Clone() => (WindowBehaviorSettings)MemberwiseClone();
}
