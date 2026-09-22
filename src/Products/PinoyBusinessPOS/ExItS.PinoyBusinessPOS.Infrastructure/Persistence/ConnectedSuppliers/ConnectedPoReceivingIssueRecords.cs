using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedPoReceivingIssueRecord
{
    public Guid Id { get; set; }
    public Guid ConnectedPurchaseOrderId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid FulfillmentSourceId { get; set; }
    public string Status { get; set; } = nameof(ConnectedPoReceivingIssueStatus.PendingSellerReview);
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? SellerNotes { get; set; }
    public List<ConnectedPoReceivingIssueLineRecord> Lines { get; set; } = [];
}

internal sealed class ConnectedPoReceivingIssueLineRecord
{
    public Guid Id { get; set; }
    public Guid ReceivingIssueId { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid SupplierProductId { get; set; }
    public Guid? BuyerProductId { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UomSnapshot { get; set; } = string.Empty;
    public Guid FulfillmentSourceId { get; set; }
    public decimal ShippedQty { get; set; }
    public decimal GoodQty { get; set; }
    public decimal DamagedQty { get; set; }
    public decimal MissingQty { get; set; }
    public string LineKind { get; set; } = nameof(ConnectedPoReceivingIssueLineKind.Missing);
    public string BuyerDiscrepancyKind { get; set; } = nameof(ConnectedPoReceivingDiscrepancyKind.None);
    public string? BuyerDiscrepancyNote { get; set; }
    public string? MissingResolution { get; set; }
    public string? DamagedResolution { get; set; }
    public decimal ResolutionQty { get; set; }
    public string? SellerNote { get; set; }
    public Guid? InventoryMovementId { get; set; }
    public Guid? ReturnBatchId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public Guid? ResolvedByUserId { get; set; }
}

internal static class ConnectedPoReceivingIssueEntityMapper
{
    public static ConnectedPoReceivingIssue ToDomain(ConnectedPoReceivingIssueRecord r) =>
        ConnectedPoReceivingIssue.Rehydrate(
            ConnectedPoReceivingIssueId.From(r.Id),
            ConnectedPurchaseOrderId.From(r.ConnectedPurchaseOrderId),
            PurchaseOrderId.From(r.PurchaseOrderId),
            GoodsReceiptId.From(r.GoodsReceiptId),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SellerOrganizationId),
            r.FulfillmentSourceId,
            ConnectedPoReceivingIssueStatuses.Parse(r.Status),
            r.CreatedAtUtc,
            r.CreatedByUserId,
            r.ResolvedAtUtc,
            r.ResolvedByUserId,
            r.SellerNotes,
            r.Lines.OrderBy(l => l.Id).Select(ToDomain).ToList());

    public static ConnectedPoReceivingIssueLine ToDomain(ConnectedPoReceivingIssueLineRecord r) =>
        ConnectedPoReceivingIssueLine.Rehydrate(
            ConnectedPoReceivingIssueLineId.From(r.Id),
            ConnectedPoReceivingIssueId.From(r.ReceivingIssueId),
            PosOrganizationId.From(r.SellerOrganizationId),
            GoodsReceiptLineId.From(r.GoodsReceiptLineId),
            PurchaseOrderLineId.From(r.PurchaseOrderLineId),
            CatalogProductId.From(r.SupplierProductId),
            r.BuyerProductId is Guid buyer ? CatalogProductId.From(buyer) : null,
            r.NameSnapshot,
            r.UomSnapshot,
            r.FulfillmentSourceId,
            r.ShippedQty,
            r.GoodQty,
            r.DamagedQty,
            r.MissingQty,
            ConnectedPoReceivingIssueLineKinds.Parse(r.LineKind),
            Enum.TryParse<ConnectedPoReceivingDiscrepancyKind>(r.BuyerDiscrepancyKind, ignoreCase: false, out var kind)
                ? kind
                : ConnectedPoReceivingDiscrepancyKind.None,
            r.BuyerDiscrepancyNote,
            string.IsNullOrWhiteSpace(r.MissingResolution)
                ? null
                : ConnectedPoMissingResolutions.Parse(r.MissingResolution),
            string.IsNullOrWhiteSpace(r.DamagedResolution)
                ? null
                : ConnectedPoDamagedResolutions.Parse(r.DamagedResolution),
            r.ResolutionQty,
            r.SellerNote,
            r.InventoryMovementId,
            r.ReturnBatchId,
            r.ResolvedAtUtc,
            r.ResolvedByUserId);

    public static ConnectedPoReceivingIssueRecord ToRecord(ConnectedPoReceivingIssue x) => new()
    {
        Id = x.Id.Value,
        ConnectedPurchaseOrderId = x.ConnectedPurchaseOrderId.Value,
        PurchaseOrderId = x.PurchaseOrderId.Value,
        GoodsReceiptId = x.GoodsReceiptId.Value,
        BuyerOrganizationId = x.BuyerOrganizationId.Value,
        SellerOrganizationId = x.SellerOrganizationId.Value,
        FulfillmentSourceId = x.FulfillmentSourceId,
        Status = ConnectedPoReceivingIssueStatuses.ToCode(x.Status),
        CreatedAtUtc = x.CreatedAtUtc,
        CreatedByUserId = x.CreatedByUserId,
        ResolvedAtUtc = x.ResolvedAtUtc,
        ResolvedByUserId = x.ResolvedByUserId,
        SellerNotes = x.SellerNotes,
        Lines = x.Lines.Select(ToRecord).ToList()
    };

    public static ConnectedPoReceivingIssueLineRecord ToRecord(ConnectedPoReceivingIssueLine x) => new()
    {
        Id = x.Id.Value,
        ReceivingIssueId = x.ReceivingIssueId.Value,
        SellerOrganizationId = x.SellerOrganizationId.Value,
        GoodsReceiptLineId = x.GoodsReceiptLineId.Value,
        PurchaseOrderLineId = x.PurchaseOrderLineId.Value,
        SupplierProductId = x.SupplierProductId.Value,
        BuyerProductId = x.BuyerProductId?.Value,
        NameSnapshot = x.NameSnapshot,
        UomSnapshot = x.UomSnapshot,
        FulfillmentSourceId = x.FulfillmentSourceId,
        ShippedQty = x.ShippedQty,
        GoodQty = x.GoodQty,
        DamagedQty = x.DamagedQty,
        MissingQty = x.MissingQty,
        LineKind = ConnectedPoReceivingIssueLineKinds.ToCode(x.LineKind),
        BuyerDiscrepancyKind = x.BuyerDiscrepancyKind.ToString(),
        BuyerDiscrepancyNote = x.BuyerDiscrepancyNote,
        MissingResolution = x.MissingResolution is { } mr
            ? ConnectedPoMissingResolutions.ToCode(mr)
            : null,
        DamagedResolution = x.DamagedResolution is { } dr
            ? ConnectedPoDamagedResolutions.ToCode(dr)
            : null,
        ResolutionQty = x.ResolutionQty,
        SellerNote = x.SellerNote,
        InventoryMovementId = x.InventoryMovementId,
        ReturnBatchId = x.ReturnBatchId,
        ResolvedAtUtc = x.ResolvedAtUtc,
        ResolvedByUserId = x.ResolvedByUserId
    };

    public static void Apply(ConnectedPoReceivingIssue x, ConnectedPoReceivingIssueRecord r)
    {
        r.Status = ConnectedPoReceivingIssueStatuses.ToCode(x.Status);
        r.ResolvedAtUtc = x.ResolvedAtUtc;
        r.ResolvedByUserId = x.ResolvedByUserId;
        r.SellerNotes = x.SellerNotes;

        var existing = r.Lines.ToDictionary(l => l.Id);
        foreach (var line in x.Lines)
        {
            if (existing.TryGetValue(line.Id.Value, out var row))
            {
                Apply(line, row);
            }
            else
            {
                r.Lines.Add(ToRecord(line));
            }
        }
    }

    public static void Apply(ConnectedPoReceivingIssueLine x, ConnectedPoReceivingIssueLineRecord r)
    {
        r.MissingResolution = x.MissingResolution is { } mr
            ? ConnectedPoMissingResolutions.ToCode(mr)
            : null;
        r.DamagedResolution = x.DamagedResolution is { } dr
            ? ConnectedPoDamagedResolutions.ToCode(dr)
            : null;
        r.ResolutionQty = x.ResolutionQty;
        r.SellerNote = x.SellerNote;
        r.InventoryMovementId = x.InventoryMovementId;
        r.ReturnBatchId = x.ReturnBatchId;
        r.ResolvedAtUtc = x.ResolvedAtUtc;
        r.ResolvedByUserId = x.ResolvedByUserId;
    }
}
