using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class SettingsCloneTests
{
    [Fact]
    public void AppearanceSettings_Clone_すべての項目を写し別のインスタンスを返す()
    {
        // すべての項目を既定値と違う値にして、写し漏れがないことを確かめる
        var original = new AppearanceSettings
        {
            ShowDate = false,
            ShowDayOfWeek = false,
            ShowSeconds = false,
            FontFamily = "Yu Gothic UI",
            TimeFontSize = 40.0,
            TimeFontColor = "#FF112233",
            DateFontSize = 20.0,
            DateFontColor = "#FF445566",
            DateSeparator = "-",
            DayOfWeekFormat = DayOfWeekFormat.LongEnglish,
            DateDayOfWeekPosition = RelativePosition.Right,
            BackgroundColor = "#FF778899",
            BackgroundOpacity = 70.0,
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.False(clone.ShowDate);
        Assert.False(clone.ShowDayOfWeek);
        Assert.False(clone.ShowSeconds);
        Assert.Equal("Yu Gothic UI", clone.FontFamily);
        Assert.Equal(40.0, clone.TimeFontSize);
        Assert.Equal("#FF112233", clone.TimeFontColor);
        Assert.Equal(20.0, clone.DateFontSize);
        Assert.Equal("#FF445566", clone.DateFontColor);
        Assert.Equal("-", clone.DateSeparator);
        Assert.Equal(DayOfWeekFormat.LongEnglish, clone.DayOfWeekFormat);
        Assert.Equal(RelativePosition.Right, clone.DateDayOfWeekPosition);
        Assert.Equal("#FF778899", clone.BackgroundColor);
        Assert.Equal(70.0, clone.BackgroundOpacity);
    }

    [Fact]
    public void AppearanceSettings_Clone_複製を変えても元は変わらない()
    {
        var original = new AppearanceSettings { TimeFontSize = 40.0, DateSeparator = null };

        var clone = original.Clone();
        clone.TimeFontSize = 12.0;
        clone.DateSeparator = ".";

        Assert.Equal(40.0, original.TimeFontSize);
        Assert.Null(original.DateSeparator);
    }

    [Fact]
    public void WindowBehaviorSettings_Clone_すべての項目を写し別のインスタンスを返す()
    {
        var original = new WindowBehaviorSettings { TopMost = false, PositionLocked = true };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.False(clone.TopMost);
        Assert.True(clone.PositionLocked);
    }

    [Fact]
    public void WindowBehaviorSettings_Clone_複製を変えても元は変わらない()
    {
        var original = new WindowBehaviorSettings { TopMost = true, PositionLocked = false };

        var clone = original.Clone();
        clone.TopMost = false;
        clone.PositionLocked = true;

        Assert.True(original.TopMost);
        Assert.False(original.PositionLocked);
    }
}
