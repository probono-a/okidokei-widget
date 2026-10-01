using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Persistence;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "OkidokeiWidget.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private readonly Dictionary<string, ClockWindow> _clockWindowsByMonitor = new();
    private IReadOnlyList<ConnectedMonitor> _connectedMonitors = Array.Empty<ConnectedMonitor>();
    private WidgetSettings _settings = null!;
    private SettingsWindow? _settingsWindow;
    private TrayIconManager? _trayIconManager;
    private DisplayChangeNotifier? _displayChangeNotifier;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 二重起動時、2 つ目のプロセスは UI を作らずそのまま終了する (research.md #6)
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        // 個々の ClockWindow の表示/非表示に関わらずアプリを常駐させ続けるため、
        // 終了は右クリックメニューの「終了」から明示的にのみ行う (FR-023)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var (settings, fellBackToDefaults) = SettingsRepository.Load();
        _settings = settings;

        // 読み込めなかった設定ファイルは、下の保存で初期設定に上書きされる前に別名で残す (issue #50)
        var backupPath = fellBackToDefaults ? SettingsRepository.BackupBrokenFile() : null;

        RefreshConnectedMonitors();
        SettingsRepository.Save(settings);

        // Startup フォルダのショートカット作成/削除は、タスクトレイのメニューで「自動起動」を
        // 切り替えたとき (OnAutoStartChanged) のみ行う。起動のたびにここで書き換えると、Release exe
        // 以外の方法 (dotnet run 等) で起動した際にショートカットの対象が意図せず
        // 上書きされてしまうため (issue #20)
        SyncClockWindows();

        if (fellBackToDefaults)
        {
            var message = "設定ファイルの読み込みに失敗したため、デフォルト設定で起動しました。";
            if (backupPath is not null)
            {
                message += $"\n\n元の設定ファイルは次の場所に残してあります。\n{backupPath}";
            }

            MessageBox.Show(
                message,
                "OkidokeiWidget",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        // タスクトレイの常駐アイコン: クリックで最前面表示が無効でもウィジェットを前面に呼び戻し、
        // 右クリックで「詳細設定」「自動起動」「終了」のメニューを出す (FR-031, FR-032, FR-038)
        _trayIconManager = new TrayIconManager(BuildTrayContextMenu);
        _trayIconManager.ActivateAllRequested += ActivateAllClockWindows;

        // 稼働中のモニタ接続/取り外しを検知し、表示を追随させる (T044、Edge Case: 稼働中の
        // モニタ切断時は自動非表示、設定は保持)
        _displayChangeNotifier = new DisplayChangeNotifier();
        _displayChangeNotifier.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void RefreshConnectedMonitors()
    {
        _connectedMonitors = MonitorEnumerationService.GetConnectedMonitors();
        MonitorSettingsReconciler.Reconcile(_settings, _connectedMonitors);
    }

    private void OnDisplaySettingsChanged()
    {
        RefreshConnectedMonitors();
        SyncClockWindows();
        SettingsRepository.Save(_settings);

        // 詳細設定画面を開いたままモニタを抜き差ししたら、「編集するモニター」の選択肢も追随させる (issue #17)
        _settingsWindow?.UpdateConnectedMonitors(_connectedMonitors);
    }

    /// <summary>
    /// 現在接続中のモニタ・<see cref="WidgetSettings.Monitors"/> の表示/非表示設定と、実際に
    /// 開いている <see cref="ClockWindow"/> の集合が一致するように生成/破棄する (FR-014, T033, T045)。
    /// </summary>
    private void SyncClockWindows()
    {
        var currentIds = new HashSet<string>(_connectedMonitors.Select(m => m.Identifier));

        // 稼働中に取り外されたモニタのウィンドウを閉じる。Monitors エントリ自体は保持する
        foreach (var identifier in _clockWindowsByMonitor.Keys.Where(id => !currentIds.Contains(id)).ToList())
        {
            _clockWindowsByMonitor[identifier].Close();
            _clockWindowsByMonitor.Remove(identifier);
        }

        foreach (var monitor in _connectedMonitors)
        {
            var isVisible = _settings.Monitors.TryGetValue(monitor.Identifier, out var placement) && placement.IsVisible;
            var hasWindow = _clockWindowsByMonitor.TryGetValue(monitor.Identifier, out var window);

            if (isVisible && !hasWindow)
            {
                CreateClockWindow(monitor, _settings.Monitors[monitor.Identifier]);
            }
            else if (!isVisible && hasWindow)
            {
                window!.Close();
                _clockWindowsByMonitor.Remove(monitor.Identifier);
            }
            else if (isVisible)
            {
                // モニタの取り外し・再接続・解像度変更で作業領域の原点が動くため、表示中の
                // ウィンドウにも最新のモニタ情報を渡し、保存された位置へ配置し直す (issue #10)
                window!.UpdateMonitor(monitor);
            }
        }
    }

    private void CreateClockWindow(ConnectedMonitor monitor, MonitorPlacement placement)
    {
        var window = new ClockWindow(_settings, monitor, placement, OpenSettingsWindow, OnDisplaySettingsChanged);
        _clockWindowsByMonitor[monitor.Identifier] = window;
        window.Show();
    }

    /// <summary>
    /// タスクトレイの右クリックメニューを組み立てる。項目は「詳細設定」「自動起動」「終了」だけで、
    /// 位置ロック・最前面表示・配置はモニタごとの設定のため、ウィジェット本体のメニューで切り替える
    /// (FR-038、contracts/context-menus.md、research.md #24)。
    /// </summary>
    private ContextMenu BuildTrayContextMenu()
    {
        var menu = new ContextMenu();

        // タスクトレイにはモニタがないため、詳細設定画面はプライマリモニタを選んだ状態で開く (FR-042)
        var settingsItem = new MenuItem { Header = "詳細設定..." };
        settingsItem.Click += (_, _) => OpenSettingsWindow(null);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        // Fluent テーマは IsCheckable が true の項目にしかチェックを描かない (issue #34)。
        // メニューは開くたびに作り直すので、クリックで IsChecked が反転しても表示と食い違わない。
        // 切り替えに失敗して値が戻っても、次に開いたときのチェックは正しい (OnAutoStartChanged)
        var autoStartItem = new MenuItem { Header = "自動起動", IsCheckable = true, IsChecked = _settings.AutoStartEnabled };
        autoStartItem.Click += (_, _) =>
        {
            _settings.AutoStartEnabled = !_settings.AutoStartEnabled;
            OnAutoStartChanged();
        };
        menu.Items.Add(autoStartItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "終了" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ActivateAllClockWindows()
    {
        foreach (var window in _clockWindowsByMonitor.Values)
        {
            // Topmost を一時的に true にしてから元の値へ戻すことで、「最前面表示」が
            // OFF の場合でも確実に他のウィンドウより前面へ引き上げる
            var originalTopMost = window.Topmost;
            window.Topmost = true;
            window.Activate();
            window.Topmost = originalTopMost;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _displayChangeNotifier?.Dispose();
        _trayIconManager?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// 詳細設定画面を開く。<paramref name="identifier"/> は編集するモニタで、ウィジェットのメニューからは
    /// そのウィジェットのモニタを、タスクトレイからは null を渡す (FR-042)。
    /// 開いていなければ、そのモニタ (null ならプライマリ) を選んだ状態で開く。
    /// すでに開いていれば、モニタが渡されたときだけ編集するモニタを切り替えて前に出す。
    /// </summary>
    private void OpenSettingsWindow(string? identifier)
    {
        if (_settingsWindow is not null)
        {
            if (identifier is not null)
            {
                _settingsWindow.SelectMonitor(identifier);
            }

            _settingsWindow.Activate();
            return;
        }

        var initialIdentifier = identifier
            ?? (_connectedMonitors.FirstOrDefault(m => m.IsPrimary) ?? _connectedMonitors.FirstOrDefault())?.Identifier
            ?? string.Empty;
        _settingsWindow = new SettingsWindow(_settings, _connectedMonitors, initialIdentifier, OnAppearanceChanged, OnMonitorVisibilityChanged);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    /// <summary>
    /// 見た目が変わったモニタのウィジェットにだけ反映して保存する (FR-041)。
    /// 非表示のモニタにはウィジェットがないので、保存だけ行う。
    /// </summary>
    private void OnAppearanceChanged(string identifier)
    {
        if (_clockWindowsByMonitor.TryGetValue(identifier, out var window))
        {
            window.ApplyAppearance();
        }

        SettingsRepository.Save(_settings);
    }

    private void OnMonitorVisibilityChanged()
    {
        SyncClockWindows();
        SettingsRepository.Save(_settings);
    }

    private void OnAutoStartChanged()
    {
        // Startup フォルダへの書き込み失敗等で切り替えられなくても落とさない (issue #18)。
        // 設定値を切り替え前に戻す。タスクトレイのメニューは開くたびに作り直すので、
        // チェックを合わせ直す処理は要らない
        if (!AutoStartManager.TrySetEnabled(_settings.AutoStartEnabled))
        {
            _settings.AutoStartEnabled = !_settings.AutoStartEnabled;
            MessageBox.Show(
                "自動起動の設定を変更できませんでした。",
                "OkidokeiWidget",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SettingsRepository.Save(_settings);
    }
}
