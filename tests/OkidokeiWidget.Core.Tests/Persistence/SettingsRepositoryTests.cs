using OkidokeiWidget.Core.Persistence;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class SettingsRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsPath;

    public SettingsRepositoryTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OkidokeiWidgetTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _settingsPath = Path.Combine(_tempDirectory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_ファイルが存在しない場合はデフォルト値を返し通知フラグは立てない()
    {
        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        // 2026-10-01 から、見た目はモニタごとに持つ。ルートの値は以前のバージョンのファイルにしかない (FR-041)
        Assert.Null(settings.Appearance);
        Assert.Null(settings.WindowBehavior);
        Assert.Empty(settings.Monitors);
    }

    [Fact]
    public void Load_不正なJSONの場合はデフォルト値へフォールバックし通知フラグを立てる()
    {
        File.WriteAllText(_settingsPath, "{ this is not valid json");

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.Null(settings.Appearance);
        Assert.Empty(settings.Monitors);
    }

    [Fact]
    public void Save_BackgroundOpacityを0から100の範囲にクランプして書き込む()
    {
        // 2026-10-01 から、各モニタの値をクランプする (ルートは null になるため。/speckit-analyze の指摘 C1)
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement();
        settings.Monitors["monitor-1"].Appearance.BackgroundOpacity = 150.0;
        settings.Monitors["monitor-2"] = new Core.Settings.MonitorPlacement();
        settings.Monitors["monitor-2"].Appearance.BackgroundOpacity = -5.0;

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(100.0, loaded.Monitors["monitor-1"].Appearance.BackgroundOpacity);
        Assert.Equal(0.0, loaded.Monitors["monitor-2"].Appearance.BackgroundOpacity);
    }

    [Fact]
    public void Save_ルートの見た目が残っていてもクランプして書き込む()
    {
        // 以前のバージョンのファイルを読んで、引き継ぐ前に保存する場合
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Appearance = new Core.Settings.AppearanceSettings { BackgroundOpacity = 150.0 };

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, _) = SettingsRepository.Load(_settingsPath);
        Assert.Equal(100.0, loaded.Appearance!.BackgroundOpacity);
    }

    [Fact]
    public void Save_ルートの見た目がnullなら書き出さず落ちない()
    {
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement();

        SettingsRepository.Save(settings, _settingsPath);

        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(_settingsPath));
        Assert.False(document.RootElement.TryGetProperty("Appearance", out _));
        Assert.False(document.RootElement.TryGetProperty("WindowBehavior", out _));
        Assert.True(document.RootElement.GetProperty("Monitors").GetProperty("monitor-1").TryGetProperty("Appearance", out _));
    }

    [Fact]
    public void Save_モニタごとの見た目とウィンドウ挙動を保存して読み戻せる()
    {
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement();
        settings.Monitors["monitor-1"].Appearance.TimeFontSize = 40.0;
        settings.Monitors["monitor-1"].Appearance.ShowDate = false;
        settings.Monitors["monitor-1"].WindowBehavior.PositionLocked = true;
        settings.Monitors["monitor-2"] = new Core.Settings.MonitorPlacement();
        settings.Monitors["monitor-2"].WindowBehavior.TopMost = false;

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(40.0, loaded.Monitors["monitor-1"].Appearance.TimeFontSize);
        Assert.False(loaded.Monitors["monitor-1"].Appearance.ShowDate);
        Assert.True(loaded.Monitors["monitor-1"].WindowBehavior.PositionLocked);
        Assert.True(loaded.Monitors["monitor-1"].WindowBehavior.TopMost);
        Assert.Equal(24.0, loaded.Monitors["monitor-2"].Appearance.TimeFontSize);
        Assert.False(loaded.Monitors["monitor-2"].WindowBehavior.TopMost);
    }

    [Fact]
    public void Load_以前のバージョンの形のファイルは壊れたファイルとして扱わずルートの値を読む()
    {
        // 2026-10-01 より前の形。ルートに Appearance・WindowBehavior があり、各モニタにはない (FR-041)
        File.WriteAllText(_settingsPath, """
            {
              "Appearance": { "TimeFontSize": 36, "ShowDate": false },
              "WindowBehavior": { "TopMost": false, "PositionLocked": true },
              "Monitors": { "monitor-1": { "IsVisible": true, "X": 10, "Y": 20 } }
            }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        Assert.Equal(36.0, settings.Appearance!.TimeFontSize);
        Assert.False(settings.Appearance.ShowDate);
        Assert.False(settings.WindowBehavior!.TopMost);
        Assert.True(settings.WindowBehavior.PositionLocked);
        // 各モニタの値は、引き継ぐまでは既定値
        Assert.Equal(24.0, settings.Monitors["monitor-1"].Appearance.TimeFontSize);
    }

    [Fact]
    public void Load_アンカーを持たない既存形式の設定は自由配置として読み込む()
    {
        // 2026-09-24 より前の形式。アップデートしても既存ユーザーの位置が変わらないこと (FR-034)
        File.WriteAllText(_settingsPath, """
            {
              "Monitors": {
                "monitor-1": { "IsVisible": true, "X": 1480, "Y": 9 }
              }
            }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        var placement = settings.Monitors["monitor-1"];
        Assert.Null(placement.Anchor);
        Assert.Equal(Core.Settings.AnchorMargin.Narrow, placement.AnchorMargin);
        Assert.Equal(1480, placement.X);
        Assert.Equal(9, placement.Y);
    }

    [Fact]
    public void Load_区切り文字がnullでも壊れたファイルとして扱わずnullのまま読み込む()
    {
        File.WriteAllText(_settingsPath, """
            { "Appearance": { "DateSeparator": null } }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        Assert.Null(settings.Appearance!.DateSeparator);
    }

    [Fact]
    public void Load_モニタごとの区切り文字がnullでも壊れたファイルとして扱わずnullのまま読み込む()
    {
        File.WriteAllText(_settingsPath, """
            { "Monitors": { "monitor-1": { "Appearance": { "DateSeparator": null } } } }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        Assert.Null(settings.Monitors["monitor-1"].Appearance.DateSeparator);
    }

    [Fact]
    public void Load_区切り文字の項目がなければデフォルトになる()
    {
        File.WriteAllText(_settingsPath, """
            { "Appearance": { "ShowDate": true } }
            """);

        var (settings, _) = SettingsRepository.Load(_settingsPath);

        Assert.Equal("/", settings.Appearance!.DateSeparator);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("🍣")]
    public void Save_区切り文字は保存して読み戻しても同じ値になる(string? separator)
    {
        // 🍣 は \uXXXX の形で書き出されるが、読み込めば同じ文字に戻る (contracts/settings-file.md)
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement();
        settings.Monitors["monitor-1"].Appearance.DateSeparator = separator;

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(separator, loaded.Monitors["monitor-1"].Appearance.DateSeparator);
    }

    [Fact]
    public void Save_アンカー指定を保存して読み戻せる()
    {
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement
        {
            Anchor = Core.Settings.AnchorPosition.TopRight,
            AnchorMargin = Core.Settings.AnchorMargin.Wide,
        };

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(Core.Settings.AnchorPosition.TopRight, loaded.Monitors["monitor-1"].Anchor);
        Assert.Equal(Core.Settings.AnchorMargin.Wide, loaded.Monitors["monitor-1"].AnchorMargin);
    }

    [Fact]
    public void Load_未知のアンカー値はデフォルト値へフォールバックし通知フラグを立てる()
    {
        File.WriteAllText(_settingsPath, """
            {
              "Monitors": {
                "monitor-1": { "IsVisible": true, "X": 0, "Y": 0, "Anchor": "Middle" }
              }
            }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.Empty(settings.Monitors);
    }

    [Theory]
    [InlineData("""{ "Monitors": null }""")]
    [InlineData("""{ "Monitors": { "monitor-1": null } }""")]
    [InlineData("""{ "Monitors": { "monitor-1": { "Appearance": null } } }""")]
    [InlineData("""{ "Monitors": { "monitor-1": { "WindowBehavior": null } } }""")]
    public void Load_設定の一部がnullの場合はデフォルト値へフォールバックし通知フラグを立てる(string json)
    {
        // JSON に明示的な null があると初期値 (= new()) が上書きされ、そのまま使うと落ちる (issue #51)。
        // 2026-10-01 から、見た目とウィンドウ挙動は各モニタが持つので、各モニタの値の null も対象にする
        File.WriteAllText(_settingsPath, json);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.Null(settings.Appearance);
        Assert.Null(settings.WindowBehavior);
        Assert.Empty(settings.Monitors);
    }

    [Theory]
    [InlineData("""{ "Appearance": null }""")]
    [InlineData("""{ "WindowBehavior": null }""")]
    public void Load_ルートの見た目やウィンドウ挙動がnullでも壊れたファイルとして扱わない(string json)
    {
        // ルートの値は、以前のバージョンのファイルを読むときにしか使わないため (contracts/settings-file.md)
        File.WriteAllText(_settingsPath, json);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        Assert.Null(settings.Appearance);
        Assert.Null(settings.WindowBehavior);
    }

    [Fact]
    public void BackupBrokenFile_読めなかった設定ファイルを残し後の保存で上書きされない()
    {
        // 起動時と同じ順序: 読み込みに失敗 → 元のファイルを残す → 初期設定を保存 (issue #50)
        const string brokenJson = """{ "Appearance": { "ShowDate": false }""";
        File.WriteAllText(_settingsPath, brokenJson);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        var backupPath = SettingsRepository.BackupBrokenFile(_settingsPath);
        SettingsRepository.Save(settings, _settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.Equal(_settingsPath + ".bak", backupPath);
        Assert.Equal(brokenJson, File.ReadAllText(backupPath!));
    }

    [Fact]
    public void BackupBrokenFile_設定ファイルが存在しない場合は何もせずnullを返す()
    {
        var backupPath = SettingsRepository.BackupBrokenFile(_settingsPath);

        Assert.Null(backupPath);
        Assert.False(File.Exists(_settingsPath + ".bak"));
    }
}
