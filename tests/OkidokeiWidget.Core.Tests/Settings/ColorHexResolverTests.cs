using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class ColorHexResolverTests
{
    [Theory]
    [InlineData("#FFFFFFFF")]
    [InlineData("#80112233")]
    public void Resolve_有効な16進ARGB文字列はそのまま返す(string hex)
    {
        Assert.Equal(hex, ColorHexResolver.Resolve(hex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#FFFFFF")]
    [InlineData("#GGFFFFFF")]
    public void Resolve_パース不可な文字列はデフォルト色を返す(string hex)
    {
        Assert.Equal(ColorHexResolver.DefaultColor, ColorHexResolver.Resolve(hex));
    }

    [Theory]
    [InlineData("#FFFFFFFF", "#FFFFFF")]
    [InlineData("#80112233", "#112233")]
    public void ToRgbHex_有効な16進ARGB文字列はアルファ成分を除いた6桁を返す(string hex, string expected)
    {
        Assert.Equal(expected, ColorHexResolver.ToRgbHex(hex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#FFFFFF")]
    public void ToRgbHex_パース不可な文字列はデフォルト色の6桁を返す(string hex)
    {
        Assert.Equal("#FFFFFF", ColorHexResolver.ToRgbHex(hex));
    }
}
