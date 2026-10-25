using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Resolves a preferred calendar day + Morning/Afternoon/Evening window against branch hours.
/// Does not invent an exact appointment time such as 09:00 for Morning.
/// </summary>
public static class PreferredFulfillmentSchedule
{
    /// <summary>
    /// Minimum usable intersection (minutes) after lead-time clipping before a period is offered.
    /// Conservative default when no seller-configured minimum usable window exists.
    /// </summary>
    public const int MinUsableWindowMinutes = 30;

    public const string DateFormat = "yyyy-MM-dd";

    public sealed record ResolvedPreferred(
        DateOnly Date,
        CustomerOrderPreferredFulfillmentPeriod Period,
        string TimeZoneId,
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc);

    public static ApplicationResult<ResolvedPreferred> Resolve(
        CustomerOrderFulfillmentType fulfillmentType,
        string? preferredDate,
        string? preferredPeriod,
        string? branchTimeZoneId,
        DateTimeOffset utcNow,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours = null,
        int minPreparationLeadMinutes = 0,
        int maxAdvanceScheduleDays = 0)
    {
        var hasDate = !string.IsNullOrWhiteSpace(preferredDate);
        var hasPeriod = !string.IsNullOrWhiteSpace(preferredPeriod);
        if (!hasDate && !hasPeriod)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                fulfillmentType == CustomerOrderFulfillmentType.Delivery
                    ? "Choose a preferred delivery day and period."
                    : "Choose a preferred pickup day and period.");
        }

        if (!hasDate || !hasPeriod)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                fulfillmentType == CustomerOrderFulfillmentType.Delivery
                    ? "Choose both a preferred delivery day and period."
                    : "Choose both a preferred pickup day and period.");
        }

        if (string.IsNullOrWhiteSpace(branchTimeZoneId))
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a preferred fulfillment window right now.");
        }

        if (!DateOnly.TryParseExact(
                preferredDate!.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var day))
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "The preferred fulfillment date is not valid for this store.");
        }

        if (!Enum.TryParse<CustomerOrderPreferredFulfillmentPeriod>(
                preferredPeriod!.Trim(),
                ignoreCase: true,
                out var period)
            || !Enum.IsDefined(period))
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Choose Morning, Afternoon, or Evening.");
        }

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(branchTimeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a preferred fulfillment window right now.");
        }
        catch (InvalidTimeZoneException)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store cannot accept a preferred fulfillment window right now.");
        }

        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone);
        var localToday = DateOnly.FromDateTime(localNow.DateTime);
        if (day < localToday)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Choose a preferred day that is still ahead for this store.");
        }

        if (maxAdvanceScheduleDays > 0)
        {
            var latestDay = localToday.AddDays(maxAdvanceScheduleDays);
            if (day > latestDay)
            {
                return ApplicationResult<ResolvedPreferred>.Failure(
                    DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                    $"Choose a day within the next {maxAdvanceScheduleDays} days for this store.");
            }
        }

        var openIntervals = OpenIntervalsForDay(day, operatingHours);
        if (openIntervals.Count == 0)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "This store is closed on that day.");
        }

        var periodWindow = PeriodWindow(period);
        var intersections = Intersect(openIntervals, periodWindow);
        if (intersections.Count == 0)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                $"{period} is not available while this store is open on that day.");
        }

        var earliestLocal = day == localToday
            ? localNow.DateTime.TimeOfDay.Add(TimeSpan.FromMinutes(Math.Max(0, minPreparationLeadMinutes)))
            : TimeSpan.Zero;

        var usable = ClipAfter(intersections, earliestLocal);
        if (!HasMinUsable(usable, MinUsableWindowMinutes))
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                minPreparationLeadMinutes > 0
                    ? $"{period} is no longer available after this store's preparation time."
                    : $"{period} is no longer available for this store today.");
        }

        var windowStartLocal = usable[0].Start;
        var windowEndLocal = usable[^1].End;
        var startUtc = ToUtc(day, windowStartLocal, zone);
        var endUtc = ToUtc(day, windowEndLocal, zone);
        if (endUtc <= utcNow)
        {
            return ApplicationResult<ResolvedPreferred>.Failure(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                $"{period} is no longer available for this store today.");
        }

        return ApplicationResult<ResolvedPreferred>.Success(
            new ResolvedPreferred(day, period, zone.Id, startUtc, endUtc));
    }

    /// <summary>Lists periods that still have a usable intersection for the day.</summary>
    public static IReadOnlyList<CustomerOrderPreferredFulfillmentPeriod> AvailablePeriods(
        DateOnly day,
        string? branchTimeZoneId,
        DateTimeOffset utcNow,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours,
        int minPreparationLeadMinutes = 0)
    {
        var list = new List<CustomerOrderPreferredFulfillmentPeriod>(3);
        foreach (CustomerOrderPreferredFulfillmentPeriod period in Enum.GetValues<CustomerOrderPreferredFulfillmentPeriod>())
        {
            var result = Resolve(
                CustomerOrderFulfillmentType.Pickup,
                day.ToString(DateFormat, CultureInfo.InvariantCulture),
                period.ToString(),
                branchTimeZoneId,
                utcNow,
                operatingHours,
                minPreparationLeadMinutes,
                maxAdvanceScheduleDays: 0);
            if (result.IsSuccess)
            {
                list.Add(period);
            }
        }

        return list;
    }

    internal static (TimeSpan Start, TimeSpan End) PeriodWindow(CustomerOrderPreferredFulfillmentPeriod period) =>
        period switch
        {
            CustomerOrderPreferredFulfillmentPeriod.Morning => (TimeSpan.Zero, TimeSpan.FromHours(12)),
            CustomerOrderPreferredFulfillmentPeriod.Afternoon => (TimeSpan.FromHours(12), TimeSpan.FromHours(18)),
            CustomerOrderPreferredFulfillmentPeriod.Evening => (TimeSpan.FromHours(18), TimeSpan.FromHours(24)),
            _ => (TimeSpan.Zero, TimeSpan.Zero)
        };

    internal static IReadOnlyList<(TimeSpan Start, TimeSpan End)> OpenIntervalsForDay(
        DateOnly day,
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot>? operatingHours)
    {
        if (operatingHours is null || operatingHours.Count == 0)
        {
            return [];
        }

        var intervals = new List<(TimeSpan Start, TimeSpan End)>(2);
        var today = FindDay(operatingHours, day.DayOfWeek);
        if (today is null)
        {
            return [];
        }

        if (today.IsClosed)
        {
            // Overnight spill from previous calendar day may still open early morning.
        }
        else if (today.IsOpen24Hours)
        {
            intervals.Add((TimeSpan.Zero, TimeSpan.FromHours(24)));
        }
        else if (TryParseClock(today.OpenTime, out var open) && TryParseClock(today.CloseTime, out var close))
        {
            if (close > open)
            {
                intervals.Add((open, close));
            }
            else if (close < open)
            {
                // Overnight: open late through end of this calendar day.
                intervals.Add((open, TimeSpan.FromHours(24)));
            }
        }

        var previous = FindDay(operatingHours, day.AddDays(-1).DayOfWeek);
        if (previous is not null
            && !previous.IsClosed
            && !previous.IsOpen24Hours
            && TryParseClock(previous.OpenTime, out var prevOpen)
            && TryParseClock(previous.CloseTime, out var prevClose)
            && prevClose < prevOpen
            && prevClose > TimeSpan.Zero)
        {
            // Previous overnight window continues past midnight into this morning.
            intervals.Add((TimeSpan.Zero, prevClose));
        }

        return Merge(intervals);
    }

    private static CustomerOrderBranchHoursDaySnapshot? FindDay(
        IReadOnlyList<CustomerOrderBranchHoursDaySnapshot> operatingHours,
        DayOfWeek weekday) =>
        operatingHours.FirstOrDefault(item =>
            Enum.TryParse<DayOfWeek>(item.DayOfWeek, true, out var day) && day == weekday);

    private static bool TryParseClock(string? value, out TimeSpan clock)
    {
        clock = default;
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return false;
        }

        clock = time.ToTimeSpan();
        return true;
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> Intersect(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> open,
        (TimeSpan Start, TimeSpan End) period)
    {
        var result = new List<(TimeSpan Start, TimeSpan End)>();
        foreach (var interval in open)
        {
            var start = interval.Start > period.Start ? interval.Start : period.Start;
            var end = interval.End < period.End ? interval.End : period.End;
            if (end > start)
            {
                result.Add((start, end));
            }
        }

        return result;
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> ClipAfter(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> intervals,
        TimeSpan earliest)
    {
        var result = new List<(TimeSpan Start, TimeSpan End)>();
        foreach (var interval in intervals)
        {
            var start = interval.Start < earliest ? earliest : interval.Start;
            if (interval.End > start)
            {
                result.Add((start, interval.End));
            }
        }

        return result;
    }

    private static bool HasMinUsable(IReadOnlyList<(TimeSpan Start, TimeSpan End)> intervals, int minMinutes)
    {
        var need = TimeSpan.FromMinutes(Math.Max(1, minMinutes));
        return intervals.Any(i => i.End - i.Start >= need);
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> Merge(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> intervals)
    {
        if (intervals.Count <= 1)
        {
            return intervals;
        }

        var ordered = intervals.OrderBy(i => i.Start).ToList();
        var merged = new List<(TimeSpan Start, TimeSpan End)> { ordered[0] };
        for (var i = 1; i < ordered.Count; i++)
        {
            var last = merged[^1];
            var next = ordered[i];
            if (next.Start <= last.End)
            {
                merged[^1] = (last.Start, next.End > last.End ? next.End : last.End);
            }
            else
            {
                merged.Add(next);
            }
        }

        return merged;
    }

    private static DateTimeOffset ToUtc(DateOnly day, TimeSpan clock, TimeZoneInfo zone)
    {
        // End-of-day 24:00 maps to next midnight exclusive bound as 23:59:59.999 locally is awkward;
        // use exact next-day 00:00 when clock is 24:00.
        DateTime unspecified;
        if (clock >= TimeSpan.FromHours(24))
        {
            unspecified = day.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        }
        else
        {
            unspecified = day.ToDateTime(TimeOnly.FromTimeSpan(clock), DateTimeKind.Unspecified);
        }

        if (zone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
        return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
    }
}
