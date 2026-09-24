using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Authoritative custody rules for transfer "Other" discrepancy reasons.
/// Follow-up (replacement vs accept shortage) remains independent of custody.
/// </summary>
public static class TransferExceptionCustodyPolicy
{
    public static bool RequiresActualProduct(string reasonCode) =>
        reasonCode is ReceiveDiscrepancyOtherReason.WrongItem
            or ReceiveDiscrepancyOtherReason.WrongVariant;

    /// <summary>
    /// Wrong item/variant returns restore directly to source sellable stock (no inspection).
    /// </summary>
    public static bool RestoresDirectlyToSellableOnSourceReceive(string reasonCode) =>
        RequiresActualProduct(reasonCode);

    public static bool ForcesReturnToSource(string reasonCode) =>
        reasonCode is ReceiveDiscrepancyOtherReason.WrongItem
            or ReceiveDiscrepancyOtherReason.WrongVariant
            or ReceiveDiscrepancyOtherReason.Expired;

    public static bool AllowsKeepAtDestination(string reasonCode) =>
        reasonCode is ReceiveDiscrepancyOtherReason.PackagingIssue
            or ReceiveDiscrepancyOtherReason.QualityIssue
            or ReceiveDiscrepancyOtherReason.Other;

    public static bool RequiresDescription(string reasonCode) =>
        string.Equals(reasonCode, ReceiveDiscrepancyOtherReason.Other, StringComparison.Ordinal);

    /// <summary>
    /// Resolves custody for an other-qty wave. Throws when Keep is requested for locked reasons
    /// or when a choosable reason omits a decision.
    /// </summary>
    public static InventoryTransferExceptionCustodyDecision ResolveDecision(
        string reasonCode,
        InventoryTransferExceptionCustodyDecision? requested)
    {
        if (ForcesReturnToSource(reasonCode))
        {
            if (requested is InventoryTransferExceptionCustodyDecision.KeepAtDestination)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidInventoryTransferExceptionCustodyDecision,
                    $"{reasonCode} requires return to source; keep at destination is not allowed.");
            }

            return InventoryTransferExceptionCustodyDecision.ReturnToSource;
        }

        if (!AllowsKeepAtDestination(reasonCode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferOtherReason,
                $"Unknown other discrepancy reason '{reasonCode}'.");
        }

        if (requested is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyDecision,
                "Exception custody decision is required for this other reason.");
        }

        return requested.Value;
    }
}
