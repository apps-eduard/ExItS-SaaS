using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Seller-review case for buyer-reported damaged / missing quantities on a connected PO goods receipt.
/// Buyer GRN remains immutable evidence; this aggregate records seller decisions and links inventory reconciliation.
/// </summary>
public sealed class ConnectedPoReceivingIssue
{
    public const int SellerNotesMaxLength = 500;

    private readonly List<ConnectedPoReceivingIssueLine> _lines;

    public ConnectedPoReceivingIssueId Id { get; }
    public ConnectedPurchaseOrderId ConnectedPurchaseOrderId { get; }
    public PurchaseOrderId PurchaseOrderId { get; }
    public GoodsReceiptId GoodsReceiptId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public Guid FulfillmentSourceId { get; }
    public ConnectedPoReceivingIssueStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? SellerNotes { get; private set; }
    public byte[] RowVersion { get; private set; }

    public IReadOnlyList<ConnectedPoReceivingIssueLine> Lines => _lines;

    public int UnresolvedLineCount => _lines.Count(l => !l.IsResolved);

    private ConnectedPoReceivingIssue(
        ConnectedPoReceivingIssueId id,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        PurchaseOrderId purchaseOrderId,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        Guid fulfillmentSourceId,
        ConnectedPoReceivingIssueStatus status,
        DateTimeOffset createdAtUtc,
        Guid createdByUserId,
        DateTimeOffset? resolvedAtUtc,
        Guid? resolvedByUserId,
        string? sellerNotes,
        List<ConnectedPoReceivingIssueLine> lines,
        byte[]? rowVersion = null)
    {
        Id = id;
        ConnectedPurchaseOrderId = connectedPurchaseOrderId;
        PurchaseOrderId = purchaseOrderId;
        GoodsReceiptId = goodsReceiptId;
        BuyerOrganizationId = buyerOrganizationId;
        SellerOrganizationId = sellerOrganizationId;
        FulfillmentSourceId = fulfillmentSourceId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
        ResolvedAtUtc = resolvedAtUtc;
        ResolvedByUserId = resolvedByUserId;
        SellerNotes = sellerNotes;
        _lines = lines;
        RowVersion = rowVersion ?? Array.Empty<byte>();
    }

    public static ConnectedPoReceivingIssue CreateFromReceipt(
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        PurchaseOrderId purchaseOrderId,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        Guid fulfillmentSourceId,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        IReadOnlyList<ConnectedPoReceivingIssueLineDraft> lineDrafts)
    {
        if (fulfillmentSourceId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueFulfillmentSource,
                "Fulfillment source id is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidActorId,
                "Actor id is required.");
        }

        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "CreatedAtUtc must be UTC.");
        }

        if (lineDrafts is null || lineDrafts.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLine,
                "At least one damaged or missing line is required.");
        }

        var id = ConnectedPoReceivingIssueId.New();
        var lines = new List<ConnectedPoReceivingIssueLine>();
        foreach (var draft in lineDrafts)
        {
            lines.Add(ConnectedPoReceivingIssueLine.Create(
                id,
                sellerOrganizationId,
                draft.GoodsReceiptLineId,
                draft.PurchaseOrderLineId,
                draft.SupplierProductId,
                draft.BuyerProductId,
                draft.NameSnapshot,
                draft.UomSnapshot,
                draft.FulfillmentSourceId == Guid.Empty ? fulfillmentSourceId : draft.FulfillmentSourceId,
                draft.ShippedQty,
                draft.GoodQty,
                draft.DamagedQty,
                draft.MissingQty,
                draft.LineKind,
                draft.BuyerDiscrepancyKind,
                draft.BuyerDiscrepancyNote));
        }

        return new ConnectedPoReceivingIssue(
            id,
            connectedPurchaseOrderId,
            purchaseOrderId,
            goodsReceiptId,
            buyerOrganizationId,
            sellerOrganizationId,
            fulfillmentSourceId,
            ConnectedPoReceivingIssueStatus.PendingSellerReview,
            utcNow,
            createdByUserId,
            resolvedAtUtc: null,
            resolvedByUserId: null,
            sellerNotes: null,
            lines);
    }

    public ConnectedPoReceivingIssueLine GetLine(ConnectedPoReceivingIssueLineId lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueLineNotFound,
                "Receiving issue line was not found.");
        }

        return line;
    }

    public void EnsureSellerOrganization(PosOrganizationId sellerOrganizationId)
    {
        if (SellerOrganizationId != sellerOrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReceivingIssueForbidden,
                "Only the seller organization may resolve this receiving issue.");
        }
    }

    public void RefreshStatus(Guid actorId, DateTimeOffset utcNow, string? sellerNotes = null)
    {
        if (_lines.All(l => l.IsResolved))
        {
            Status = ConnectedPoReceivingIssueStatus.Resolved;
            ResolvedAtUtc ??= utcNow;
            ResolvedByUserId ??= actorId;
            if (!string.IsNullOrWhiteSpace(sellerNotes))
            {
                var trimmed = sellerNotes.Trim();
                if (trimmed.Length > SellerNotesMaxLength)
                {
                    throw new DomainException(
                        DomainErrorCodes.InvalidConnectedPoReceivingIssueSellerNote,
                        $"Seller notes must be at most {SellerNotesMaxLength} characters.");
                }

                SellerNotes = trimmed;
            }
        }
        else
        {
            Status = ConnectedPoReceivingIssueStatus.PendingSellerReview;
            ResolvedAtUtc = null;
            ResolvedByUserId = null;
        }
    }

    public void SetRowVersion(byte[] rowVersion) =>
        RowVersion = rowVersion ?? Array.Empty<byte>();

    public static ConnectedPoReceivingIssue Rehydrate(
        ConnectedPoReceivingIssueId id,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        PurchaseOrderId purchaseOrderId,
        GoodsReceiptId goodsReceiptId,
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        Guid fulfillmentSourceId,
        ConnectedPoReceivingIssueStatus status,
        DateTimeOffset createdAtUtc,
        Guid createdByUserId,
        DateTimeOffset? resolvedAtUtc,
        Guid? resolvedByUserId,
        string? sellerNotes,
        IReadOnlyList<ConnectedPoReceivingIssueLine> lines,
        byte[]? rowVersion = null) =>
        new(
            id,
            connectedPurchaseOrderId,
            purchaseOrderId,
            goodsReceiptId,
            buyerOrganizationId,
            sellerOrganizationId,
            fulfillmentSourceId,
            status,
            createdAtUtc,
            createdByUserId,
            resolvedAtUtc,
            resolvedByUserId,
            sellerNotes,
            lines.ToList(),
            rowVersion);
}

/// <summary>Draft used when creating issue lines from a posted goods receipt.</summary>
public sealed record ConnectedPoReceivingIssueLineDraft(
    GoodsReceiptLineId GoodsReceiptLineId,
    PurchaseOrderLineId PurchaseOrderLineId,
    CatalogProductId SupplierProductId,
    CatalogProductId? BuyerProductId,
    string NameSnapshot,
    string UomSnapshot,
    decimal ShippedQty,
    decimal GoodQty,
    decimal DamagedQty,
    decimal MissingQty,
    ConnectedPoReceivingIssueLineKind LineKind,
    ConnectedPoReceivingDiscrepancyKind BuyerDiscrepancyKind,
    string? BuyerDiscrepancyNote,
    Guid FulfillmentSourceId = default);
