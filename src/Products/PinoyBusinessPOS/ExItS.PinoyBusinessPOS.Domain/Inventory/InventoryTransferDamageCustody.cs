using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Tracks damaged quantity custody after a transfer receive wave until inspection finalizes
/// recovered sellable vs confirmed damaged outcomes.
/// </summary>
public sealed class InventoryTransferDamageCustody
{
    public InventoryTransferDamageCustodyId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public InventoryTransferId TransferId { get; }
    public InventoryTransferId RootTransferId { get; }
    public InventoryTransferReceiptLineId ReceiptLineId { get; }
    public CatalogProductId ProductId { get; }
    public decimal Quantity { get; }
    public InventoryTransferDamagedCustodyDecision Decision { get; }
    public InventoryTransferDiscrepancyFollowUp FollowUpIntent { get; }
    public InventoryTransferDamageCustodyStatus Status { get; private set; }
    public PosBranchId HeldBranchId { get; private set; }
    public decimal RecoveredSellableQty { get; private set; }
    public decimal ConfirmedDamagedQty { get; private set; }
    public decimal WaivedQty { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ReturnDispatchedAtUtc { get; private set; }
    public Guid? ReturnDispatchedBy { get; private set; }
    public DateTimeOffset? ReturnReceivedAtUtc { get; private set; }
    public Guid? ReturnReceivedBy { get; private set; }
    public DateTimeOffset? InspectedAtUtc { get; private set; }
    public Guid? InspectedBy { get; private set; }

    /// <summary>
    /// Destination-side recovered sellable that counts toward coverage satisfaction.
    /// Source-side recovery never counts as destination fulfillment.
    /// </summary>
    public decimal DestinationRecoveredSellableQty =>
        Decision == InventoryTransferDamagedCustodyDecision.KeepAtDestination
            ? RecoveredSellableQty
            : 0m;

    /// <summary>
    /// Quantity that still needs replacement after inspection (or all qty when returning,
    /// because destination lost physical possession of the entire damaged wave).
    /// </summary>
    public decimal ReplacementDemandQty
    {
        get
        {
            if (FollowUpIntent == InventoryTransferDiscrepancyFollowUp.AcceptShortage)
            {
                return 0m;
            }

            if (Decision == InventoryTransferDamagedCustodyDecision.ReturnToSource)
            {
                // Entire damaged qty left destination; all requires replacement.
                return Quantity - WaivedQty;
            }

            if (Status != InventoryTransferDamageCustodyStatus.Inspected)
            {
                // Held/uninspected keep qty is not yet satisfied and not open-in-transit.
                return Quantity;
            }

            return Math.Max(0m, ConfirmedDamagedQty - WaivedQty);
        }
    }

    private InventoryTransferDamageCustody(
        InventoryTransferDamageCustodyId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId productId,
        decimal quantity,
        InventoryTransferDamagedCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        InventoryTransferDamageCustodyStatus status,
        PosBranchId heldBranchId,
        decimal recoveredSellableQty,
        decimal confirmedDamagedQty,
        decimal waivedQty,
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
        ProductId = productId;
        Quantity = quantity;
        Decision = decision;
        FollowUpIntent = followUpIntent;
        Status = status;
        HeldBranchId = heldBranchId;
        RecoveredSellableQty = recoveredSellableQty;
        ConfirmedDamagedQty = confirmedDamagedQty;
        WaivedQty = waivedQty;
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

    public static InventoryTransferDamageCustody Open(
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId productId,
        decimal quantity,
        InventoryTransferDamagedCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        PosBranchId destinationBranchId,
        Guid createdBy,
        DateTimeOffset utcNow,
        InventoryTransferDamageCustodyId? id = null)
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
                "Damage custody quantity must be greater than zero.");
        }

        var status = decision == InventoryTransferDamagedCustodyDecision.ReturnToSource
            ? InventoryTransferDamageCustodyStatus.AwaitingReturn
            : InventoryTransferDamageCustodyStatus.HeldAtDestination;

        return new InventoryTransferDamageCustody(
            id ?? InventoryTransferDamageCustodyId.New(),
            organizationId,
            transferId,
            rootTransferId,
            receiptLineId,
            productId,
            quantity,
            decision,
            followUpIntent,
            status,
            destinationBranchId,
            recoveredSellableQty: 0m,
            confirmedDamagedQty: 0m,
            waivedQty: 0m,
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
        if (Decision != InventoryTransferDamagedCustodyDecision.ReturnToSource)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Only return-to-source custody can be dispatched back to source.");
        }

        if (Status is not (InventoryTransferDamageCustodyStatus.AwaitingReturn
            or InventoryTransferDamageCustodyStatus.HeldAtDestination))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Damage return can only be dispatched from awaiting-return custody.");
        }

        Status = InventoryTransferDamageCustodyStatus.ReturnInTransit;
        HeldBranchId = sourceBranchId;
        ReturnDispatchedAtUtc = utcNow;
        ReturnDispatchedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkReturnReceivedAtSource(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferDamageCustodyStatus.ReturnInTransit)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Only in-transit damage returns can be received at source.");
        }

        Status = InventoryTransferDamageCustodyStatus.ReceivedAtSource;
        ReturnReceivedAtUtc = utcNow;
        ReturnReceivedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkReadyForSourceInspection(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status != InventoryTransferDamageCustodyStatus.ReceivedAtSource)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Only damage received at source can move to source inspection.");
        }

        Status = InventoryTransferDamageCustodyStatus.AwaitingInspection;
        UpdatedAtUtc = utcNow;
    }

    public void MarkReadyForDestinationInspection(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Decision != InventoryTransferDamagedCustodyDecision.KeepAtDestination)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Only keep-at-destination custody is inspected at destination.");
        }

        if (Status != InventoryTransferDamageCustodyStatus.HeldAtDestination)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Only held damage can move to destination inspection.");
        }

        Status = InventoryTransferDamageCustodyStatus.AwaitingInspection;
        UpdatedAtUtc = utcNow;
    }

    public void Inspect(
        decimal recoveredSellableQty,
        decimal confirmedDamagedQty,
        Guid actorId,
        DateTimeOffset utcNow,
        InventoryTransferDiscrepancyFollowUp? followUpOverride = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (Status is not (
            InventoryTransferDamageCustodyStatus.AwaitingInspection
            or InventoryTransferDamageCustodyStatus.HeldAtDestination
            or InventoryTransferDamageCustodyStatus.ReceivedAtSource))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                "Damage custody is not ready for inspection.");
        }

        if (recoveredSellableQty < 0m || confirmedDamagedQty < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Inspection quantities cannot be negative.");
        }

        if (recoveredSellableQty + confirmedDamagedQty != Quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferQuantity,
                "Recovered + confirmed damaged must equal the custody quantity.");
        }

        var followUp = followUpOverride ?? FollowUpIntent;
        RecoveredSellableQty = recoveredSellableQty;
        ConfirmedDamagedQty = confirmedDamagedQty;
        WaivedQty = followUp == InventoryTransferDiscrepancyFollowUp.AcceptShortage
            ? confirmedDamagedQty
            : 0m;
        Status = InventoryTransferDamageCustodyStatus.Inspected;
        InspectedAtUtc = utcNow;
        InspectedBy = actorId;
        UpdatedAtUtc = utcNow;
    }

    public static InventoryTransferDamageCustody Rehydrate(
        InventoryTransferDamageCustodyId id,
        PosOrganizationId organizationId,
        InventoryTransferId transferId,
        InventoryTransferId rootTransferId,
        InventoryTransferReceiptLineId receiptLineId,
        CatalogProductId productId,
        decimal quantity,
        InventoryTransferDamagedCustodyDecision decision,
        InventoryTransferDiscrepancyFollowUp followUpIntent,
        InventoryTransferDamageCustodyStatus status,
        PosBranchId heldBranchId,
        decimal recoveredSellableQty,
        decimal confirmedDamagedQty,
        decimal waivedQty,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? returnDispatchedAtUtc = null,
        Guid? returnDispatchedBy = null,
        DateTimeOffset? returnReceivedAtUtc = null,
        Guid? returnReceivedBy = null,
        DateTimeOffset? inspectedAtUtc = null,
        Guid? inspectedBy = null) =>
        new(
            id,
            organizationId,
            transferId,
            rootTransferId,
            receiptLineId,
            productId,
            quantity,
            decision,
            followUpIntent,
            status,
            heldBranchId,
            recoveredSellableQty,
            confirmedDamagedQty,
            waivedQty,
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

public readonly record struct InventoryTransferDamageCustodyId(Guid Value)
{
    public static InventoryTransferDamageCustodyId New() => new(Guid.NewGuid());

    public static InventoryTransferDamageCustodyId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                "Damage custody id must be a non-empty GUID.");
        }

        return new InventoryTransferDamageCustodyId(value);
    }

    public override string ToString() => Value.ToString("D");
}
