using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BranchHoursScheduleUiTests
{
    [Fact]
    public void FormatSummary_renders_closed_24h_and_hours()
    {
        Assert.Equal(
            "Closed",
            BranchHoursScheduleUi.FormatSummary(true, false, new TimeOnly(8, 0), new TimeOnly(21, 0), "Closed", "24 hours"));
        Assert.Equal(
            "24 hours",
            BranchHoursScheduleUi.FormatSummary(false, true, new TimeOnly(8, 0), new TimeOnly(21, 0), "Closed", "24 hours"));
        Assert.Equal(
            "8:00 AM – 9:00 PM",
            BranchHoursScheduleUi.FormatSummary(false, false, new TimeOnly(8, 0), new TimeOnly(21, 0), "Closed", "24 hours"));
        Assert.False(BranchHoursScheduleUi.ShowsTimes(true, false));
        Assert.False(BranchHoursScheduleUi.ShowsTimes(false, true));
        Assert.True(BranchHoursScheduleUi.ShowsTimes(false, false));
    }

    [Fact]
    public void CopyToSelected_changes_only_checked_days_and_keeps_source()
    {
        var days = BranchHoursScheduleUi.OrderedDays
            .Select(name => new BranchHoursDayDraft { DayOfWeek = name, IsClosed = true })
            .ToList();
        days[0].IsClosed = false;
        days[0].OpenTime = new TimeOnly(8, 0);
        days[0].CloseTime = new TimeOnly(21, 0);

        var targets = BranchHoursScheduleUi.DefaultCopyTargets(nameof(DayOfWeek.Monday));
        BranchHoursScheduleUi.CopyToSelected(days, nameof(DayOfWeek.Monday), targets);

        Assert.Equal("Open", days[0].Mode);
        Assert.Equal(new TimeOnly(8, 0), days[0].OpenTime);
        foreach (var weekday in new[] { days[1], days[2], days[3], days[4] })
        {
            Assert.Equal("Open", weekday.Mode);
            Assert.Equal(new TimeOnly(8, 0), weekday.OpenTime);
            Assert.Equal(new TimeOnly(21, 0), weekday.CloseTime);
        }

        Assert.True(days[5].IsClosed);
        Assert.True(days[6].IsClosed);
    }

    [Fact]
    public void ToDto_omits_times_for_closed_and_24h()
    {
        var closed = new BranchHoursDayDraft { DayOfWeek = "Saturday", IsClosed = true, OpenTime = new TimeOnly(8, 0) };
        var open24 = new BranchHoursDayDraft { DayOfWeek = "Sunday", IsOpen24Hours = true, OpenTime = new TimeOnly(8, 0) };
        var hours = new BranchHoursDayDraft
        {
            DayOfWeek = "Monday",
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(21, 0)
        };

        var closedDto = BranchHoursScheduleUi.ToDto(closed);
        var open24Dto = BranchHoursScheduleUi.ToDto(open24);
        var hoursDto = BranchHoursScheduleUi.ToDto(hours);

        Assert.True(closedDto.IsClosed);
        Assert.Null(closedDto.OpenTime);
        Assert.Null(closedDto.CloseTime);
        Assert.True(open24Dto.IsOpen24Hours);
        Assert.Null(open24Dto.OpenTime);
        Assert.Equal("08:00", hoursDto.OpenTime);
        Assert.Equal("21:00", hoursDto.CloseTime);
        Assert.False(hoursDto.IsClosed);
        Assert.False(hoursDto.IsOpen24Hours);
    }

    [Fact]
    public void Day_editor_preserves_existing_values_until_apply()
    {
        var monday = new BranchHoursDayDraft
        {
            DayOfWeek = "Monday",
            OpenTime = new TimeOnly(8, 0),
            CloseTime = new TimeOnly(21, 0)
        };
        var draft = monday.Clone();
        draft.Mode = BranchHoursScheduleUi.ModeClosed;
        Assert.Equal("Open", monday.Mode);
        Assert.Equal("Closed", draft.Mode);

        monday.ApplyFrom(draft);
        Assert.True(monday.IsClosed);
        Assert.Equal(new TimeOnly(8, 0), monday.OpenTime);
    }
}
