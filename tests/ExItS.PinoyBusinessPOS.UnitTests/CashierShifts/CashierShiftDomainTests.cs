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
}
