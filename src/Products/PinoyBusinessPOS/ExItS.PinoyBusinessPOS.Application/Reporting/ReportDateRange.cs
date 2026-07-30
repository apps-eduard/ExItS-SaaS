using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

/// <summary>
/// Inclusive calendar-date range for operational reports. Storage timestamps remain UTC; range
/// membership for sales/movements/credits/repayments uses the UTC calendar date of the recorded
/// timestamp. Expenses use <c>ExpenseDate</c> (calendar-date semantics).
/// </summary>
public sealed record ReportDateRange(DateOnly FromDate, DateOnly ToDate)
{
    public int InclusiveDayCount => ToDate.DayNumber - FromDate.DayNumber + 1;

    /// <summary>Immediately preceding equal-length inclusive period ending the day before <see cref="FromDate"/>.</summary>
    public ReportDateRange PrecedingEqualLengthPeriod()
    {
        var priorTo = FromDate.AddDays(-1);
        var priorFrom = priorTo.AddDays(-(InclusiveDayCount - 1));
        return new ReportDateRange(priorFrom, priorTo);
    }

    public static ApplicationResult<ReportDateRange> Resolve(
        DateOnly? fromDate,
        DateOnly? toDate,
        IClock clock)
    {
        var (defaultFrom, defaultTo) = PosReportOptions.DefaultPeriodUtc(clock.UtcNow);
        var from = fromDate ?? defaultFrom;
        var to = toDate ?? defaultTo;

        if (to < from)
        {
            return ApplicationResult<ReportDateRange>.Failure(
                ApplicationErrorCodes.ReportInvalidDateRange,
                "Report toDate must be on or after fromDate.");
        }

        var span = to.DayNumber - from.DayNumber + 1;
        if (span > PosReportOptions.MaxInclusiveDaySpan)
        {
            return ApplicationResult<ReportDateRange>.Failure(
                ApplicationErrorCodes.ReportRangeTooLarge,
                $"Report date range cannot exceed {PosReportOptions.MaxInclusiveDaySpan} inclusive calendar days.");
        }

        return ApplicationResult<ReportDateRange>.Success(new ReportDateRange(from, to));
    }

    public static DateTimeOffset InclusiveStartUtc(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public static DateTimeOffset ExclusiveEndUtc(DateOnly date) =>
        new(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public bool ContainsUtcCalendarDay(DateTimeOffset utcTimestamp)
    {
        var day = DateOnly.FromDateTime(utcTimestamp.UtcDateTime);
        return day >= FromDate && day <= ToDate;
    }
}
