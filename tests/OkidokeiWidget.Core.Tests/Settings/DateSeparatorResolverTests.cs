using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class DateSeparatorResolverTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("-")]
    [InlineData(".")]
    [InlineData("")]
    [InlineData("//")]
    [InlineData("🍣")]
    public void Resolve_nullでなければそのまま返す(string separator)
    {
        // 設定ファイルでは選択肢以外の文字列 (空欄を含む) も使える (FR-026)
        Assert.Equal(separator, DateSeparatorResolver.Resolve(separator));
    }

    [Fact]
    public void Resolve_nullはデフォルトを返す()
    {
        Assert.Equal(DateSeparatorResolver.DefaultSeparator, DateSeparatorResolver.Resolve(null));
    }
}
