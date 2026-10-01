using OkidokeiWidget.Core.Monitors;

namespace OkidokeiWidget.Core.Tests.Monitors;

public class MonitorIdentifierTests
{
    [Fact]
    public void GetModel_実際の形のIDから型番を取り出す()
    {
        var model = MonitorIdentifier.GetModel(@"\\?\DISPLAY#SNYAE04#5&3b7d6ecd&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}");

        Assert.Equal("SNYAE04", model);
    }

    [Fact]
    public void GetModel_IDが取れなかったときのアダプター名はnull()
    {
        Assert.Null(MonitorIdentifier.GetModel(@"\\.\DISPLAY1"));
    }

    [Fact]
    public void GetModel_2つ目が空のIDはnull()
    {
        Assert.Null(MonitorIdentifier.GetModel(@"\\?\DISPLAY##5&3b7d6ecd&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"));
    }

    [Fact]
    public void GetModel_2つにしか区切れないIDはnull()
    {
        Assert.Null(MonitorIdentifier.GetModel(@"\\?\DISPLAY#SNYAE04"));
    }

    [Fact]
    public void GetModel_末尾がシャープで終わるIDは3つに区切れるので型番を返す()
    {
        Assert.Equal("SNYAE04", MonitorIdentifier.GetModel(@"\\?\DISPLAY#SNYAE04#"));
    }
}
