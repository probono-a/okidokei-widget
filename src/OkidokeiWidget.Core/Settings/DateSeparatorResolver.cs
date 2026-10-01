namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// <see cref="AppearanceSettings.DateSeparator"/> を表示に使う区切り文字へ解決する (FR-026、data-model.md のバリデーション)。
/// 詳細設定画面の選択肢は `/`・`-`・`.` だが、設定ファイルでは任意の文字列 (空欄を含む) を使える。
/// `null` のときだけデフォルト (`/`) にフォールバックする。
/// </summary>
public static class DateSeparatorResolver
{
    public const string DefaultSeparator = "/";

    public static string Resolve(string? separator) => separator ?? DefaultSeparator;
}
