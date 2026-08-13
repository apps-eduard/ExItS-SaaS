using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CashierShifts;

public sealed class CashCountDenominationTests
{
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly RegisterId Register = RegisterId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Default_php_seed_includes_current_useful_denominations()
    {
        Assert.Equal([1000m, 500m, 200m, 100m, 50m, 20m, 10m, 5m, 1m], PhilippineCashDenominationDefaults.Values);
    }

    [Fact]
    public void Recalculate_matches_authoritative_total()
    {
        var lines = new[]
        {
            CashCountDenominationLine.Create(1000m, 2),
            CashCountDenominationLine.Create(500m, 3),
            CashCountDenominationLine.Create(100m, 10),
            CashCountDenominationLine.Create(50m, 5)
        };

        Assert.Equal(4750m, CashCountDenominationBreakdown.Recalculate(lines));
        Assert.Equal(lines, CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(4750m, lines));
    }

    [Fact]
    public void Zero_quantity_is_allowed_and_negative_is_rejected()
    {
        var zero = CashCountDenominationLine.Create(1000m, 0);
        Assert.Equal(0m, zero.LineTotal);
        var ex = Assert.Throws<DomainException>(() => CashCountDenominationLine.Create(1000m, -1));
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Zero_and_negative_denomination_values_are_rejected()
    {
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationValue,
            Assert.Throws<DomainException>(() => OrganizationCashDenomination.NormalizeValue(0m)).ErrorCode);
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationValue,
            Assert.Throws<DomainException>(() => OrganizationCashDenomination.NormalizeValue(-5m)).ErrorCode);
    }

    [Fact]
    public void Duplicate_lines_and_total_mismatch_are_rejected()
    {
        var duplicate = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.Normalize(
        [
            CashCountDenominationLine.Create(1000m, 1),
            CashCountDenominationLine.Create(1000m, 2)
        ]));
        Assert.Equal(DomainErrorCodes.CashCountDenominationDuplicateLine, duplicate.ErrorCode);

        var mismatch = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(
            100m,
            [CashCountDenominationLine.Create(1000m, 1)]));
        Assert.Equal(DomainErrorCodes.CashCountDenominationTotalMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public void Disabled_or_unknown_denomination_is_rejected_for_new_counts()
    {
        var enabled = new HashSet<decimal> { 1000m, 500m };
        var ex = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.EnsureConfigured(
            [CashCountDenominationLine.Create(5000m, 1)],
            enabled));
        Assert.Equal(DomainErrorCodes.CashCountDenominationNotConfigured, ex.ErrorCode);
    }

    [Fact]
    public void Historical_breakdown_keeps_original_values_after_config_change()
    {
        var shift = CashierShift.Open(
            Org,
            "SHIFT-20260813-000001",
            Actor,
            Register,
            2000m,
            Now,
            cashCountMode: CashCountMode.Required,
            openingDenominationLines: [CashCountDenominationLine.Create(1000m, 2)]);

        Assert.Equal(2000m, shift.OpeningCashAmount);
        Assert.Equal(1000m, Assert.Single(shift.OpeningDenominationLines).DenominationValue);
        Assert.Equal(2, shift.OpeningDenominationLines[0].Quantity);
    }

    [Fact]
    public void Custom_future_denomination_works_without_hard_coded_ui_list()
    {
        var denom = OrganizationCashDenomination.Create(Org, 5000m, 0, Now);
        Assert.Equal(5000m, denom.Value);
        Assert.True(denom.IsEnabled);
        var total = CashCountDenominationBreakdown.Recalculate([CashCountDenominationLine.Create(5000m, 1)]);
        Assert.Equal(5000m, total);
    }

    [Fact]
    public void Manual_total_has_no_breakdown()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260813-000002", Actor, Register, 1000m, Now);
        Assert.Empty(shift.OpeningDenominationLines);
        shift.Close(4750m, 1000m, Actor, Now.AddHours(8));
        Assert.Empty(shift.ClosingDenominationLines);
        Assert.Equal(4750m, shift.ClosingCashAmount);
        Assert.Equal(3750m, shift.CashVarianceAmount);
    }
}
