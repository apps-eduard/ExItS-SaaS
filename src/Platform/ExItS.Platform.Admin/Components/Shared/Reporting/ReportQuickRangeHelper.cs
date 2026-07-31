namespace ExItS.Platform.Admin.Components.Shared.Reporting;

/// <summary>
/// Quick range presets for report filters. Values are UI convenience only;
/// the page still submits from/to dates to the server for authoritative results.
/// </summary>
public enum ReportQuickRange
{
    Today,
    Last7Days,
    Last30Days,
    ThisMonth
}

public static class ReportQuickRangeHelper
{
    /// <summary>
    /// Computes inclusive calendar dates for a quick range using the provided UTC "today".
    /// Does not invent totals — dates are filter inputs only.
    /// </summary>
    public static (DateOnly From, DateOnly To) Resolve(ReportQuickRange range, DateOnly utcToday) =>
        range switch
        {
            ReportQuickRange.Last7Days => (utcToday.AddDays(-6), utcToday),
            ReportQuickRange.Last30Days => (utcToday.AddDays(-29), utcToday),
            ReportQuickRange.ThisMonth => (new DateOnly(utcToday.Year, utcToday.Month, 1), utcToday),
            _ => (utcToday, utcToday)
        };

    public static bool IsWithinMaxSpan(DateOnly from, DateOnly to, int maxInclusiveDays) =>
        to >= from && (to.DayNumber - from.DayNumber + 1) <= maxInclusiveDays;
}
