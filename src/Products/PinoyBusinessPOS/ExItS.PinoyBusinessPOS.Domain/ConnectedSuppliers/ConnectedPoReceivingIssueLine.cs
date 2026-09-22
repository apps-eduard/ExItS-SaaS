using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// One immutable buyer-discrepancy line awaiting or holding a seller resolution.
/// Buyer quantities are evidence snapshots; seller resolution is append-only via <see cref="Resolve"/>.
/// </summary>
public sealed class ConnectedPoReceivingIssueLine
{
    public const int NameMaxLength = 200;
    public const int NoteMaxLength = 500;
    public const int DiscrepancyNoteMaxLength = 280;

    public ConnectedPoReceivingIssueLineId Id { get; }
    public ConnectedPoReceivingIssueId ReceivingIssueId { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public GoodsReceiptLineId GoodsReceiptLineId { get; }
    public PurchaseOrderLineId PurchaseOrderLineId { get; }
    public CatalogProductId SupplierProductId { get; }
    public CatalogProductId? BuyerProductId { get; }
    public string NameSnapshot { get; }
    public string UomSnapshot { get; }
    public Guid FulfillmentSourceId { get; }
    public decimal ShippedQty { get; }
    public decimal GoodQty { get; }
    public decimal DamagedQty { get; }
    public decimal MissingQty { get; }
    public ConnectedPoReceivingIssueLineKind LineKind { get; }
    public ConnectedPoReceivingDiscrepancyKind BuyerDiscrepancyKind { get; }
    public string? BuyerDiscrepancyNote { get; }
    public ConnectedPoMissingResolution? MissingResolution { get; private set; }
    public ConnectedPoDamagedResolution? DamagedResolution { get; private set; }
    public decimal ResolutionQty { get; private set; }
    public string? SellerNote { get; private set; }
    public Guid? InventoryMovementId { get; private set; }
    public Guid? ReturnBatchId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }

    public bool IsResolved => ResolvedAtUtc is not null;

    /// <summary>Quantity of this line that is in dispute (missing or damaged).</summary>
    public decimal IssueQty =>
        LineKind == ConnectedPoReceivingIssueLineKind.Missing ? MissingQty : DamagedQty;

    private ConnectedPoReceivingIssueLine(
        ConnectedPoReceivingIssueLineId id,
        ConnectedPoReceivingIssueId receivingIssueId,
        PosOrganizationId sellerOrganizationId,
        GoodsReceiptLineId goodsReceiptLineId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId supplierProductId,
        CatalogProductId? buyerProductId,
        string nameSnapshot,
        string uomSnapshot,
        Guid fulfillmentSourceId,
        decimal shippedQty,
        decimal goodQty,
        decimal damagedQty,
        decimal missingQty,
        ConnectedPoReceivingIssueLineKind lineKind,
        ConnectedPoReceivingDiscrepancyKind buyerDiscrepancyKind,
        string? buyerDiscrepancyNote,
        ConnectedPoMissingResolution? missingResolution,
        ConnectedPoDamagedResolution? damagedResolution,
        decimal resolutionQty,
        string? sellerNote,
        Guid? inventoryMovementId,
        Guid? returnBatchId,
        DateTimeOffset? resolvedAtUtc,
        Guid? resolvedByUserId)
    {
        Id = id;
        ReceivingIssueId = receivingIssueId;
        SellerOrganizationId = sellerOrganizationId;
        GoodsReceiptLineId = goodsReceiptLineId;
        PurchaseOrderLineId = purchaseOrderLineId;
        SupplierProductId = supplierProductId;
        BuyerProductId = buyerProductId;
        NameSnapshot = nameSnapshot;
        UomSnapshot = uomSnapshot;
        FulfillmentSourceId = fulfillmentSourceId;
        ShippedQty = shippedQty;
        GoodQty = goodQty;
        DamagedQty = damagedQty;
        MissingQty = missingQty;
        LineKind = lineKind;
        BuyerDiscrepancyKind = buyerDiscrepancyKind;
        BuyerDiscrepancyNote = buyerDiscrepancyNote;
        MissingResolution = missingResolution;
        DamagedResolution = damagedResolution;
        ResolutionQty = resolutionQty;
        SellerNote = sellerNote;
        InventoryMovementId = inventoryMovementId;
        ReturnBatchId = returnBatchId;
        ResolvedAtUtc = resolvedAtUtc;
        ResolvedByUserId = resolvedByUserId;
    }

    internal static ConnectedPoReceivingIssueLine Create(
        ConnectedPoReceivingIssueId receivingIssueId,
        PosOrganizationId sellerOrganizationId,
        GoodsReceiptLineId goodsReceiptLineId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId supplierProductId,
        CatalogProductId? buyerProductId,
        string nameSnapshot,
        string uomSnapshot,
        Guid fulfillmentSourceId,
        decimal shippedQty,
        decimal goodQty,
        decimal damagedQty,
        decimal missingQty,
        ConnectedPoReceivingIssueLineKind lineKind,
        ConnectedPoReceivingDiscrepancyKind buyerDiscrepancyKind,
        string? buyerDiscrepancyNote)
    {
        if (fulfillmentSourceId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueFulfillmentSource,
                "Fulfillment source id is required.");
        }

        if (lineKind == ConnectedPoReceivingIssueLineKind.Missing && missingQty <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLine,
                "Missing issue lines require missing quantity greater than zero.");
        }

        if (lineKind == ConnectedPoReceivingIssueLineKind.Damaged && damagedQty <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLine,
                "Damaged issue lines require damaged quantity greater than zero.");
        }

        var name = (nameSnapshot ?? string.Empty).Trim();
        if (name.Length == 0 || name.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLine,
                $"Product name snapshot must be 1–{NameMaxLength} characters.");
        }

        string? note = null;
        if (!string.IsNullOrWhiteSpace(buyerDiscrepancyNote))
        {
            note = buyerDiscrepancyNote.Trim();
            if (note.Length > DiscrepancyNoteMaxLength)
            {
                note = note[..DiscrepancyNoteMaxLength];
            }
        }

        return new ConnectedPoReceivingIssueLine(
            ConnectedPoReceivingIssueLineId.New(),
            receivingIssueId,
            sellerOrganizationId,
            goodsReceiptLineId,
            purchaseOrderLineId,
            supplierProductId,
            buyerProductId,
            name,
            (uomSnapshot ?? string.Empty).Trim(),
            fulfillmentSourceId,
            shippedQty,
            goodQty,
            damagedQty,
            missingQty,
            lineKind,
            buyerDiscrepancyKind,
            note,
            missingResolution: null,
            damagedResolution: null,
            resolutionQty: 0m,
            sellerNote: null,
            inventoryMovementId: null,
            returnBatchId: null,
            resolvedAtUtc: null,
            resolvedByUserId: null);
    }

    /// <summary>
    /// Applies a seller resolution. Inventory effect is computed by the application layer from the resolution.
    /// Idempotent: re-resolving an already-resolved line with the same decision is a no-op success.
    /// </summary>
    public void ResolveMissing(
        ConnectedPoMissingResolution resolution,
        decimal resolutionQty,
        string? sellerNote,
        Guid actorId,
        DateTimeOffset utcNow,
        Guid? inventoryMovementId,
        Guid? returnBatchId = null)
    {
        EnsureNotAlreadyResolvedDifferently(
            ConnectedPoReceivingIssueLineKind.Missing,
            ConnectedPoMissingResolutions.ToCode(resolution),
            resolutionQty,
            sellerNote,
            inventoryMovementId,
            returnBatchId);

        if (IsResolved)
        {
            return;
        }

        if (LineKind != ConnectedPoReceivingIssueLineKind.Missing)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueResolutionMismatch,
                "Cannot apply a missing resolution to a damaged issue line.");
        }

        if (resolutionQty <= 0m || resolutionQty > MissingQty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueResolutionQty,
                "Resolution quantity must be between 0 (exclusive) and the missing quantity.");
        }

        EnsureActorAndNote(resolution == ConnectedPoMissingResolution.Other, sellerNote, actorId, utcNow);

        MissingResolution = resolution;
        DamagedResolution = null;
        ResolutionQty = resolutionQty;
        SellerNote = NormalizeSellerNote(sellerNote);
        InventoryMovementId = inventoryMovementId;
        ReturnBatchId = returnBatchId;
        ResolvedAtUtc = utcNow;
        ResolvedByUserId = actorId;
    }

    public void ResolveDamaged(
        ConnectedPoDamagedResolution resolution,
        decimal resolutionQty,
        string? sellerNote,
        Guid actorId,
        DateTimeOffset utcNow,
        Guid? inventoryMovementId,
        Guid? returnBatchId = null)
    {
        EnsureNotAlreadyResolvedDifferently(
            ConnectedPoReceivingIssueLineKind.Damaged,
            ConnectedPoDamagedResolutions.ToCode(resolution),
            resolutionQty,
            sellerNote,
            inventoryMovementId,
            returnBatchId);

        if (IsResolved)
        {
            return;
        }

        if (LineKind != ConnectedPoReceivingIssueLineKind.Damaged)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueResolutionMismatch,
                "Cannot apply a damaged resolution to a missing issue line.");
        }

        if (resolutionQty <= 0m || resolutionQty > DamagedQty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueResolutionQty,
                "Resolution quantity must be between 0 (exclusive) and the damaged quantity.");
        }

        EnsureActorAndNote(resolution == ConnectedPoDamagedResolution.Other, sellerNote, actorId, utcNow);

        DamagedResolution = resolution;
        MissingResolution = null;
        ResolutionQty = resolutionQty;
        SellerNote = NormalizeSellerNote(sellerNote);
        InventoryMovementId = inventoryMovementId;
        ReturnBatchId = returnBatchId;
        ResolvedAtUtc = utcNow;
        ResolvedByUserId = actorId;
    }

    private void EnsureNotAlreadyResolvedDifferently(
        ConnectedPoReceivingIssueLineKind expectedKind,
        string resolutionCode,
        decimal resolutionQty,
        string? sellerNote,
        Guid? inventoryMovementId,
        Guid? returnBatchId)
    {
        if (!IsResolved)
        {
            return;
        }

        if (LineKind != expectedKind)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueAlreadyResolved,
                "Receiving issue line is already resolved.");
        }

        var existingCode = LineKind == ConnectedPoReceivingIssueLineKind.Missing
            ? (MissingResolution is null ? null : ConnectedPoMissingResolutions.ToCode(MissingResolution.Value))
            : (DamagedResolution is null ? null : ConnectedPoDamagedResolutions.ToCode(DamagedResolution.Value));

        var same =
            string.Equals(existingCode, resolutionCode, StringComparison.Ordinal)
            && ResolutionQty == resolutionQty
            && string.Equals(SellerNote, NormalizeSellerNote(sellerNote), StringComparison.Ordinal)
            && InventoryMovementId == inventoryMovementId
            && ReturnBatchId == returnBatchId;

        if (!same)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueAlreadyResolved,
                "Receiving issue line is already resolved with a different decision.");
        }
    }

    private static void EnsureActorAndNote(bool noteRequired, string? sellerNote, Guid actorId, DateTimeOffset utcNow)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidActorId,
                "Actor id is required.");
        }

        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "ResolvedAtUtc must be UTC.");
        }

        if (noteRequired && string.IsNullOrWhiteSpace(sellerNote))
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueSellerNoteRequired,
                "A seller note is required for Other resolutions.");
        }
    }

    private static string? NormalizeSellerNote(string? sellerNote)
    {
        if (string.IsNullOrWhiteSpace(sellerNote))
        {
            return null;
        }

        var trimmed = sellerNote.Trim();
        if (trimmed.Length > NoteMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueSellerNote,
                $"Seller note must be at most {NoteMaxLength} characters.");
        }

        return trimmed;
    }

    public static ConnectedPoReceivingIssueLine Rehydrate(
        ConnectedPoReceivingIssueLineId id,
        ConnectedPoReceivingIssueId receivingIssueId,
        PosOrganizationId sellerOrganizationId,
        GoodsReceiptLineId goodsReceiptLineId,
        PurchaseOrderLineId purchaseOrderLineId,
        CatalogProductId supplierProductId,
        CatalogProductId? buyerProductId,
        string nameSnapshot,
        string uomSnapshot,
        Guid fulfillmentSourceId,
        decimal shippedQty,
        decimal goodQty,
        decimal damagedQty,
        decimal missingQty,
        ConnectedPoReceivingIssueLineKind lineKind,
        ConnectedPoReceivingDiscrepancyKind buyerDiscrepancyKind,
        string? buyerDiscrepancyNote,
        ConnectedPoMissingResolution? missingResolution,
        ConnectedPoDamagedResolution? damagedResolution,
        decimal resolutionQty,
        string? sellerNote,
        Guid? inventoryMovementId,
        Guid? returnBatchId,
        DateTimeOffset? resolvedAtUtc,
        Guid? resolvedByUserId) =>
        new(
            id,
            receivingIssueId,
            sellerOrganizationId,
            goodsReceiptLineId,
            purchaseOrderLineId,
            supplierProductId,
            buyerProductId,
            nameSnapshot,
            uomSnapshot,
            fulfillmentSourceId,
            shippedQty,
            goodQty,
            damagedQty,
            missingQty,
            lineKind,
            buyerDiscrepancyKind,
            buyerDiscrepancyNote,
            missingResolution,
            damagedResolution,
            resolutionQty,
            sellerNote,
            inventoryMovementId,
            returnBatchId,
            resolvedAtUtc,
            resolvedByUserId);
}
