using System.Globalization;

namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// <see cref="AppearanceSettings.FontColor"/> (<c>#AARRGGBB</c> 形式) がパース不可の場合に
/// デフォルト色へフォールバックする (data-model.md)。WPF に依存せず文字列レベルで検証するため
/// <see cref="OkidokeiWidget.Core"/> に置く。
/// </summary>
public static class ColorHexResolver
{
    public const string DefaultColor = "#FFFFFFFF";

    public static string Resolve(string fontColor) => IsValidArgbHex(fontColor) ? fontColor : DefaultColor;

    /// <summary>
    /// 画面に出すための <c>#RRGGBB</c> 形式を返す。色の選択画面では透明度を扱わないため、
    /// 詳細設定のボタンと色の選択画面のどちらも、先頭のアルファ成分を除いて表示する (issue #76)。
    /// </summary>
    public static string ToRgbHex(string color) => "#" + Resolve(color).Substring(3);

    private static bool IsValidArgbHex(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != 9 || hex[0] != '#')
        {
            return false;
        }

        return byte.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            && byte.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            && byte.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)
            && byte.TryParse(hex.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
