using System.Text.Json;
using System.Text.Json.Serialization;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Persistence;

public static class SettingsRepository
{
    public static string DefaultSettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OkidokeiWidget",
        "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// 設定ファイルを読み込む。ファイル不在時、またはパース不可時は <see cref="WidgetSettings.CreateDefault"/>
    /// を返す (FR-018)。<c>FellBackToDefaults</c> はパース不可でフォールバックした場合のみ true になり、
    /// 単にファイルが存在しないだけの場合は false (ユーザーへの通知要否の判断に使う。contracts/settings-file.md 参照)。
    /// </summary>
    public static (WidgetSettings Settings, bool FellBackToDefaults) Load(string? path = null)
    {
        path ??= DefaultSettingsFilePath;

        if (!File.Exists(path))
        {
            return (WidgetSettings.CreateDefault(), false);
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<WidgetSettings>(json, SerializerOptions);
            return settings is null || HasNullSection(settings) ? (WidgetSettings.CreateDefault(), true) : (settings, false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return (WidgetSettings.CreateDefault(), true);
        }
    }

    /// <summary>
    /// JSON に明示的な <c>null</c> があると初期値 (<c>= new()</c>) が上書きされ、そのまま使うと落ちる
    /// ため、パース不可と同じく壊れたファイルとして扱う (issue #51)。
    /// 2026-10-01 の改訂 (FR-041): 見た目とウィンドウ挙動は各モニタが持つため、各モニタの値の null を見る。
    /// ルートの <c>Appearance</c>・<c>WindowBehavior</c> は、以前のバージョンのファイルを読むときにしか使わないので、
    /// null でも壊れたファイルとは扱わない (contracts/settings-file.md の読み込み契約)。
    /// </summary>
    private static bool HasNullSection(WidgetSettings settings) =>
        settings.Monitors is null
        || settings.Monitors.Values.Any(placement =>
            placement is null || placement.Appearance is null || placement.WindowBehavior is null);

    /// <summary>
    /// 読み込めなかった設定ファイルを <c>settings.json.bak</c> としてコピーして残し、そのパスを返す。
    /// 初期設定で上書きされる前に呼ぶことで、ユーザーが元のファイルを手で直せるようにする (issue #50)。
    /// ファイルが存在しない、またはコピーできなかった場合は null を返す。
    /// </summary>
    public static string? BackupBrokenFile(string? path = null)
    {
        path ??= DefaultSettingsFilePath;
        var backupPath = path + ".bak";

        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            File.Copy(path, backupPath, overwrite: true);
            return backupPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(WidgetSettings settings, string? path = null)
    {
        path ??= DefaultSettingsFilePath;
        // 2026-10-01 からは各モニタの値をクランプする。ルートの値は、以前のバージョンのファイルを
        // 引き継ぐ前だけ残っている (引き継いだ後は null。research.md #22)
        foreach (var placement in settings.Monitors.Values)
        {
            placement.Appearance.BackgroundOpacity = Math.Clamp(placement.Appearance.BackgroundOpacity, 0.0, 100.0);
        }

        if (settings.Appearance is not null)
        {
            settings.Appearance.BackgroundOpacity = Math.Clamp(settings.Appearance.BackgroundOpacity, 0.0, 100.0);
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, SerializerOptions));
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // FR-022: 書き込みに失敗してもクラッシュせず継続する。この変更はこのセッション中は永続化されない
        }
    }
}
