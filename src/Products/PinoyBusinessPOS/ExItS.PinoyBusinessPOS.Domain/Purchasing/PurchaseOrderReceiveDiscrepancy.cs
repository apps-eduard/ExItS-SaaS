using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Authoritative receive discrepancy classification rules.
/// Damaged + NotDelivered (Rejected) must equal Outstanding − Good when Good &lt; Outstanding.
/// Short-close is the cancel-remaining decision and does not replace classification.
/// </summary>
public static class PurchaseOrderReceiveDiscrepancy
{
    public const string RemainingActionDeliverLater = "DeliverLater";
    public const string RemainingActionCancelRemaining = "CancelRemaining";

    public static void EnsureValid(
        decimal outstandingBeforeReceive,
        PurchaseOrderReceiveLineDraft receive,
        UnitOfMeasure uom,
        SellingMode sellingMode)
    {
        if (outstandingBeforeReceive < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Outstanding quantity cannot be negative.");
        }

        var good = Normalize(receive.ReceiveQty, uom, sellingMode);
        var damaged = Normalize(receive.DamagedQty, uom, sellingMode);
        var notDelivered = Normalize(receive.RejectedQty, uom, sellingMode);
        var shortClosed = Normalize(receive.ShortClosedQty, uom, sellingMode);

        if (good < 0m || damaged < 0m || notDelivered < 0m || shortClosed < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantities cannot be negative.");
        }

        if (good > outstandingBeforeReceive)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOverReceipt,
                "Good received cannot exceed outstanding quantity.");
        }

        var discrepancy = outstandingBeforeReceive - good;
        if (discrepancy <= 0m)
        {
            if (damaged > 0m || notDelivered > 0m || shortClosed > 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                    "Discrepancy classification is only allowed when good received is less than outstanding.");
            }

            return;
        }

        if (damaged + notDelivered != discrepancy)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Damaged plus not delivered must equal the discrepancy quantity.");
        }

        if (shortClosed > 0m && shortClosed != discrepancy)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Cancel remaining must short-close the full discrepancy quantity.");
        }

        if (receive.DiscrepancyKind == ConnectedPoReceivingDiscrepancyKind.None
            && (damaged > 0m || notDelivered > 0m))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Discrepancy kind is required when classifying a short receipt.");
        }
    }

    public static string? ResolveRemainingAction(decimal shortClosedQty, decimal damagedQty, decimal rejectedQty)
    {
        if (shortClosedQty > 0m)
        {
            return RemainingActionCancelRemaining;
        }

        if (damagedQty > 0m || rejectedQty > 0m)
        {
            return RemainingActionDeliverLater;
        }

        return null;
    }

    public static ConnectedPoReceivingDiscrepancyKind ResolveKind(decimal damagedQty, decimal notDeliveredQty)
    {
        if (damagedQty > 0m && notDeliveredQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Other;
        }

        if (damagedQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Damaged;
        }

        if (notDeliveredQty > 0m)
        {
            return ConnectedPoReceivingDiscrepancyKind.Short;
        }

        return ConnectedPoReceivingDiscrepancyKind.None;
    }

    private static decimal Normalize(decimal qty, UnitOfMeasure uom, SellingMode sellingMode)
    {
        if (qty <= 0m)
        {
            return 0m;
        }

        return PurchaseOrderLine.NormalizeQuantity(qty, uom, sellingMode);
    }
}
