using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Reporting;

public sealed class ReportDateRangeTests
{
    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    [Fact]
    public void Defaults_to_utc_today_when_dates_omitted()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 7, 30, 15, 0, 0, TimeSpan.Zero));
        var result = ReportDateRange.Resolve(null, null, clock);
        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 7, 30), result.Value!.FromDate);
        Assert.Equal(new DateOnly(2026, 7, 30), result.Value.ToDate);
    }

    [Fact]
    public void Rejects_inverted_range()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var result = ReportDateRange.Resolve(new DateOnly(2026, 7, 31), new DateOnly(2026, 7, 30), clock);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReportInvalidDateRange, result.ErrorCode);
    }

    [Fact]
    public void Rejects_range_beyond_max_inclusive_span()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var from = new DateOnly(2025, 1, 1);
        var to = from.AddDays(PosReportOptions.MaxInclusiveDaySpan);
        var result = ReportDateRange.Resolve(from, to, clock);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ReportRangeTooLarge, result.ErrorCode);
    }

    [Fact]
    public void Accepts_max_inclusive_span()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var from = new DateOnly(2025, 1, 1);
        var to = from.AddDays(PosReportOptions.MaxInclusiveDaySpan - 1);
        var result = ReportDateRange.Resolve(from, to, clock);
        Assert.True(result.IsSuccess);
        Assert.Equal(PosReportOptions.MaxInclusiveDaySpan, result.Value!.InclusiveDayCount);
    }

    [Fact]
    public void Preceding_equal_length_period_is_contiguous()
    {
        var range = new ReportDateRange(new DateOnly(2026, 7, 21), new DateOnly(2026, 7, 30));
        var prior = range.PrecedingEqualLengthPeriod();
        Assert.Equal(new DateOnly(2026, 7, 11), prior.FromDate);
        Assert.Equal(new DateOnly(2026, 7, 20), prior.ToDate);
        Assert.Equal(range.InclusiveDayCount, prior.InclusiveDayCount);
    }

    [Fact]
    public void Comparison_marks_percentage_unavailable_when_prior_is_zero()
    {
        var prior = new ReportDateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1));
        var comparison = ReportMath_Compare(100m, 0m, prior);
        Assert.Equal(100m, comparison.AbsoluteChange);
        Assert.False(comparison.PercentageAvailable);
        Assert.Null(comparison.PercentageChange);
    }

    // Expose Compare for unit test without making ReportMath public API surface larger than needed.
    private static ReportPeriodComparisonDto ReportMath_Compare(
        decimal current,
        decimal prior,
        ReportDateRange priorRange)
    {
        var absolute = decimal.Round(current - prior, 2, MidpointRounding.AwayFromZero);
        if (prior == 0m)
        {
            return new ReportPeriodComparisonDto(
                priorRange.FromDate,
                priorRange.ToDate,
                absolute,
                null,
                false);
        }

        var pct = Math.Round((current - prior) / prior * 100m, 2, MidpointRounding.AwayFromZero);
        return new ReportPeriodComparisonDto(priorRange.FromDate, priorRange.ToDate, absolute, pct, true);
    }
}
