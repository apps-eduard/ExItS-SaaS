using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Semantic bucket effects for a stock-movement type (physical / sellable / damaged / inspection hold).
/// </summary>
public readonly record struct StockMovementBucketEffects(
    decimal PhysicalDelta,
    decimal SellableDelta,
    decimal DamagedDelta,
    decimal InspectionHoldDelta);

/// <summary>
/// Friendly display labels for stock-movement codes. Persistence and API values stay on
/// <see cref="StockMovementType"/> member names.
/// </summary>
public static class StockMovementPresentation
{
    public static string ToFriendlyLabel(string? movementTypeCode)
    {
        if (string.IsNullOrWhiteSpace(movementTypeCode))
        {
            return string.Empty;
        }

        if (!StockMovementTypes.TryParse(movementTypeCode, out var type))
        {
            return movementTypeCode.Trim();
        }

        return type switch
        {
            StockMovementType.ManualIncrease => "Stock added",
            StockMovementType.ManualDecrease => "Stock removed",
            StockMovementType.OpeningStock => "Opening stock",
            StockMovementType.SaleDeduction => "Sold",
            StockMovementType.SaleVoidRestoration => "Sale voided",
            StockMovementType.PurchaseReceipt => "Purchase received",
            StockMovementType.StockCountVarianceIncrease => "Count increase",
            StockMovementType.StockCountVarianceDecrease => "Count decrease",
            StockMovementType.SaleReturnRestock => "Return restocked",
            StockMovementType.TransferOut => "Transfer out",
            StockMovementType.TransferIn => "Transfer in (good)",
            StockMovementType.TransferCancelRestore => "Transfer cancelled",
            StockMovementType.TransferDamageHold => "Damaged transfer received",
            StockMovementType.TransferDamageRecovery => "Returned damage recovered",
            StockMovementType.TransferDamageReturnOut => "Damaged returned to source",
            StockMovementType.TransferDamageReturnIn => "Damaged return received",
            StockMovementType.TransferDamageWriteOff => "Confirmed damaged",
            StockMovementType.TransferExceptionHold => "Exception transfer received",
            StockMovementType.TransferExceptionExpectedRestore => "Exception expected restore",
            StockMovementType.TransferExceptionActualOut => "Exception actual out",
            StockMovementType.TransferExceptionReturnOut => "Exception returned to source",
            StockMovementType.TransferExceptionReturnIn => "Exception return received",
            StockMovementType.TransferExceptionReturnRestock => "Wrong item return received",
            StockMovementType.TransferExceptionRecovery => "Returned exception recovered",
            StockMovementType.TransferExceptionWriteOff => "Confirmed non-sellable exception",
            StockMovementType.DirectPurchaseReceipt => "Direct purchase",
            StockMovementType.ExpirationInitialization => "Expiration initialization",
            StockMovementType.StockUse => "Stock use",
            StockMovementType.StockUseVoidRestoration => "Stock use voided",
            StockMovementType.ProductionMaterialConsumption => "Production material used",
            StockMovementType.ProductionMaterialRestoration => "Production material restored",
            StockMovementType.ProductionOutput => "Production output",
            StockMovementType.ProductionOutputReversal => "Production output reversed",
            StockMovementType.WasteLoss => "Waste/loss",
            StockMovementType.WasteLossVoidRestoration => "Waste/loss voided",
            StockMovementType.PurchaseReceiptReversal => "Purchase receipt reversed",
            StockMovementType.DirectPurchaseReceiptReversal => "Direct purchase reversed",
            StockMovementType.ConnectedPurchaseFulfillment => "Connected PO fulfillment",
            StockMovementType.ConnectedPurchaseFulfillmentReconciliation => "Connected PO fulfillment reconciliation",
            _ => type.ToString()
        };
    }

    /// <summary>
    /// Persisted on <see cref="StockMovementType.TransferDamageHold"/> reason so history and drawer
    /// show custody + replacement without re-inferring from status.
    /// </summary>
    public static string FormatDamageHoldDecisionDetail(
        InventoryTransferDamagedCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUp)
    {
        var custody = decision == InventoryTransferDamagedCustodyDecision.ReturnToSource
            ? "Return to source"
            : "Keep at destination";
        var replacement = followUp == InventoryTransferDiscrepancyFollowUp.RequestReplacement
            ? "Replacement requested"
            : "Accepted — no replacement";
        return $"{custody} · {replacement}";
    }

    public static string FormatExceptionHoldDecisionDetail(
        InventoryTransferExceptionCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUp,
        string reasonCode)
    {
        var custody = decision == InventoryTransferExceptionCustodyDecision.ReturnToSource
            ? "Return to source"
            : "Keep at destination";
        var replacement = followUp == InventoryTransferDiscrepancyFollowUp.RequestReplacement
            ? "Replacement requested"
            : "Accepted — no replacement";
        return $"{reasonCode} · {custody} · {replacement}";
    }

    /// <summary>
    /// Bucket semantics for transfer (and damage) movements so UI never treats
    /// <see cref="StockMovementType.TransferDamageHold"/> as +sellable.
    /// <paramref name="signedQuantityEffect"/> is the persisted movement quantity effect.
    /// </summary>
    public static StockMovementBucketEffects DescribeBucketEffects(
        string? movementTypeCode,
        decimal signedQuantityEffect)
    {
        var abs = Math.Abs(signedQuantityEffect);
        if (!StockMovementTypes.TryParse(movementTypeCode, out var type))
        {
            return new StockMovementBucketEffects(signedQuantityEffect, signedQuantityEffect, 0m, 0m);
        }

        return type switch
        {
            StockMovementType.TransferOut => new(-abs, -abs, 0m, 0m),
            StockMovementType.TransferIn => new(abs, abs, 0m, 0m),
            StockMovementType.TransferCancelRestore => new(abs, abs, 0m, 0m),
            // Physical +qty, sellable +0, damaged +qty
            StockMovementType.TransferDamageHold => new(abs, 0m, abs, 0m),
            // Physical unchanged; inspection hold −qty; sellable +qty
            StockMovementType.TransferDamageRecovery => new(0m, abs, 0m, -abs),
            // Physical −qty, damaged −qty, sellable 0
            StockMovementType.TransferDamageReturnOut => new(-abs, 0m, -abs, 0m),
            // Physical +qty, sellable 0, inspection hold +qty
            StockMovementType.TransferDamageReturnIn => new(abs, 0m, 0m, abs),
            // Physical unchanged; inspection hold −qty; damaged +qty; sellable 0
            StockMovementType.TransferDamageWriteOff => new(0m, 0m, abs, -abs),
            StockMovementType.TransferExceptionHold => new(abs, 0m, 0m, abs),
            StockMovementType.TransferExceptionExpectedRestore => new(abs, abs, 0m, 0m),
            StockMovementType.TransferExceptionActualOut => new(-abs, -abs, 0m, 0m),
            StockMovementType.TransferExceptionReturnOut => new(-abs, 0m, 0m, -abs),
            StockMovementType.TransferExceptionReturnIn => new(abs, 0m, 0m, abs),
            StockMovementType.TransferExceptionReturnRestock => new(abs, abs, 0m, 0m),
            StockMovementType.TransferExceptionRecovery => new(0m, abs, 0m, -abs),
            StockMovementType.TransferExceptionWriteOff => new(0m, 0m, abs, -abs),
            _ => new(signedQuantityEffect, signedQuantityEffect, 0m, 0m)
        };
    }
}
