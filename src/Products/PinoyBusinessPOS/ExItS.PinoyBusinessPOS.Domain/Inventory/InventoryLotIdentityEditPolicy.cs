namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Canonical server-side policy for whether a lot's expiration/lot-number identity may be corrected.
/// Distinguishes origin/acquisition movements from downstream/operational movements. Fail-closed:
/// any movement type not explicitly acquisition-only locks identity.
/// </summary>
public static class InventoryLotIdentityEditPolicy
{
    public const string LockReasonNone = "None";
    public const string LockReasonUsed = "Used";
    public const string LockReasonTransferred = "Transferred";
    public const string LockReasonActiveTransferDraft = "ActiveTransferDraft";
    public const string LockReasonReceivedFromTransfer = "ReceivedFromTransfer";
    public const string LockReasonReferencedByDocument = "ReferencedByDocument";

    /// <summary>
    /// Origin/acquisition movements that alone do not lock identity (correction still allowed).
    /// TransferIn is intentionally excluded — destination identity must stay traceable.
    /// </summary>
    public static bool IsOriginAcquisitionOnly(StockMovementType movementType) =>
        movementType is StockMovementType.OpeningStock
            or StockMovementType.ManualIncrease
            or StockMovementType.PurchaseReceipt
            or StockMovementType.DirectPurchaseReceipt
            or StockMovementType.ExpirationInitialization
            or StockMovementType.ProductionOutput;

    /// <summary>
    /// Any meaningful downstream / non-acquisition movement locks identity permanently for normal edit.
    /// Unknown types lock (fail-closed).
    /// </summary>
    public static bool IsIdentityLockingMovement(StockMovementType movementType) =>
        !IsOriginAcquisitionOnly(movementType);

    public static string ResolveLockReason(
        IEnumerable<StockMovementType> movementTypes,
        bool hasActiveTransferDraft,
        bool hasOtherActiveDocumentReference = false)
    {
        var types = movementTypes as ICollection<StockMovementType> ?? movementTypes.ToList();

        if (types.Contains(StockMovementType.TransferIn))
        {
            return LockReasonReceivedFromTransfer;
        }

        if (hasActiveTransferDraft)
        {
            return LockReasonActiveTransferDraft;
        }

        if (hasOtherActiveDocumentReference)
        {
            return LockReasonReferencedByDocument;
        }

        if (types.Contains(StockMovementType.TransferOut)
            || types.Contains(StockMovementType.TransferCancelRestore))
        {
            return LockReasonTransferred;
        }

        if (types.Any(IsIdentityLockingMovement))
        {
            return LockReasonUsed;
        }

        return LockReasonNone;
    }

    public static bool CanEditIdentity(string lockReason) =>
        string.Equals(lockReason, LockReasonNone, StringComparison.Ordinal);
}
