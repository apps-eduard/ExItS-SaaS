using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Tracks non-damage "Other" exception quantity custody after a transfer receive wave
/// (wrong item/variant, expired, packaging/quality, other).
/// Held goods are non-sellable (inspection hold). Wrong item/variant store the actual SKU.
/// </summary>
public sealed class InventoryTransferExceptionCustody
{
    public InventoryTransferExceptionCustodyId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public InventoryTransferId TransferId { get; }
    public InventoryTransferId RootTransferId { get; }
    public InventoryTransferReceiptLineId ReceiptLineId { get; }
    public CatalogProductId ExpectedProductId { get; }
    public CatalogProductId ActualProductId { get; }
    public decimal Quantity { get; }
    public string ReasonCode { get; }
    public InventoryTransferExceptionCustodyDecision Decision { get; }
    public InventoryTransferDiscrepancyFollowUp FollowUpIntent { get; }
    public InventoryTransferExceptionCustodyStatus Status { get; private set; }
    public PosBranchId HeldBranchId { get; private set; }
    public decimal RecoveredSellableQty { get; private set; }
    public decimal ConfirmedNonSellableQty { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ReturnDispatchedAtUtc { get; private set; }
    public Guid? ReturnDispatchedBy { get; private set; }
    public DateTimeOffset? ReturnReceivedAtUtc { get; private set; }
    public Guid? ReturnReceivedBy { get; private set; }
    public DateTimeOffset? InspectedAtUtc { get; private set; }
    public Guid? InspectedBy { get; private set; }

    public decimal ReplacementDemandQty =>
        FollowUpIntent == InventoryTransferDiscrepancyFollowUp.AcceptShortage
            ? 0m
            : Quantity;

    private InventoryTransferExceptionCustody(
        InventoryTransferExceptionCustodyId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId expectedProductId,
        CatalogProductId actualProductId,
        decimal quantity,
        string reasonCode,
        InventoryTransferExceptionCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        InventoryTransferExceptionCustodyStatus status,
        PosBranchId heldBranchId,
        decimal recoveredSellableQty,
        decimal confirmedNonSellableQty,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? returnDispatchedAtUtc,
        Guid? returnDispatchedBy,
        DateTimeOffset? returnReceivedAtUtc,
        Guid? returnReceivedBy,
        DateTimeOffset? inspectedAtUtc,
        Guid? inspectedBy)
    {
        Id = id;
        OrganizationId = organizationId;
        TransferId = transferId;
        RootTransferId = rootTransferId;
        ReceiptLineId = receiptLineId;
        ExpectedProductId = expectedProductId;
        ActualProductId = actualProductId;
        Quantity = quantity;
        ReasonCode = reasonCode;
        Decision = decision;
        FollowUpIntent = followUpIntent;
        Status = status;
        HeldBranchId = heldBranchId;
        RecoveredSellableQty = recoveredSellableQty;
        ConfirmedNonSellableQty = confirmedNonSellableQty;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAtUtc;
        ReturnDispatchedAtUtc = returnDispatchedAtUtc;
        ReturnDispatchedBy = returnDispatchedBy;
        ReturnReceivedAtUtc = returnReceivedAtUtc;
        ReturnReceivedBy = returnReceivedBy;
        InspectedAtUtc = inspectedAtUtc;
        InspectedBy = inspectedBy;
    }

    public static InventoryTransferExceptionCustody Open(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId expectedProductId,
        CatalogProductId actualProductId,
        decimal quantity,
        string reasonCode,
        InventoryTransferExceptionCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        PosBranchId destinationBranchId,
        Guid createdBy,
        DateTimeOffset utcNow,
        InventoryTransferExceptionCustodyId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (createdBy == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleActor, "A non-empty actor identifier is required.");
        }

        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Exception custody quantity must be greater than zero.");
        }

        if (!ReceiveDiscrepancyOtherReason.TryParse(reasonCode, out var normalizedReason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferOtherReason,
                "Other discrepancy reason is not recognized.");
        }

        decision = TransferExceptionCustodyPolicy.ResolveDecision(normalizedReason, decision);

        var status = decision == InventoryTransferExceptionCustodyDecision.ReturnToSource
            ? InventoryTransferExceptionCustodyStatus.AwaitingReturn
            : InventoryTransferExceptionCustodyStatus.HeldAtDestination;

        return new InventoryTransferExceptionCustody(
            id ?? InventoryTransferExceptionCustodyId.New(),
            organizationId,
            transferId,
            rootTransferId,
            receiptLineId,
            expectedProductId,
            actualProductId,
            quantity,
            normalizedReason,
            decision,
            followUpIntent,
            status,
            destinationBranchId,
            recoveredSellableQty: 0m,
            confirmedNonSellableQty: 0m,
            utcNow,
            createdBy,
            utcNow,
            returnDispatchedAtUtc: null,
            returnDispatchedBy: null,
            returnReceivedAtUtc: null,
            returnReceivedBy: null,
            inspectedAtUtc: null,
            inspectedBy: null);
    }

    public void MarkReturnDispatched(Guid actorId, DateTimeOffset utcNow, PosBranchId sourceBranchId)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Decision != InventoryTransferExceptionCustodyDecision.ReturnToSource)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Only return-to-source exception custody can be dispatched back to source.");
        }

        if (Status is not (InventoryTransferExceptionCustodyStatus.AwaitingReturn
            or InventoryTransferExceptionCustodyStatus.HeldAtDestination))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Exception return can only be dispatched from awaiting-return custody.");
        }

        Status = InventoryTransferExceptionCustodyStatus.ReturnInTransit;
        HeldBranchId = sourceBranchId;
        ReturnDispatchedAtUtc = utcNow;
        ReturnDispatchedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkReturnReceivedAtSource(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferExceptionCustodyStatus.ReturnInTransit)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Only in-transit exception returns can be received at source.");
        }

        Status = InventoryTransferExceptionCustodyStatus.ReceivedAtSource;
        ReturnReceivedAtUtc = utcNow;
        ReturnReceivedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkReadyForSourceInspection(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferExceptionCustodyStatus.ReceivedAtSource)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Only exception received at source can move to source inspection.");
        }

        Status = InventoryTransferExceptionCustodyStatus.AwaitingInspection;
        UpdatedAtUtc = utcNow;
    }

    public void Inspect(
        decimal recoveredSellableQty,
        decimal confirmedNonSellableQty,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (TransferExceptionCustodyPolicy.RestoresDirectlyToSellableOnSourceReceive(ReasonCode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Wrong item or wrong variant returns do not use source inspection; they restore sellable stock on receive.");
        }

        if (Status is not (
            InventoryTransferExceptionCustodyStatus.AwaitingInspection
            or InventoryTransferExceptionCustodyStatus.ReceivedAtSource))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                "Exception custody is not ready for source inspection.");
        }

        if (recoveredSellableQty < 0m || confirmedNonSellableQty < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Inspection quantities cannot be negative.");
        }

        if (recoveredSellableQty + confirmedNonSellableQty != Quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Recovered + confirmed non-sellable must equal the custody quantity.");
        }

        RecoveredSellableQty = recoveredSellableQty;
        ConfirmedNonSellableQty = confirmedNonSellableQty;
        Status = InventoryTransferExceptionCustodyStatus.Inspected;
        InspectedAtUtc = utcNow;
        InspectedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public static InventoryTransferExceptionCustody Rehydrate(
        InventoryTransferExceptionCustodyId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId expectedProductId,
        CatalogProductId actualProductId,
        decimal quantity,
        string reasonCode,
        InventoryTransferExceptionCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        InventoryTransferExceptionCustodyStatus status,
        PosBranchId heldBranchId,
        decimal recoveredSellableQty,
        decimal confirmedNonSellableQty,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? returnDispatchedAtUtc,
        Guid? returnDispatchedBy,
        DateTimeOffset? returnReceivedAtUtc,
        Guid? returnReceivedBy,
        DateTimeOffset? inspectedAtUtc,
        Guid? inspectedBy) =>
        new(
            id,
            organizationId,
            transferId,
            rootTransferId,
            receiptLineId,
            expectedProductId,
            actualProductId,
            quantity,
            reasonCode,
            decision,
            followUpIntent,
            status,
            heldBranchId,
            recoveredSellableQty,
            confirmedNonSellableQty,
            createdAtUtc,
            createdBy,
            updatedAtUtc,
            returnDispatchedAtUtc,
            returnDispatchedBy,
            returnReceivedAtUtc,
            returnReceivedBy,
            inspectedAtUtc,
            inspectedBy);

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleActor, "A non-empty actor identifier is required.");
        }
    }
}

public readonly record struct InventoryTransferExceptionCustodyId(Guid Value)
{
    public static InventoryTransferExceptionCustodyId New() => new(Guid.NewGuid());

    public static InventoryTransferExceptionCustodyId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                "Exception custody id is required.");
        }

        return new InventoryTransferExceptionCustodyId(value);
    }
}
