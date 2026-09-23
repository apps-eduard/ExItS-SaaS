using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockMovementPresentationTests
{
    [Theory]
    [InlineData(nameof(StockMovementType.ManualIncrease), "Stock added")]
    [InlineData(nameof(StockMovementType.ManualDecrease), "Stock removed")]
    [InlineData(nameof(StockMovementType.DirectPurchaseReceipt), "Direct purchase")]
    [InlineData(nameof(StockMovementType.ExpirationInitialization), "Expiration initialization")]
    public void Maps_manual_adjustment_codes_to_friendly_labels(string code, string expected)
    {
        Assert.Equal(expected, StockMovementPresentation.ToFriendlyLabel(code));
    }

    [Fact]
    public void Does_not_change_persistence_enum_names()
    {
        Assert.Equal("ManualIncrease", StockMovementTypes.ToCode(StockMovementType.ManualIncrease));
        Assert.Equal("ManualDecrease", StockMovementTypes.ToCode(StockMovementType.ManualDecrease));
        Assert.True(StockMovementTypes.TryParse("ManualIncrease", out var increase));
        Assert.Equal(StockMovementType.ManualIncrease, increase);
    }

    [Fact]
    public void Unknown_codes_are_returned_unchanged()
    {
        Assert.Equal("CustomType", StockMovementPresentation.ToFriendlyLabel("CustomType"));
        Assert.Equal(string.Empty, StockMovementPresentation.ToFriendlyLabel("  "));
    }

    [Fact]
    public void Damage_hold_is_physical_not_sellable()
    {
        var effects = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferDamageHold),
            5m);
        Assert.Equal(5m, effects.PhysicalDelta);
        Assert.Equal(0m, effects.SellableDelta);
        Assert.Equal(5m, effects.DamagedDelta);
        Assert.Equal("Damaged transfer received", StockMovementPresentation.ToFriendlyLabel("TransferDamageHold"));
        Assert.Equal(
            "Return to source · Replacement requested",
            StockMovementPresentation.FormatDamageHoldDecisionDetail(
                InventoryTransferDamagedCustodyDecision.ReturnToSource,
                InventoryTransferDiscrepancyFollowUp.RequestReplacement));
        Assert.Equal(
            "Keep at destination · Accepted — no replacement",
            StockMovementPresentation.FormatDamageHoldDecisionDetail(
                InventoryTransferDamagedCustodyDecision.KeepAtDestination,
                InventoryTransferDiscrepancyFollowUp.AcceptShortage));
    }

    [Fact]
    public void Damage_return_in_parks_inspection_hold_not_sellable()
    {
        var effects = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferDamageReturnIn),
            5m);
        Assert.Equal(5m, effects.PhysicalDelta);
        Assert.Equal(0m, effects.SellableDelta);
        Assert.Equal(0m, effects.DamagedDelta);
        Assert.Equal(5m, effects.InspectionHoldDelta);
        Assert.Equal(
            "Damaged return received",
            StockMovementPresentation.ToFriendlyLabel("TransferDamageReturnIn"));
    }

    [Fact]
    public void Damage_return_out_clears_destination_damaged_not_sellable()
    {
        var effects = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferDamageReturnOut),
            -5m);
        Assert.Equal(-5m, effects.PhysicalDelta);
        Assert.Equal(0m, effects.SellableDelta);
        Assert.Equal(-5m, effects.DamagedDelta);
        Assert.Equal(0m, effects.InspectionHoldDelta);
        Assert.Equal(
            "Damaged returned to source",
            StockMovementPresentation.ToFriendlyLabel("TransferDamageReturnOut"));
    }

    [Fact]
    public void Damage_recovery_and_write_off_reclassify_inspection_hold()
    {
        var recovered = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferDamageRecovery),
            2m);
        Assert.Equal(0m, recovered.PhysicalDelta);
        Assert.Equal(2m, recovered.SellableDelta);
        Assert.Equal(0m, recovered.DamagedDelta);
        Assert.Equal(-2m, recovered.InspectionHoldDelta);
        Assert.Equal(
            "Returned damage recovered",
            StockMovementPresentation.ToFriendlyLabel("TransferDamageRecovery"));

        var writtenOff = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferDamageWriteOff),
            3m);
        Assert.Equal(0m, writtenOff.PhysicalDelta);
        Assert.Equal(0m, writtenOff.SellableDelta);
        Assert.Equal(3m, writtenOff.DamagedDelta);
        Assert.Equal(-3m, writtenOff.InspectionHoldDelta);
        Assert.Equal(
            "Confirmed damaged",
            StockMovementPresentation.ToFriendlyLabel("TransferDamageWriteOff"));
    }

    [Fact]
    public void Good_transfer_in_is_sellable()
    {
        var effects = StockMovementPresentation.DescribeBucketEffects(
            nameof(StockMovementType.TransferIn),
            5m);
        Assert.Equal(5m, effects.PhysicalDelta);
        Assert.Equal(5m, effects.SellableDelta);
        Assert.Equal(0m, effects.DamagedDelta);
    }
}
