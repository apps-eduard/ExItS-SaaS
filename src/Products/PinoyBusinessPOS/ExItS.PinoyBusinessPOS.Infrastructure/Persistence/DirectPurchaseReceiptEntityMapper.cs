using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class DirectPurchaseReceiptEntityMapper
{
    public static DirectPurchaseReceipt ToDomain(
        DirectPurchaseReceiptRecord record,
        IReadOnlyList<DirectPurchaseReceiptLineRecord> lines) =>
        DirectPurchaseReceipt.Rehydrate(
            DirectPurchaseReceiptId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.ReceiptNumber,
            record.PurchaseDate,
            record.SupplierId is null ? null : SupplierId.From(record.SupplierId.Value),
            record.SourceNameSnapshot,
            record.ReferenceNumber,
            record.Notes,
            record.TotalCost,
            record.CreatedByUserId,
            record.CreatedAtUtc,
            record.IdempotencyKey,
            record.ReceivingBranchId is null ? null : PosBranchId.From(record.ReceivingBranchId.Value),
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList(),
            DirectPurchaseReceiptStatuses.Parse(record.Status),
            record.VoidedAtUtc,
            record.VoidedByUserId,
            record.VoidReason);

    public static DirectPurchaseReceiptLine ToDomain(DirectPurchaseReceiptLineRecord record) =>
        DirectPurchaseReceiptLine.Rehydrate(
            DirectPurchaseReceiptLineId.From(record.Id),
            DirectPurchaseReceiptId.From(record.ReceiptId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.LineNumber,
            record.ProductNameSnapshot,
            record.SkuSnapshot,
            UnitOfMeasures.Parse(record.UnitOfMeasureSnapshot),
            record.Quantity,
            record.UnitCost,
            record.LineTotal,
            record.ExpiryDate,
            record.LotNumber,
            record.InventoryMovementId);

    public static DirectPurchaseReceiptRecord ToRecord(DirectPurchaseReceipt receipt) =>
        new()
        {
            Id = receipt.Id.Value,
            OrganizationId = receipt.OrganizationId.Value,
            ReceiptNumber = receipt.ReceiptNumber,
            PurchaseDate = receipt.PurchaseDate,
            SupplierId = receipt.SupplierId?.Value,
            SourceNameSnapshot = receipt.SourceNameSnapshot,
            ReferenceNumber = receipt.ReferenceNumber,
            Notes = receipt.Notes,
            TotalCost = receipt.TotalCost,
            CreatedByUserId = receipt.CreatedByUserId,
            CreatedAtUtc = receipt.CreatedAtUtc,
            IdempotencyKey = receipt.IdempotencyKey,
            ReceivingBranchId = receipt.ReceivingBranchId?.Value,
            Status = DirectPurchaseReceiptStatuses.ToCode(receipt.Status),
            VoidedAtUtc = receipt.VoidedAtUtc,
            VoidedByUserId = receipt.VoidedByUserId,
            VoidReason = receipt.VoidReason
        };

    public static DirectPurchaseReceiptLineRecord ToRecord(DirectPurchaseReceiptLine line) =>
        new()
        {
            Id = line.Id.Value,
            ReceiptId = line.ReceiptId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            ProductNameSnapshot = line.ProductNameSnapshot,
            SkuSnapshot = line.SkuSnapshot,
            UnitOfMeasureSnapshot = UnitOfMeasures.ToCode(line.UnitOfMeasureSnapshot),
            Quantity = line.Quantity,
            UnitCost = line.UnitCost,
            LineTotal = line.LineTotal,
            ExpiryDate = line.ExpiryDate,
            LotNumber = line.LotNumber,
            InventoryMovementId = line.InventoryMovementId
        };

    public static void Apply(DirectPurchaseReceipt receipt, DirectPurchaseReceiptRecord record)
    {
        record.ReceiptNumber = receipt.ReceiptNumber;
        record.PurchaseDate = receipt.PurchaseDate;
        record.SupplierId = receipt.SupplierId?.Value;
        record.SourceNameSnapshot = receipt.SourceNameSnapshot;
        record.ReferenceNumber = receipt.ReferenceNumber;
        record.Notes = receipt.Notes;
        record.TotalCost = receipt.TotalCost;
        record.CreatedByUserId = receipt.CreatedByUserId;
        record.CreatedAtUtc = receipt.CreatedAtUtc;
        record.IdempotencyKey = receipt.IdempotencyKey;
        record.ReceivingBranchId = receipt.ReceivingBranchId?.Value;
        record.Status = DirectPurchaseReceiptStatuses.ToCode(receipt.Status);
        record.VoidedAtUtc = receipt.VoidedAtUtc;
        record.VoidedByUserId = receipt.VoidedByUserId;
        record.VoidReason = receipt.VoidReason;
    }
}
