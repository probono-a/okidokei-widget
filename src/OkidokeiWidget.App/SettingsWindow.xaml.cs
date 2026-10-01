using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

public partial class SettingsWindow : Window
{
    private sealed record DayOfWeekFormatOption(DayOfWeekFormat Value, string Label);

    private static readonly DayOfWeekFormatOption[] DayOfWeekFormatOptions =
    [
        new(DayOfWeekFormat.ShortKanjiParen, "(月)"),
        new(DayOfWeekFormat.LongKanji, "月曜日"),
        new(DayOfWeekFormat.ShortEnglish, "Mon."),
        new(DayOfWeekFormat.LongEnglish, "Monday"),
    ];

    private static readonly string[] DateSeparatorOptions = ["/", "-", "."];

    private sealed record DateDayOfWeekPositionOption(RelativePosition Value, string Label);

    private static readonly DateDayOfWeekPositionOption[] DateDayOfWeekPositionOptions =
    [
        new(RelativePosition.Above, "上"),
        new(RelativePosition.Below, "下"),
        new(RelativePosition.Left, "左"),
        new(RelativePosition.Right, "右"),
    ];

    // 「編集するモニター」の選択肢。record にしないのは、値の等しい要素が新しいリストにあると
    // ItemsSource を替えても選択が残り、動きが 2 通りになるため (research.md #23 の前提 1)
    private sealed class MonitorOption(string identifier, string label)
    {
        public string Identifier { get; } = identifier;

        public string Label { get; } = label;
    }

    private readonly WidgetSettings _settings;
    private readonly Action<string> _onAppearanceChanged;
    private readonly Action _onMonitorVisibilityChanged;
    private IReadOnlyList<ConnectedMonitor> _connectedMonitors;
    private bool _isInitializing;

    public SettingsWindow(
        WidgetSettings settings,
        IReadOnlyList<ConnectedMonitor> connectedMonitors,
        string initialIdentifier,
        Action<string> onAppearanceChanged,
        Action onMonitorVisibilityChanged)
    {
        _settings = settings;
        _connectedMonitors = connectedMonitors;
        _onAppearanceChanged = onAppearanceChanged;
        _onMonitorVisibilityChanged = onMonitorVisibilityChanged;
        // InitializeComponent() が Slider/ComboBox の初期値設定時に対応する ValueChanged/
        // SelectionChanged を同期的に発火させるため、_settings 代入・_isInitializing のガードは
        // InitializeComponent() より前に済ませておく必要がある
        _isInitializing = true;

        InitializeComponent();
        WindowThemeHelper.ApplyImmersiveDarkMode(this);

        FontFamilyCombo.ItemsSource = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        DateDayOfWeekPositionCombo.ItemsSource = DateDayOfWeekPositionOptions;
        DateSeparatorCombo.ItemsSource = DateSeparatorOptions;
        DayOfWeekFormatCombo.ItemsSource = DayOfWeekFormatOptions;

        RebuildMonitorOptions(initialIdentifier);

        _isInitializing = false;
    }

    /// <summary>今「編集するモニター」で選んでいるモニタの識別子。選べるモニタがなければ null。</summary>
    private string? SelectedIdentifier => (MonitorCombo.SelectedItem as MonitorOption)?.Identifier;

    /// <summary>選んでいるモニタの設定。接続中のモニタには必ず <c>Monitors</c> のエントリがある (Reconcile)。</summary>
    private MonitorPlacement? SelectedPlacement =>
        SelectedIdentifier is { } identifier && _settings.Monitors.TryGetValue(identifier, out var placement) ? placement : null;

    /// <summary>
    /// 「編集するモニター」の選択肢を接続中のモニタから作り直し、<paramref name="preferredIdentifier"/> のモニタを選ぶ。
    /// そのモニタがなければ、プライマリモニタ (なければ先頭) を選ぶ。作り直す間は変更イベントを無視し、
    /// 最後に選んだモニタの値を全コントロールへ 1 回だけ入れ直す (research.md #23、issue #17)。
    /// </summary>
    private void RebuildMonitorOptions(string? preferredIdentifier)
    {
        var wasInitializing = _isInitializing;
        _isInitializing = true;

        var options = _connectedMonitors
            .OrderBy(monitor => monitor.DisplayNumber)
            .Select(monitor => new MonitorOption(monitor.Identifier, BuildMonitorLabel(monitor)))
            .ToList();
        MonitorCombo.ItemsSource = options;
        MonitorCombo.SelectedItem =
            options.FirstOrDefault(option => option.Identifier == preferredIdentifier)
            ?? options.FirstOrDefault(option => _connectedMonitors.First(m => m.Identifier == option.Identifier).IsPrimary)
            ?? options.FirstOrDefault();

        LoadSelectedMonitor();

        _isInitializing = wasInitializing;
    }

    private string BuildMonitorLabel(ConnectedMonitor monitor)
    {
        // DisplayNumber は Windows の「設定 > システム > ディスプレイ」の識別番号と一致する
        var isHidden = _settings.Monitors.TryGetValue(monitor.Identifier, out var placement) && !placement.IsVisible;
        var notes = new List<string>();
        if (monitor.IsPrimary)
        {
            notes.Add("プライマリ");
        }

        if (isHidden)
        {
            notes.Add("非表示");
        }

        var suffix = notes.Count > 0 ? $" ({string.Join("、", notes)})" : string.Empty;
        return $"モニター {monitor.DisplayNumber}{suffix}";
    }

    /// <summary>
    /// 選んでいるモニタの設定を全コントロールへ入れ直す (research.md #23)。入れ直す間は変更イベントを無視する
    /// (<c>CheckBox.IsChecked</c> などをコードから変えるとイベントが起きるため。前提 2)。
    /// </summary>
    private void LoadSelectedMonitor()
    {
        var wasInitializing = _isInitializing;
        _isInitializing = true;

        if (SelectedPlacement is { } placement)
        {
            var appearance = placement.Appearance;

            MonitorVisibleCheckBox.IsChecked = placement.IsVisible;

            // 設定のフォント名が選択肢にない (既定値の "" を含む) ときは null を入れて空欄にする。
            // 選択肢にない値を入れても前の選択が残り、前のモニタのフォントが見えてしまうため (research.md #21 の前提 2)。
            // 空欄のまま閉じたり、ほかの項目を変えたりしても、フォント名は書き換えない
            var fontFamilies = (IEnumerable<string>)FontFamilyCombo.ItemsSource;
            FontFamilyCombo.SelectedItem = fontFamilies.Contains(appearance.FontFamily) ? appearance.FontFamily : null;

            TimeFontSizeSlider.Value = appearance.TimeFontSize;
            DateFontSizeSlider.Value = appearance.DateFontSize;
            OpacitySlider.Value = appearance.BackgroundOpacity;

            ShowDateCheckBox.IsChecked = appearance.ShowDate;
            ShowDayOfWeekCheckBox.IsChecked = appearance.ShowDayOfWeek;
            ShowSecondsCheckBox.IsChecked = appearance.ShowSeconds;

            DateDayOfWeekPositionCombo.SelectedItem = DateDayOfWeekPositionOptions.First(o => o.Value == appearance.DateDayOfWeekPosition);

            // 設定ファイルで選択肢以外の文字を指定していたら、空欄にする (FR-026)。
            // 選択肢にない値を SelectedItem に入れても空欄にならず前の選択が残るため、null を入れる (research.md #21)
            var dateSeparator = DateSeparatorResolver.Resolve(appearance.DateSeparator);
            DateSeparatorCombo.SelectedItem = DateSeparatorOptions.Contains(dateSeparator) ? dateSeparator : null;

            DayOfWeekFormatCombo.SelectedItem = DayOfWeekFormatOptions.First(o => o.Value == appearance.DayOfWeekFormat);

            UpdateColorButtonLabel(TimeFontColorButton, "文字色を選択", appearance.TimeFontColor);
            UpdateColorButtonLabel(DateFontColorButton, "文字色を選択", appearance.DateFontColor);
            UpdateColorButtonLabel(BackgroundColorButton, "背景色を選択", appearance.BackgroundColor);
        }

        _isInitializing = wasInitializing;
    }

    /// <summary>
    /// 編集するモニタを切り替える。ウィジェットの右クリックメニューから「詳細設定」を開き直したときに、
    /// そのウィジェットのモニタへ切り替えるために App から呼ばれる (FR-042)。
    /// </summary>
    public void SelectMonitor(string identifier)
    {
        if (MonitorCombo.ItemsSource is not IEnumerable<MonitorOption> options)
        {
            return;
        }

        var option = options.FirstOrDefault(o => o.Identifier == identifier);
        if (option is null)
        {
            return;
        }

        // SelectionChanged から LoadSelectedMonitor が呼ばれて二重に読み込まないよう、フラグを立てて変え、最後に 1 回だけ読み込む
        var wasInitializing = _isInitializing;
        _isInitializing = true;
        MonitorCombo.SelectedItem = option;
        _isInitializing = wasInitializing;

        LoadSelectedMonitor();
    }

    /// <summary>
    /// 接続中のモニタが変わったとき (稼働中の抜き差し) に、「編集するモニター」の選択肢を作り直す (issue #17、FR-042)。
    /// 選んでいたモニタがつながっていれば、そのまま選び直す。なければ、プライマリモニタの編集に切り替わる。
    /// </summary>
    public void UpdateConnectedMonitors(IReadOnlyList<ConnectedMonitor> connectedMonitors)
    {
        _connectedMonitors = connectedMonitors;
        RebuildMonitorOptions(SelectedIdentifier);
    }

    private void MonitorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        LoadSelectedMonitor();
    }

    private void MonitorVisibleCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // LoadSelectedMonitor が IsChecked を入れると Checked/Unchecked が起きる。読み込みの途中で
        // 選択肢を作り直してフラグが下りてしまうことがないよう、ここでも初期化中は何もしない
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        // FR-014: モニタごとの表示/非表示は、選んだモニタの「このモニターに表示する」で切り替える
        placement.IsVisible = MonitorVisibleCheckBox.IsChecked == true;
        _onMonitorVisibilityChanged();

        // 選択肢の「(非表示)」の表記を直す
        RebuildMonitorOptions(SelectedIdentifier);
    }

    private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.FontFamily = FontFamilyCombo.SelectedItem as string ?? string.Empty;
        NotifyAppearanceChanged();
    }

    private void TimeFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.TimeFontSize = TimeFontSizeSlider.Value;
        NotifyAppearanceChanged();
    }

    private void DateFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.DateFontSize = DateFontSizeSlider.Value;
        NotifyAppearanceChanged();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.BackgroundOpacity = OpacitySlider.Value;
        NotifyAppearanceChanged();
    }

    private void ShowDateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.ShowDate = ShowDateCheckBox.IsChecked == true;
        NotifyAppearanceChanged();
    }

    private void ShowDayOfWeekCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.ShowDayOfWeek = ShowDayOfWeekCheckBox.IsChecked == true;
        NotifyAppearanceChanged();
    }

    private void ShowSecondsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.ShowSeconds = ShowSecondsCheckBox.IsChecked == true;
        NotifyAppearanceChanged();
    }

    private void DateDayOfWeekPositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        if (DateDayOfWeekPositionCombo.SelectedItem is DateDayOfWeekPositionOption option)
        {
            placement.Appearance.DateDayOfWeekPosition = option.Value;
            NotifyAppearanceChanged();
        }
    }

    private void DateSeparatorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        placement.Appearance.DateSeparator = DateSeparatorCombo.SelectedItem as string ?? DateSeparatorResolver.DefaultSeparator;
        NotifyAppearanceChanged();
    }

    private void DayOfWeekFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || SelectedPlacement is not { } placement)
        {
            return;
        }

        if (DayOfWeekFormatCombo.SelectedItem is DayOfWeekFormatOption option)
        {
            placement.Appearance.DayOfWeekFormat = option.Value;
            NotifyAppearanceChanged();
        }
    }

    private void TimeFontColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPlacement is not { } placement)
        {
            return;
        }

        PickColor(TimeFontColorButton, "文字色を選択", placement.Appearance.TimeFontColor, hex =>
        {
            placement.Appearance.TimeFontColor = hex;
        });
    }

    private void DateFontColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPlacement is not { } placement)
        {
            return;
        }

        PickColor(DateFontColorButton, "文字色を選択", placement.Appearance.DateFontColor, hex =>
        {
            placement.Appearance.DateFontColor = hex;
        });
    }

    private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPlacement is not { } placement)
        {
            return;
        }

        PickColor(BackgroundColorButton, "背景色を選択", placement.Appearance.BackgroundColor, hex =>
        {
            placement.Appearance.BackgroundColor = hex;
        });
    }

    /// <summary>
    /// 色の選択画面を開き、選んだ色を適用する。書き込み先は、開く前に選んでいたモニタの設定にする
    /// (開いている間にモニタが抜かれて選択が変わっても、別のモニタへ書き込まないため。
    /// <c>/speckit-analyze</c> の指摘 L3)。
    /// </summary>
    private void PickColor(Button button, string label, string currentColor, Action<string> applyHex)
    {
        var identifier = SelectedIdentifier;
        var picker = new ColorPickerWindow(currentColor) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedHex is not { } hex || identifier is null)
        {
            return;
        }

        applyHex(hex);

        // ボタンの表示は、開いたモニタを今も選んでいるときだけ直す
        if (SelectedIdentifier == identifier)
        {
            UpdateColorButtonLabel(button, label, hex);
        }

        _onAppearanceChanged(identifier);
    }

    private void NotifyAppearanceChanged()
    {
        if (SelectedIdentifier is { } identifier)
        {
            _onAppearanceChanged(identifier);
        }
    }

    private static void UpdateColorButtonLabel(Button button, string label, string color)
    {
        button.Content = $"{label}…({ColorHexResolver.Resolve(color)})";
    }
}
