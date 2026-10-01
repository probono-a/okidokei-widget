using System.Globalization;

namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// 日付を「年{区切り}月{区切り}日」の文字列にする (年は 4 桁、月・日は 2 桁。FR-026)。
/// 区切り文字は書式文字列に埋め込まず、数字とつなぎ合わせる。設定ファイルでは任意の文字列を区切りに
/// 使えるため、埋め込むと `y`・`M`・`d`・`'`・`\` などが書式の記号として解釈されてしまう (research.md #21)。
/// </summary>
public static class DateTextFormatter
{
    public static string Format(DateTime date, string? separator)
    {
        var resolved = DateSeparatorResolver.Resolve(separator);
        var year = date.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = date.Month.ToString("D2", CultureInfo.InvariantCulture);
        var day = date.Day.ToString("D2", CultureInfo.InvariantCulture);

        return string.Concat(year, resolved, month, resolved, day);
    }
}
