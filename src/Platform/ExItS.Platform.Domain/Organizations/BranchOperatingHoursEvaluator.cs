namespace ExItS.Platform.Domain.Organizations;

public enum BranchStoreOpenStatus
{
    Open,
    Closed,
    OpensLater,
    ClosesLater
}

public sealed record BranchStoreOpenState(
    BranchStoreOpenStatus Status,
    bool IsOpenNow,
    TimeOnly? NextOpenTimeLocal,
    TimeOnly? NextCloseTimeLocal,
    DayOfWeek? NextOpenDayOfWeekLocal);

public interface IBranchOperatingHoursEvaluator
{
    BranchStoreOpenState Evaluate(
        BranchOperatingHoursSchedule? schedule,
        string effectiveTimeZoneId,
        DateTimeOffset evaluationInstantUtc);
}

/// <summary>
/// Evaluates whether a branch is open using the branch effective timezone (never client local).
/// Supports closed days, 24-hour days, and one interval per day (including overnight close-after-midnight).
/// </summary>
public sealed class BranchOperatingHoursEvaluator : IBranchOperatingHoursEvaluator
{
    public BranchStoreOpenState Evaluate(
        BranchOperatingHoursSchedule? schedule,
        string effectiveTimeZoneId,
        DateTimeOffset evaluationInstantUtc)
    {
        if (schedule is null || !schedule.IsConfigured)
        {
            return new BranchStoreOpenState(BranchStoreOpenStatus.Closed, false, null, null, null);
        }

        if (!BranchEffectiveTimeZone.TryResolve(effectiveTimeZoneId, out var timeZone))
        {
            return new BranchStoreOpenState(BranchStoreOpenStatus.Closed, false, null, null, null);
        }

        var localNow = TimeZoneInfo.ConvertTime(evaluationInstantUtc, timeZone);
        var today = localNow.DayOfWeek;
        var nowTime = TimeOnly.FromDateTime(localNow.DateTime);
        var todayHours = schedule.Days.First(d => d.DayOfWeek == today);

        if (IsOpenAt(todayHours, nowTime))
        {
            var closeAt = todayHours.IsOpen24Hours ? (TimeOnly?)null : todayHours.CloseTime;
            return new BranchStoreOpenState(
                closeAt is null ? BranchStoreOpenStatus.Open : BranchStoreOpenStatus.ClosesLater,
                true,
                null,
                closeAt,
                null);
        }

        // Overnight window from previous day may still be open.
        var yesterday = today == DayOfWeek.Sunday ? DayOfWeek.Saturday : today - 1;
        var yesterdayHours = schedule.Days.First(d => d.DayOfWeek == yesterday);
        if (!yesterdayHours.IsClosed && !yesterdayHours.IsOpen24Hours
            && yesterdayHours.OpenTime is not null && yesterdayHours.CloseTime is not null
            && yesterdayHours.CloseTime < yesterdayHours.OpenTime
            && nowTime < yesterdayHours.CloseTime)
        {
            return new BranchStoreOpenState(
                BranchStoreOpenStatus.ClosesLater,
                true,
                null,
                yesterdayHours.CloseTime,
                null);
        }

        var (nextDay, nextHours, nextOpen) = FindNextOpen(schedule, today, nowTime);
        return new BranchStoreOpenState(
            BranchStoreOpenStatus.OpensLater,
            false,
            nextOpen,
            null,
            nextDay);
    }

    private static bool IsOpenAt(BranchDayOperatingHours day, TimeOnly now)
    {
        if (day.IsClosed)
        {
            return false;
        }

        if (day.IsOpen24Hours)
        {
            return true;
        }

        if (day.OpenTime is null || day.CloseTime is null)
        {
            return false;
        }

        if (day.CloseTime > day.OpenTime)
        {
            return now >= day.OpenTime && now < day.CloseTime;
        }

        // Overnight interval within same calendar day start (e.g. 22:00–02:00): open after open time.
        return now >= day.OpenTime || now < day.CloseTime;
    }

    private static (DayOfWeek Day, BranchDayOperatingHours Hours, TimeOnly OpenTime) FindNextOpen(
        BranchOperatingHoursSchedule schedule,
        DayOfWeek startDay,
        TimeOnly startTime)
    {
        for (var offset = 0; offset < 7; offset++)
        {
            var day = (DayOfWeek)(((int)startDay + offset) % 7);
            var hours = schedule.Days.First(d => d.DayOfWeek == day);
            if (hours.IsClosed)
            {
                continue;
            }

            if (hours.IsOpen24Hours)
            {
                return (day, hours, TimeOnly.MinValue);
            }

            if (hours.OpenTime is null)
            {
                continue;
            }

            if (offset == 0 && startTime < hours.OpenTime)
            {
                return (day, hours, hours.OpenTime.Value);
            }

            if (offset > 0)
            {
                return (day, hours, hours.OpenTime.Value);
            }
        }

        return (startDay, schedule.Days.First(d => d.DayOfWeek == startDay), TimeOnly.MinValue);
    }
}
