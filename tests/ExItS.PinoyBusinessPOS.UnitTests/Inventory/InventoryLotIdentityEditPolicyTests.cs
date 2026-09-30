using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryLotIdentityEditPolicyTests
{
    [Theory]
    [InlineData(StockMovementType.OpeningStock, false)]
    [InlineData(StockMovementType.ManualIncrease, false)]
    [InlineData(StockMovementType.PurchaseReceipt, false)]
    [InlineData(StockMovementType.DirectPurchaseReceipt, false)]
    [InlineData(StockMovementType.ExpirationInitialization, false)]
    [InlineData(StockMovementType.ProductionOutput, false)]
    [InlineData(StockMovementType.SaleDeduction, true)]
    [InlineData(StockMovementType.SaleVoidRestoration, true)]
    [InlineData(StockMovementType.ManualDecrease, true)]
    [InlineData(StockMovementType.TransferOut, true)]
    [InlineData(StockMovementType.TransferIn, true)]
    [InlineData(StockMovementType.TransferCancelRestore, true)]
    [InlineData(StockMovementType.StockUse, true)]
    [InlineData(StockMovementType.WasteLoss, true)]
    [InlineData(StockMovementType.StockCountVarianceIncrease, true)]
    [InlineData(StockMovementType.StockCountVarianceDecrease, true)]
    [InlineData(StockMovementType.ProductionMaterialConsumption, true)]
    [InlineData(StockMovementType.ProductionMaterialRestoration, true)]
    [InlineData(StockMovementType.ProductionOutputReversal, true)]
    [InlineData(StockMovementType.SaleReturnRestock, true)]
    [InlineData(StockMovementType.TransferDamageHold, true)]
    [InlineData(StockMovementType.TransferExceptionHold, true)]
    [InlineData(StockMovementType.PurchaseReceiptReversal, true)]
    [InlineData(StockMovementType.DirectPurchaseReceiptReversal, true)]
    [InlineData(StockMovementType.ConnectedPurchaseFulfillment, true)]
    [InlineData(StockMovementType.ConnectedPurchaseFulfillmentReconciliation, true)]
    public void Origin_vs_locking_classification(StockMovementType type, bool locks)
    {
        Assert.Equal(!locks, InventoryLotIdentityEditPolicy.IsOriginAcquisitionOnly(type));
        Assert.Equal(locks, InventoryLotIdentityEditPolicy.IsIdentityLockingMovement(type));
    }

    [Fact]
    public void Origin_only_is_editable()
    {
        var reason = InventoryLotIdentityEditPolicy.ResolveLockReason(
            [StockMovementType.PurchaseReceipt, StockMovementType.ManualIncrease],
            hasActiveTransferDraft: false);
        Assert.Equal(InventoryLotIdentityEditPolicy.LockReasonNone, reason);
        Assert.True(InventoryLotIdentityEditPolicy.CanEditIdentity(reason));
    }

    [Fact]
    public void Sale_and_void_lock_as_used()
    {
        var reason = InventoryLotIdentityEditPolicy.ResolveLockReason(
            [StockMovementType.PurchaseReceipt, StockMovementType.SaleDeduction, StockMovementType.SaleVoidRestoration],
            hasActiveTransferDraft: false);
        Assert.Equal(InventoryLotIdentityEditPolicy.LockReasonUsed, reason);
    }

    [Fact]
    public void Transfer_out_and_cancel_lock_as_transferred()
    {
        var reason = InventoryLotIdentityEditPolicy.ResolveLockReason(
            [StockMovementType.OpeningStock, StockMovementType.TransferOut, StockMovementType.TransferCancelRestore],
            hasActiveTransferDraft: false);
        Assert.Equal(InventoryLotIdentityEditPolicy.LockReasonTransferred, reason);
    }

    [Fact]
    public void Transfer_in_locks_as_received_from_transfer()
    {
        var reason = InventoryLotIdentityEditPolicy.ResolveLockReason(
            [StockMovementType.TransferIn],
            hasActiveTransferDraft: false);
        Assert.Equal(InventoryLotIdentityEditPolicy.LockReasonReceivedFromTransfer, reason);
    }

    [Fact]
    public void Active_transfer_draft_locks_even_without_downstream_movements()
    {
        var reason = InventoryLotIdentityEditPolicy.ResolveLockReason(
            [StockMovementType.DirectPurchaseReceipt],
            hasActiveTransferDraft: true);
        Assert.Equal(InventoryLotIdentityEditPolicy.LockReasonActiveTransferDraft, reason);
    }

    [Fact]
    public void Unknown_non_acquisition_defaults_to_locking()
    {
        // Any enum outside the acquisition allow-list locks (fail-closed).
        Assert.True(InventoryLotIdentityEditPolicy.IsIdentityLockingMovement(StockMovementType.ConnectedPoReturnDispatch));
    }
}
