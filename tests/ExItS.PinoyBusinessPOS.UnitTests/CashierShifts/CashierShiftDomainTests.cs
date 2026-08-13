using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CashierShifts;

public sealed class CashierShiftDomainTests
{
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly RegisterId Register = RegisterId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Open_creates_open_shift_with_normalized_number()
    {
        var shift = CashierShift.Open(Org, "shift-20260731-000001", Actor, Register, 500m, Now);
        Assert.Equal(CashierShiftStatus.Open, shift.Status);
        Assert.Equal("SHIFT-20260731-000001", shift.ShiftNumber);
        Assert.Equal(500m, shift.OpeningCashAmount);
        Assert.Equal(Register, shift.RegisterId);
    }

    [Fact]
    public void Close_snapshots_expected_and_variance()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 100m, Now);
        shift.Close(150m, 140m, Actor, Now.AddHours(8), "Balanced");
        Assert.Equal(CashierShiftStatus.Closed, shift.Status);
        Assert.Equal(140m, shift.ExpectedCashAmountSnapshot);
        Assert.Equal(10m, shift.CashVarianceAmount);
    }

    [Fact]
    public void Cancel_denied_when_sales_or_movements_present()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 0m, Now);
        var ex = Assert.Throws<DomainException>(() => shift.Cancel(Actor, Now, hasLinkedSales: true, hasMovements: false));
        Assert.Equal(DomainErrorCodes.CashierShiftCancelBlockedByActivity, ex.ErrorCode);
    }

    [Fact]
    public void Expected_cash_includes_voided_cash_reversal()
    {
        var movements = new[]
        {
            CashierShiftMovement.Create(
                CashierShiftId.New(),
                Org,
                CashierShiftMovementType.CashIn,
                20m,
                "Petty",
                Actor,
                Now),
            CashierShiftMovement.Create(
                CashierShiftId.New(),
                Org,
                CashierShiftMovementType.CashOut,
                5m,
                "Change fund",
                Actor,
                Now)
        };

        var expected = CashierShiftExpectedCash.Compute(
            openingCashAmount: 100m,
            netCashSales: 50m,
            movements);

        Assert.Equal(165m, expected);
    }

    [Fact]
    public void Expected_cash_subtracts_cash_refunds_on_shift()
    {
        var expected = CashierShiftExpectedCash.Compute(
            openingCashAmount: 100m,
            netCashSales: 50m,
            movements: [],
            cashRefundsOnShift: 15m);

        Assert.Equal(135m, expected);
    }

    [Fact]
    public void Negative_opening_cash_rejected()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, -1m, Now));
        Assert.Equal(DomainErrorCodes.InvalidCashierShiftOpeningCash, ex.ErrorCode);
    }

    [Fact]
    public void Off_mode_opens_and_closes_without_physical_count_and_still_snapshots_expected()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, null, Now, cashCountMode: CashCountMode.Off);
        Assert.False(shift.OpeningCashCounted);
        Assert.Equal(0m, shift.OpeningCashAmount);
        Assert.Equal(CashCountMode.Off, shift.EffectiveCashCountMode);

        shift.Close(null, expectedCashAmount: 175m, Actor, Now.AddHours(8));
        Assert.Equal(CashierShiftStatus.Closed, shift.Status);
        Assert.Null(shift.ClosingCashAmount);
        Assert.Null(shift.CashVarianceAmount);
        Assert.Equal(175m, shift.ExpectedCashAmountSnapshot);
        Assert.Equal(CashCountStates.NotRequired, CashCountModes.ClosingState(shift.EffectiveCashCountMode, shift.ClosingCashAmount));
    }

    [Fact]
    public void Optional_mode_allows_opening_count_or_skip()
    {
        var counted = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 1000m, Now, cashCountMode: CashCountMode.Optional);
        Assert.True(counted.OpeningCashCounted);
        Assert.Equal(1000m, counted.OpeningCashAmount);

        var skipped = CashierShift.Open(Org, "SHIFT-20260731-000002", Actor, Register, null, Now, cashCountMode: CashCountMode.Optional);
        Assert.False(skipped.OpeningCashCounted);
        Assert.Equal(0m, skipped.OpeningCashAmount);
    }

    [Fact]
    public void Optional_mode_skip_close_persists_null_not_zero()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 100m, Now, cashCountMode: CashCountMode.Optional);
        shift.Close(null, expectedCashAmount: 300m, Actor, Now.AddHours(8));
        Assert.Null(shift.ClosingCashAmount);
        Assert.Null(shift.CashVarianceAmount);
        Assert.Equal(300m, shift.ExpectedCashAmountSnapshot);
        Assert.Equal(CashCountStates.NotPerformed, CashCountModes.ClosingState(shift.EffectiveCashCountMode, shift.ClosingCashAmount));
    }

    [Fact]
    public void Optional_mode_counted_close_records_variance()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 100m, Now, cashCountMode: CashCountMode.Optional);
        shift.Close(90m, expectedCashAmount: 100m, Actor, Now.AddHours(8));
        Assert.Equal(90m, shift.ClosingCashAmount);
        Assert.Equal(-10m, shift.CashVarianceAmount);
        Assert.Equal(CashVarianceKinds.Short, CashVarianceKinds.Classify(shift.CashVarianceAmount!.Value));
    }

    [Fact]
    public void Required_mode_rejects_missing_opening_and_closing_count()
    {
        var openEx = Assert.Throws<DomainException>(() =>
            CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, null, Now, cashCountMode: CashCountMode.Required));
        Assert.Equal(DomainErrorCodes.CashierShiftOpeningCashCountRequired, openEx.ErrorCode);

        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 0m, Now, cashCountMode: CashCountMode.Required);
        var closeEx = Assert.Throws<DomainException>(() => shift.Close(null, 0m, Actor, Now.AddHours(8)));
        Assert.Equal(DomainErrorCodes.CashierShiftClosingCashCountRequired, closeEx.ErrorCode);
        Assert.Equal(CashierShiftStatus.Open, shift.Status);
    }

    [Fact]
    public void Required_mode_accepts_valid_counted_cash()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 50m, Now, cashCountMode: CashCountMode.Required);
        shift.Close(50m, 50m, Actor, Now.AddHours(8));
        Assert.Equal(CashierShiftStatus.Closed, shift.Status);
        Assert.Equal(50m, shift.ClosingCashAmount);
        Assert.Equal(0m, shift.CashVarianceAmount);
        Assert.Equal(CashVarianceKinds.Balanced, CashVarianceKinds.Classify(shift.CashVarianceAmount!.Value));
    }

    [Theory]
    [InlineData(140, 140, "Balanced")]
    [InlineData(150, 140, "Over")]
    [InlineData(130, 140, "Short")]
    public void Variance_classifies_balanced_over_and_short(decimal counted, decimal expected, string kind)
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 100m, Now, cashCountMode: CashCountMode.Optional);
        shift.Close(counted, expected, Actor, Now.AddHours(8));
        Assert.Equal(counted - expected, shift.CashVarianceAmount);
        Assert.Equal(kind, CashVarianceKinds.Classify(shift.CashVarianceAmount!.Value));
    }

    [Fact]
    public void Snapshotted_optional_mode_still_allows_skip_after_org_would_have_changed()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, null, Now, cashCountMode: CashCountMode.Optional);
        Assert.Equal(CashCountMode.Optional, shift.EffectiveCashCountMode);
        shift.Close(null, expectedCashAmount: 80m, Actor, Now.AddHours(8));
        Assert.Null(shift.ClosingCashAmount);
        Assert.Equal(80m, shift.ExpectedCashAmountSnapshot);
    }

    [Fact]
    public void Counted_zero_is_distinct_from_skipped_null()
    {
        var countedZero = CashierShift.Open(Org, "SHIFT-20260731-000001", Actor, Register, 0m, Now, cashCountMode: CashCountMode.Optional);
        countedZero.Close(0m, expectedCashAmount: 10m, Actor, Now.AddHours(8));
        Assert.True(countedZero.OpeningCashCounted);
        Assert.Equal(0m, countedZero.ClosingCashAmount);
        Assert.Equal(-10m, countedZero.CashVarianceAmount);

        var skipped = CashierShift.Open(Org, "SHIFT-20260731-000002", Actor, Register, null, Now, cashCountMode: CashCountMode.Optional);
        skipped.Close(null, expectedCashAmount: 10m, Actor, Now.AddHours(8));
        Assert.False(skipped.OpeningCashCounted);
        Assert.Null(skipped.ClosingCashAmount);
        Assert.Null(skipped.CashVarianceAmount);
    }
}
