using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

/// <summary>P24-WP10: free-window calendar math + shared settled-history policy (no I/O).</summary>
public sealed class PersonalSettledHistoryPolicyTests
{
    [Theory]
    [InlineData(2026, 8, 1, 2026, 6, 1)]
    [InlineData(2026, 8, 31, 2026, 6, 1)]
    [InlineData(2026, 8, 15, 2026, 6, 1)]
    [InlineData(2026, 1, 15, 2025, 11, 1)]
    public void Free_window_is_utc_calendar_months_not_rolling_days(
        int asOfY, int asOfM, int asOfD,
        int startY, int startM, int startD)
    {
        var asOf = new DateTimeOffset(asOfY, asOfM, asOfD, 23, 59, 59, TimeSpan.Zero);
        var expected = new DateTimeOffset(startY, startM, startD, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expected, PersonalHistoryWindows.ComputeFreeWindowStart(asOf, 3));
    }

    [Fact]
    public void Free_window_not_rolling_90_days()
    {
        var asOf = new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);
        var freeStart = PersonalHistoryWindows.ComputeFreeWindowStart(asOf, 3);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), freeStart);
        Assert.True(freeStart > asOf.AddDays(-90));
    }

    [Fact]
    public void Policy_allows_free_window_without_entitlement()
    {
        var freeStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            freeStart,
            openDebtExceptionApplies: false,
            hasExtendedEntitlement: false);
        Assert.Equal(PersonalHistoryDetailAccessDecision.Allowed, decision);
    }

    [Fact]
    public void Policy_denies_old_settled_without_entitlement()
    {
        var freeStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero),
            freeStart,
            openDebtExceptionApplies: false,
            hasExtendedEntitlement: false);
        Assert.Equal(PersonalHistoryDetailAccessDecision.ExtendedHistoryRequired, decision);
    }

    [Fact]
    public void Policy_open_debt_exception_allows_without_entitlement()
    {
        var freeStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            freeStart,
            openDebtExceptionApplies: true,
            hasExtendedEntitlement: false);
        Assert.Equal(PersonalHistoryDetailAccessDecision.Allowed, decision);
    }

    [Fact]
    public void Policy_entitlement_allows_old_settled()
    {
        var freeStart = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var decision = PersonalSettledHistoryPolicy.EvaluateDetailAccess(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            freeStart,
            openDebtExceptionApplies: false,
            hasExtendedEntitlement: true);
        Assert.Equal(PersonalHistoryDetailAccessDecision.Allowed, decision);
    }

    [Fact]
    public void Open_debt_receipt_exception_requires_positive_outstanding_and_active_credit()
    {
        Assert.False(PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            0m, true, true, true));
        Assert.False(PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            10m, true, true, linkedCreditIsActive: false));
        Assert.False(PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            10m, isUtangSale: false, true, true));
        Assert.True(PersonalSettledHistoryPolicy.OpenDebtReceiptExceptionApplies(
            10m, true, true, true));
    }
}
