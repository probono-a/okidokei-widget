using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Persistence;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class MonitorSettingsReconcilerTests
{
    [Fact]
    public void Reconcile_現在接続されていないモニタのエントリは削除せず保持する()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["DISCONNECTED-MONITOR"] = new MonitorPlacement { IsVisible = false, X = 10, Y = 20 };

        MonitorSettingsReconciler.Reconcile(settings, connectedMonitors: []);

        Assert.True(settings.Monitors.ContainsKey("DISCONNECTED-MONITOR"));
        Assert.Equal(10, settings.Monitors["DISCONNECTED-MONITOR"].X);
    }

    [Fact]
    public void Reconcile_初めて起動したときはプライマリモニタにデフォルト値のエントリを補完して表示する()
    {
        var settings = WidgetSettings.CreateDefault();
        var newMonitor = new ConnectedMonitor("NEW-MONITOR", IsPrimary: true, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

        MonitorSettingsReconciler.Reconcile(settings, [newMonitor]);

        Assert.True(settings.Monitors.TryGetValue("NEW-MONITOR", out var placement));
        Assert.True(placement!.IsVisible);
    }

    [Fact]
    public void Reconcile_既存エントリのあるモニタは上書きしない()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["EXISTING-MONITOR"] = new MonitorPlacement { IsVisible = false, X = 500, Y = 600 };
        var existingMonitor = new ConnectedMonitor("EXISTING-MONITOR", IsPrimary: true, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

        MonitorSettingsReconciler.Reconcile(settings, [existingMonitor]);

        var placement = settings.Monitors["EXISTING-MONITOR"];
        Assert.False(placement.IsVisible);
        Assert.Equal(500, placement.X);
    }

    // 以下は FR-040・research.md #20 の割り当ての場合分け (tasks.md T118)

    private static string Id(string model, int uid) => $@"\\?\DISPLAY#{model}#5&3b7d6ecd&0&UID{uid}#{{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}}";

    private static ConnectedMonitor Connected(string identifier) =>
        new(identifier, IsPrimary: false, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

    private static MonitorPlacement Placement(int x) => new() { IsVisible = true, X = x, Y = 20 };

    [Fact]
    public void Reconcile_型番の違うモニタを別の端子につなぎ直すと前の設定が同じインスタンスのまま移り古いキーは消える()
    {
        var settings = WidgetSettings.CreateDefault();
        var original = new MonitorPlacement { IsVisible = false, X = 300, Y = 400, Anchor = AnchorPosition.BottomRight, AnchorMargin = AnchorMargin.Wide };
        settings.Monitors[Id("DELF16C", 4353)] = original;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4355))]);

        Assert.Same(original, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.False(settings.Monitors.ContainsKey(Id("DELF16C", 4353)));
        Assert.Single(settings.Monitors);
    }

    [Fact]
    public void Reconcile_型番の違う2台の端子を入れ替えるとそれぞれの設定が入れ替わった先へ移る()
    {
        var settings = WidgetSettings.CreateDefault();
        var sony = Placement(100);
        var dell = Placement(200);
        settings.Monitors[Id("SNYAE04", 4352)] = sony;
        settings.Monitors[Id("DELF16C", 4353)] = dell;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("SNYAE04", 4353)), Connected(Id("DELF16C", 4352))]);

        Assert.Same(sony, settings.Monitors[Id("SNYAE04", 4353)]);
        Assert.Same(dell, settings.Monitors[Id("DELF16C", 4352)]);
        Assert.Equal(2, settings.Monitors.Count);
    }

    [Fact]
    public void Reconcile_同じ型番の2台が端子ごとに設定を持っていればどちらもそのまま使う()
    {
        var settings = WidgetSettings.CreateDefault();
        var left = Placement(100);
        var right = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = left;
        settings.Monitors[Id("DELF16C", 4353)] = right;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4353)), Connected(Id("DELF16C", 4352))]);

        Assert.Same(left, settings.Monitors[Id("DELF16C", 4352)]);
        Assert.Same(right, settings.Monitors[Id("DELF16C", 4353)]);
        Assert.Equal(2, settings.Monitors.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reconcile_同じ型番がもう1台増えると同じ端子の方は前の設定でもう1台はデフォルト値(bool newMonitorFirst)
    {
        var settings = WidgetSettings.CreateDefault();
        var original = Placement(123);
        settings.Monitors[Id("DELF16C", 4353)] = original;
        var existing = Connected(Id("DELF16C", 4353));
        var added = Connected(Id("DELF16C", 4352));

        MonitorSettingsReconciler.Reconcile(settings, newMonitorFirst ? [added, existing] : [existing, added]);

        Assert.Same(original, settings.Monitors[Id("DELF16C", 4353)]);
        Assert.NotSame(original, settings.Monitors[Id("DELF16C", 4352)]);
        Assert.NotEqual(123, settings.Monitors[Id("DELF16C", 4352)].X);
    }

    [Fact]
    public void Reconcile_1台だった型番を別の端子へ移し同時にもう1台つなぐとキーの順で先のモニタが前の設定を使う()
    {
        var settings = WidgetSettings.CreateDefault();
        var original = Placement(123);
        settings.Monitors[Id("DELF16C", 4353)] = original;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4356)), Connected(Id("DELF16C", 4355))]);

        Assert.Same(original, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.NotSame(original, settings.Monitors[Id("DELF16C", 4356)]);
        Assert.False(settings.Monitors.ContainsKey(Id("DELF16C", 4353)));
    }

    [Fact]
    public void Reconcile_同じ型番の1台を外し残りを外した方の端子へつなぐとその端子の設定を使う()
    {
        var settings = WidgetSettings.CreateDefault();
        var first = Placement(100);
        var second = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = first;
        settings.Monitors[Id("DELF16C", 4353)] = second;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4353))]);

        Assert.Same(second, settings.Monitors[Id("DELF16C", 4353)]);
        Assert.Same(first, settings.Monitors[Id("DELF16C", 4352)]);
    }

    [Fact]
    public void Reconcile_同じ型番の設定が2つあり1台だけが設定のない端子につながるとキーの順で先の設定が移りもう1つは残る()
    {
        var settings = WidgetSettings.CreateDefault();
        var first = Placement(100);
        var second = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = first;
        settings.Monitors[Id("DELF16C", 4353)] = second;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4355))]);

        Assert.Same(first, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.Same(second, settings.Monitors[Id("DELF16C", 4353)]);
        Assert.False(settings.Monitors.ContainsKey(Id("DELF16C", 4352)));
    }

    [Fact]
    public void Reconcile_同じ型番の2台を2台とも別の端子につなぎ直すと設定がキーの順に1つずつ移りデフォルト値は足されない()
    {
        var settings = WidgetSettings.CreateDefault();
        var first = Placement(100);
        var second = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = first;
        settings.Monitors[Id("DELF16C", 4353)] = second;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4356)), Connected(Id("DELF16C", 4355))]);

        Assert.Same(first, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.Same(second, settings.Monitors[Id("DELF16C", 4356)]);
        Assert.Equal(2, settings.Monitors.Count);
    }

    [Fact]
    public void Reconcile_型番の違うモニタの設定は使わない()
    {
        var settings = WidgetSettings.CreateDefault();
        var sony = Placement(100);
        settings.Monitors[Id("SNYAE04", 4352)] = sony;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4355))]);

        Assert.NotSame(sony, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.Same(sony, settings.Monitors[Id("SNYAE04", 4352)]);
    }

    [Fact]
    public void Reconcile_接続中のモニタが使っている設定は横取りしない()
    {
        var settings = WidgetSettings.CreateDefault();
        var used = Placement(100);
        var unused = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = used;
        settings.Monitors[Id("DELF16C", 4353)] = unused;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4352)), Connected(Id("DELF16C", 4351))]);

        Assert.Same(used, settings.Monitors[Id("DELF16C", 4352)]);
        Assert.Same(unused, settings.Monitors[Id("DELF16C", 4351)]);
        Assert.False(settings.Monitors.ContainsKey(Id("DELF16C", 4353)));
    }

    [Fact]
    public void Reconcile_接続中の2台が同じキーでも割り当ては1回だけで設定を失わない()
    {
        var settings = WidgetSettings.CreateDefault();
        var first = Placement(100);
        var second = Placement(200);
        settings.Monitors[Id("DELF16C", 4352)] = first;
        settings.Monitors[Id("DELF16C", 4353)] = second;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(Id("DELF16C", 4355)), Connected(Id("DELF16C", 4355))]);

        Assert.Same(first, settings.Monitors[Id("DELF16C", 4355)]);
        Assert.Same(second, settings.Monitors[Id("DELF16C", 4353)]);
        Assert.Equal(2, settings.Monitors.Count);
    }

    [Fact]
    public void Reconcile_型番が取れないIDは完全一致だけで他の設定を使わない()
    {
        var settings = WidgetSettings.CreateDefault();
        var other = Placement(100);
        settings.Monitors[@"\\.\DISPLAY2"] = other;

        MonitorSettingsReconciler.Reconcile(settings, [Connected(@"\\.\DISPLAY1")]);

        Assert.NotSame(other, settings.Monitors[@"\\.\DISPLAY1"]);
        Assert.Same(other, settings.Monitors[@"\\.\DISPLAY2"]);
    }

    // 以下は FR-041・research.md #22 の、新しいモニタの扱いと以前のバージョンの設定の引き継ぎ (tasks.md T138)

    private static ConnectedMonitor ConnectedPrimary(string identifier, bool isPrimary) =>
        new(identifier, IsPrimary: isPrimary, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

    [Fact]
    public void Reconcile_保存済みの設定があるときに新しいモニタがつながると非表示で既定値のエントリを足す()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["EXISTING"] = Placement(100);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("EXISTING", true), ConnectedPrimary("NEW", false)]);

        var added = settings.Monitors["NEW"];
        Assert.False(added.IsVisible);
        Assert.True(added.WindowBehavior.TopMost);
        Assert.False(added.WindowBehavior.PositionLocked);
        Assert.Equal(24.0, added.Appearance.TimeFontSize);
        Assert.True(settings.Monitors["EXISTING"].IsVisible);
    }

    [Fact]
    public void Reconcile_保存済みの設定があるときはつながったのがプライマリでも新しいモニタは非表示のまま()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["EXISTING"] = Placement(100);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("NEW-PRIMARY", true)]);

        Assert.False(settings.Monitors["NEW-PRIMARY"].IsVisible);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reconcile_初めて起動したときはプライマリモニタだけを表示する(bool primaryFirst)
    {
        var settings = WidgetSettings.CreateDefault();
        var primary = ConnectedPrimary("PRIMARY", true);
        var secondary = ConnectedPrimary("SECONDARY", false);

        MonitorSettingsReconciler.Reconcile(settings, primaryFirst ? [primary, secondary] : [secondary, primary]);

        Assert.True(settings.Monitors["PRIMARY"].IsVisible);
        Assert.False(settings.Monitors["SECONDARY"].IsVisible);
    }

    [Fact]
    public void Reconcile_初めて起動したときにプライマリがなければ先頭のモニタを表示する()
    {
        var settings = WidgetSettings.CreateDefault();

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("A", false), ConnectedPrimary("B", false)]);

        Assert.True(settings.Monitors["A"].IsVisible);
        Assert.False(settings.Monitors["B"].IsVisible);
    }

    [Fact]
    public void Reconcile_2回目の呼び出しでは新しいモニタを表示しない()
    {
        // Monitors のエントリは消さないので、空なのは起動時だけ (research.md #22)
        var settings = WidgetSettings.CreateDefault();
        var primary = ConnectedPrimary("PRIMARY", true);
        MonitorSettingsReconciler.Reconcile(settings, [primary]);
        settings.Monitors["PRIMARY"].IsVisible = false;

        MonitorSettingsReconciler.Reconcile(settings, [primary, ConnectedPrimary("SECONDARY", false)]);

        Assert.False(settings.Monitors["PRIMARY"].IsVisible);
        Assert.False(settings.Monitors["SECONDARY"].IsVisible);
    }

    private static WidgetSettings LegacySettings()
    {
        // 以前のバージョンの形: ルートに見た目とウィンドウ挙動があり、各モニタは既定値
        var settings = WidgetSettings.CreateDefault();
        settings.Appearance = new AppearanceSettings { TimeFontSize = 36.0, ShowDate = false };
        settings.WindowBehavior = new WindowBehaviorSettings { TopMost = false, PositionLocked = true };
        return settings;
    }

    [Fact]
    public void Reconcile_以前のバージョンの設定は各モニタへ複製を引き継ぎつながっていないモニタだけ非表示にする()
    {
        var settings = LegacySettings();
        settings.Monitors["CONNECTED"] = Placement(100);
        settings.Monitors["DISCONNECTED"] = Placement(200);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("CONNECTED", true)]);

        Assert.Equal(36.0, settings.Monitors["CONNECTED"].Appearance.TimeFontSize);
        Assert.False(settings.Monitors["CONNECTED"].Appearance.ShowDate);
        Assert.False(settings.Monitors["CONNECTED"].WindowBehavior.TopMost);
        Assert.True(settings.Monitors["CONNECTED"].WindowBehavior.PositionLocked);
        Assert.Equal(36.0, settings.Monitors["DISCONNECTED"].Appearance.TimeFontSize);
        Assert.True(settings.Monitors["CONNECTED"].IsVisible);
        Assert.False(settings.Monitors["DISCONNECTED"].IsVisible);
        Assert.Null(settings.Appearance);
        Assert.Null(settings.WindowBehavior);
    }

    [Fact]
    public void Reconcile_以前のバージョンの設定の複製はエントリごとに別のインスタンスでルートの元とも別()
    {
        var settings = LegacySettings();
        var rootAppearance = settings.Appearance;
        settings.Monitors["A"] = Placement(100);
        settings.Monitors["B"] = Placement(200);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("A", true), ConnectedPrimary("B", false)]);

        Assert.NotSame(settings.Monitors["A"].Appearance, settings.Monitors["B"].Appearance);
        Assert.NotSame(rootAppearance, settings.Monitors["A"].Appearance);
        Assert.NotSame(settings.Monitors["A"].WindowBehavior, settings.Monitors["B"].WindowBehavior);
        settings.Monitors["A"].Appearance.TimeFontSize = 12.0;
        Assert.Equal(36.0, settings.Monitors["B"].Appearance.TimeFontSize);
    }

    [Fact]
    public void Reconcile_引き継ぎは1回だけで後からつないだモニタは非表示のまま()
    {
        var settings = LegacySettings();
        settings.Monitors["A"] = Placement(100);
        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("A", true)]);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("A", true), ConnectedPrimary("B", false)]);

        Assert.False(settings.Monitors["B"].IsVisible);
        Assert.Equal(24.0, settings.Monitors["B"].Appearance.TimeFontSize);
        Assert.Null(settings.Appearance);
    }

    [Fact]
    public void Reconcile_以前のバージョンの設定でも別の端子につなぎ直したモニタはつながっているものとして扱う()
    {
        var settings = LegacySettings();
        var original = Placement(300);
        settings.Monitors[Id("DELF16C", 4353)] = original;

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary(Id("DELF16C", 4355), true)]);

        var moved = settings.Monitors[Id("DELF16C", 4355)];
        Assert.Same(original, moved);
        Assert.True(moved.IsVisible);
        Assert.Equal(36.0, moved.Appearance.TimeFontSize);
    }

    [Fact]
    public void Reconcile_以前のバージョンの設定で新しいモニタには引き継がず既定値で非表示にする()
    {
        var settings = LegacySettings();
        settings.Monitors["A"] = Placement(100);

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("A", true), ConnectedPrimary("NEW", false)]);

        Assert.Equal(24.0, settings.Monitors["NEW"].Appearance.TimeFontSize);
        Assert.True(settings.Monitors["NEW"].WindowBehavior.TopMost);
        Assert.False(settings.Monitors["NEW"].IsVisible);
    }

    [Fact]
    public void Reconcile_以前のバージョンの形でモニタの設定が空ならプライマリだけ表示し見た目は既定値()
    {
        // 設定ファイルを手で編集したときなど。共通の見た目は引き継ぐ先がなく、既定値で始めてよい (spec.md の Clarifications)
        var settings = LegacySettings();

        MonitorSettingsReconciler.Reconcile(settings, [ConnectedPrimary("PRIMARY", true), ConnectedPrimary("SECONDARY", false)]);

        Assert.True(settings.Monitors["PRIMARY"].IsVisible);
        Assert.False(settings.Monitors["SECONDARY"].IsVisible);
        Assert.Equal(24.0, settings.Monitors["PRIMARY"].Appearance.TimeFontSize);
        Assert.Null(settings.Appearance);
        Assert.Null(settings.WindowBehavior);
    }
}
