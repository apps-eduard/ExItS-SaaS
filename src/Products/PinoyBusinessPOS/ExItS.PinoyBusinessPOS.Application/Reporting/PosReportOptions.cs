namespace ExItS.PinoyBusinessPOS.Application.Reporting;

/// <summary>
/// Report query bounds for P8-WP06. A maximum inclusive span is required so organization-scoped
/// report queries cannot materialize unbounded history in a single request.
/// </summary>
public static class PosReportOptions
{
    /// <summary>
    /// Maximum inclusive calendar-day span (fromDate through toDate) for any report or dashboard
    /// period. Documented MVP bound: one leap year. Empty periods remain valid when both ends fall
    /// inside the span; exceeding the span returns <c>pos.report.range_too_large</c>.
    /// </summary>
    public const int MaxInclusiveDaySpan = 366;

    /// <summary>Default period when the client omits dates: the UTC calendar day of "now".</summary>
    public static (DateOnly From, DateOnly To) DefaultPeriodUtc(DateTimeOffset utcNow)
    {
        var day = DateOnly.FromDateTime(utcNow.UtcDateTime);
        return (day, day);
    }
}
