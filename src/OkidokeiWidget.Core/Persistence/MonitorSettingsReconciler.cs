using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Persistence;

/// <summary>
/// 読み込んだ設定の <see cref="WidgetSettings.Monitors"/> と、現在接続中のモニタ構成を突き合わせる。
/// 接続されていないモニタのエントリは削除せず保持し、新規接続モニタにはデフォルト値を補完する
/// (data-model.md の状態遷移、Edge Case: モニタ取り外し時の設定保持)。
/// キーが一致しないモニタ (別の端子につなぎ直したモニタ等) には、同じ型番の使われていないエントリを
/// キーの順に割り当てて今のキーへ移し、割り当てるものがなければデフォルト値を補完する (FR-040、research.md #20)。
/// 以前のバージョンの設定ファイル (ルートに見た目がある) は、付け替えの後、デフォルト値の補完の前に、
/// 各モニタへ引き継ぐ (FR-041、research.md #22)。補完した新しいモニタは非表示で始め、
/// <c>Monitors</c> が空だったとき (初めて起動したときなど) だけ、プライマリモニタを表示する。
/// </summary>
public static class MonitorSettingsReconciler
{
    // モニタの作業領域中央に寄せる際の目安サイズ (実際のウィンドウサイズはフォント設定に依存するため概算)
    private const int AssumedWidgetWidth = 260;
    private const int AssumedWidgetHeight = 100;

    public static void Reconcile(WidgetSettings settings, IReadOnlyList<ConnectedMonitor> connectedMonitors)
    {
        // 空だったかどうかを覚えておく。Monitors のエントリは消さないので、空なのは起動時だけになる (research.md #22)
        var wasEmpty = settings.Monitors.Count == 0;

        // 1: キーが一致するモニタはそのまま使う。同じキーのモニタが複数あっても割り当ては 1 回だけにし、
        // 2 つ目の割り当てで 1 つ目を上書きして設定を失わないようにする (research.md #20)
        var unmatchedMonitors = connectedMonitors
            .Where(monitor => !settings.Monitors.ContainsKey(monitor.Identifier))
            .DistinctBy(monitor => monitor.Identifier)
            .ToList();

        MoveUnusedPlacementsOfSameModel(settings, connectedMonitors, unmatchedMonitors);

        InheritLegacyCommonSettings(settings, connectedMonitors);

        // 3: 割り当てるエントリがなかったモニタには、デフォルト値を補完する。新しいモニタは非表示で始める (FR-041)
        var added = new List<(ConnectedMonitor Monitor, MonitorPlacement Placement)>();
        foreach (var monitor in unmatchedMonitors)
        {
            if (settings.Monitors.ContainsKey(monitor.Identifier))
            {
                continue;
            }

            var placement = new MonitorPlacement
            {
                IsVisible = false,
                X = Math.Max(0, (monitor.WorkAreaWidth - AssumedWidgetWidth) / 2),
                Y = Math.Max(0, (monitor.WorkAreaHeight - AssumedWidgetHeight) / 2),
            };
            settings.Monitors[monitor.Identifier] = placement;
            added.Add((monitor, placement));
        }

        // 初めて起動したとき (Monitors が空だったとき) は、どこにも時計が出ないのを避けるため、
        // プライマリモニタだけを表示する。プライマリがなければ、列挙した先頭のモニタを表示する
        if (wasEmpty && added.Count > 0)
        {
            var target = added.FirstOrDefault(entry => entry.Monitor.IsPrimary);
            (target.Placement ?? added[0].Placement).IsVisible = true;
        }
    }

    /// <summary>
    /// 以前のバージョンの設定ファイル (ルートの <c>Appearance</c>・<c>WindowBehavior</c> がある) なら、
    /// すべてのエントリへその複製を入れ、接続中のどのモニタとも一致しないエントリを非表示にしてから、
    /// ルートの 2 つを null にする。null にするので、引き継ぎは 1 回だけ行われる (FR-041、research.md #22)。
    /// </summary>
    private static void InheritLegacyCommonSettings(WidgetSettings settings, IReadOnlyList<ConnectedMonitor> connectedMonitors)
    {
        if (settings.Appearance is null && settings.WindowBehavior is null)
        {
            return;
        }

        var connectedIdentifiers = connectedMonitors.Select(monitor => monitor.Identifier).ToHashSet();

        foreach (var (identifier, placement) in settings.Monitors)
        {
            if (settings.Appearance is not null)
            {
                placement.Appearance = settings.Appearance.Clone();
            }

            if (settings.WindowBehavior is not null)
            {
                placement.WindowBehavior = settings.WindowBehavior.Clone();
            }

            if (!connectedIdentifiers.Contains(identifier))
            {
                placement.IsVisible = false;
            }
        }

        settings.Appearance = null;
        settings.WindowBehavior = null;
    }

    /// <summary>
    /// 2: キーが一致しなかったモニタを型番ごとに分け、同じ型番のエントリのうち接続中のどのモニタにも
    /// 使われていないものと、キーの順に 1 対 1 で組み合わせる。組み合わせたエントリは、同じインスタンスの
    /// まま今のキーへ移し、古いキーは消す (<c>ClockWindow</c> が同じインスタンスを持っているため複製しない)。
    /// </summary>
    private static void MoveUnusedPlacementsOfSameModel(
        WidgetSettings settings,
        IReadOnlyList<ConnectedMonitor> connectedMonitors,
        IReadOnlyList<ConnectedMonitor> unmatchedMonitors)
    {
        var connectedIdentifiers = connectedMonitors.Select(monitor => monitor.Identifier).ToHashSet();

        var unmatchedByModel = unmatchedMonitors
            .Select(monitor => (Monitor: monitor, Model: MonitorIdentifier.GetModel(monitor.Identifier)))
            .Where(entry => entry.Model is not null)
            .GroupBy(entry => entry.Model!, StringComparer.Ordinal);

        foreach (var group in unmatchedByModel)
        {
            var identifiers = group
                .Select(entry => entry.Monitor.Identifier)
                .OrderBy(identifier => identifier, StringComparer.Ordinal)
                .ToList();

            var unusedKeys = settings.Monitors.Keys
                .Where(key => !connectedIdentifiers.Contains(key)
                    && string.Equals(MonitorIdentifier.GetModel(key), group.Key, StringComparison.Ordinal))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            foreach (var (identifier, oldKey) in identifiers.Zip(unusedKeys))
            {
                settings.Monitors[identifier] = settings.Monitors[oldKey];
                settings.Monitors.Remove(oldKey);
            }
        }
    }
}
