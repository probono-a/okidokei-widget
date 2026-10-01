using OkidokeiWidget.Core.Persistence;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class AutoStartManagerTests : IDisposable
{
    private readonly string _tempStartupFolder;

    public AutoStartManagerTests()
    {
        _tempStartupFolder = Path.Combine(Path.GetTempPath(), "OkidokeiWidgetStartupTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempStartupFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempStartupFolder))
        {
            Directory.Delete(_tempStartupFolder, recursive: true);
        }
    }

    [Fact]
    public void SetEnabled_trueの場合はスタートアップフォルダにショートカットを作成する()
    {
        AutoStartManager.SetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);

        Assert.True(File.Exists(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk")));
    }

    [Fact]
    public void SetEnabled_falseの場合は既存のショートカットを削除する()
    {
        AutoStartManager.SetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);
        AutoStartManager.SetEnabled(false, startupFolderPath: _tempStartupFolder);

        Assert.False(File.Exists(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk")));
    }

    [Fact]
    public void TrySetEnabled_成功した場合はtrueを返しショートカットを作成する()
    {
        var succeeded = AutoStartManager.TrySetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);

        Assert.True(succeeded);
        Assert.True(File.Exists(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk")));
    }

    [Fact]
    public void TrySetEnabled_ショートカットを保存できない場合は例外を投げずfalseを返す()
    {
        // 同名のフォルダがあるとショートカットを保存できない (書き込み権限がない場合の代わり。issue #18)
        Directory.CreateDirectory(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk"));

        var succeeded = AutoStartManager.TrySetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);

        Assert.False(succeeded);
    }

    [Fact]
    public void TrySetEnabled_スタートアップフォルダを作れない場合は例外を投げずfalseを返す()
    {
        // フォルダの場所に同名のファイルがあると、フォルダを作れない
        var blockedFolder = Path.Combine(_tempStartupFolder, "Startup");
        File.WriteAllText(blockedFolder, string.Empty);

        var succeeded = AutoStartManager.TrySetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: blockedFolder);

        Assert.False(succeeded);
    }
}
