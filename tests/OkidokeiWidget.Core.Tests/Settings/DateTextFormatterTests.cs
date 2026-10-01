using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class DateTextFormatterTests
{
    private static readonly DateTime Date = new(2026, 9, 28);

    [Theory]
    [InlineData("/", "2026/09/28")]
    [InlineData(".", "2026.09.28")]
    [InlineData("-", "2026-09-28")]
    [InlineData("", "20260928")]
    [InlineData("🍣", "2026🍣09🍣28")]
    [InlineData("年", "2026年09年28")]
    [InlineData("--", "2026--09--28")]
    public void Format_区切り文字で年月日をつなぐ(string separator, string expected)
    {
        Assert.Equal(expected, DateTextFormatter.Format(Date, separator));
    }

    [Theory]
    [InlineData("d")]
    [InlineData("M")]
    [InlineData("y")]
    [InlineData("'")]
    [InlineData("\\")]
    [InlineData("%")]
    [InlineData(":")]
    public void Format_日付の書式の記号も区切り文字としてそのまま出す(string separator)
    {
        // 書式文字列に埋め込むと記号として解釈されてしまうため、つなぎ合わせている (research.md #21)
        Assert.Equal($"2026{separator}09{separator}28", DateTextFormatter.Format(Date, separator));
    }

    [Fact]
    public void Format_月と日は1桁でも2桁で出す()
    {
        Assert.Equal("2026/01/05", DateTextFormatter.Format(new DateTime(2026, 1, 5), "/"));
    }

    [Fact]
    public void Format_区切り文字がnullならデフォルトを使う()
    {
        Assert.Equal("2026/09/28", DateTextFormatter.Format(Date, null));
    }
}
