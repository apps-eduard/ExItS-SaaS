using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Purchasing;

internal static class PurchaseEntityMapper
{
    public static PurchaseOrder ToDomain(PurchaseOrderRecord record, IEnumerable<PurchaseOrderLineRecord> lineRecords)
    {
        var poId = PurchaseOrderId.From(record.Id);
        var orgId = PosOrganizationId.From(record.OrganizationId);
        var lines = lineRecords
            .OrderBy(l => l.LineNumber)
            .Select(l => PurchaseOrderLine.Rehydrate(
                PurchaseOrderLineId.From(l.Id),
                poId,
                orgId,
                CatalogProductId.From(l.ProductId),
                l.LineNumber,
                l.NameSnapshot,
                l.UomSnapshot is null ? null : UnitOfMeasures.Parse(l.UomSnapshot),
                l.OrderedQty,
                l.UnitPurchaseCost,
                l.LineTotal,
                l.ReceivedQty,
                l.LineNotes,
                l.PurchaseUnitId is null ? null : ProductUnitId.From(l.PurchaseUnitId.Value),
                l.PurchaseUnitNameSnapshot,
                l.MultiplierToBaseSnapshot))
            .ToList();

        return PurchaseOrder.Rehydrate(
            poId,
            orgId,
            record.PoNumber,
            SupplierId.From(record.SupplierId),
            Enum.Parse<PurchaseOrderStatus>(record.Status, ignoreCase: true),
            record.OrderDate,
            record.ExpectedDeliveryDate,
            record.SupplierReference,
            record.Notes,
            record.OrderedAtUtc,
            record.OrderedBy,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            lines);
    }

    public static PurchaseOrderRecord ToRecord(PurchaseOrder po) =>
        new()
        {
            Id = po.Id.Value,
            OrganizationId = po.OrganizationId.Value,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId.Value,
            Status = po.Status.ToString(),
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            SupplierReference = po.SupplierReference,
            Notes = po.Notes,
            OrderedAtUtc = po.OrderedAtUtc,
            OrderedBy = po.OrderedBy,
            CreatedAtUtc = po.CreatedAtUtc,
            UpdatedAtUtc = po.UpdatedAtUtc
        };

    public static void ApplyToRecord(PurchaseOrder po, PurchaseOrderRecord record)
    {
        record.PoNumber = po.PoNumber;
        record.SupplierId = po.SupplierId.Value;
        record.Status = po.Status.ToString();
        record.OrderDate = po.OrderDate;
        record.ExpectedDeliveryDate = po.ExpectedDeliveryDate;
        record.SupplierReference = po.SupplierReference;
        record.Notes = po.Notes;
        record.OrderedAtUtc = po.OrderedAtUtc;
        record.OrderedBy = po.OrderedBy;
        record.UpdatedAtUtc = po.UpdatedAtUtc;
    }

    public static PurchaseOrderLineRecord ToRecord(PurchaseOrderLine line) =>
        new()
        {
            Id = line.Id.Value,
            PurchaseOrderId = line.PurchaseOrderId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            UomSnapshot = line.UomSnapshot is null ? null : UnitOfMeasures.ToCode(line.UomSnapshot.Value),
            OrderedQty = line.OrderedQty,
            UnitPurchaseCost = line.UnitPurchaseCost,
            LineTotal = line.LineTotal,
            ReceivedQty = line.ReceivedQty,
            LineNotes = line.LineNotes,
            PurchaseUnitId = line.PurchaseUnitId?.Value,
            PurchaseUnitNameSnapshot = line.PurchaseUnitNameSnapshot,
            MultiplierToBaseSnapshot = line.MultiplierToBaseSnapshot
        };

    public static GoodsReceipt ToDomain(GoodsReceiptRecord record, IEnumerable<GoodsReceiptLineRecord> lineRecords)
    {
        var grnId = GoodsReceiptId.From(record.Id);
        var orgId = PosOrganizationId.From(record.OrganizationId);
        var lines = lineRecords
            .OrderBy(l => l.LineNumber)
            .Select(l => GoodsReceiptLine.Rehydrate(
                GoodsReceiptLineId.From(l.Id),
                grnId,
                orgId,
                PurchaseOrderLineId.From(l.PurchaseOrderLineId),
                CatalogProductId.From(l.ProductId),
                l.LineNumber,
                l.NameSnapshot,
                UnitOfMeasures.Parse(l.UomSnapshot),
                l.ReceivedQty,
                l.UnitPurchaseCostSnapshot,
                l.LineTotalSnapshot,
                l.InventoryMovementId,
                l.MultiplierToBaseSnapshot))
            .ToList();

        return GoodsReceipt.Rehydrate(
            grnId,
            orgId,
            PurchaseOrderId.From(record.PurchaseOrderId),
            SupplierId.From(record.SupplierId),
            record.GrnNumber,
            record.ReceivedDate,
            record.DeliveryReference,
            record.Notes,
            record.ReceivedAtUtc,
            record.ReceivedBy,
            lines);
    }

    public static GoodsReceiptRecord ToRecord(GoodsReceipt receipt) =>
        new()
        {
            Id = receipt.Id.Value,
            OrganizationId = receipt.OrganizationId.Value,
            PurchaseOrderId = receipt.PurchaseOrderId.Value,
            SupplierId = receipt.SupplierId.Value,
            GrnNumber = receipt.GrnNumber,
            ReceivedDate = receipt.ReceivedDate,
            DeliveryReference = receipt.DeliveryReference,
            Notes = receipt.Notes,
            ReceivedAtUtc = receipt.ReceivedAtUtc,
            ReceivedBy = receipt.ReceivedBy
        };

    public static GoodsReceiptLineRecord ToRecord(GoodsReceiptLine line) =>
        new()
        {
            Id = line.Id.Value,
            GoodsReceiptId = line.GoodsReceiptId.Value,
            OrganizationId = line.OrganizationId.Value,
            PurchaseOrderLineId = line.PurchaseOrderLineId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            UomSnapshot = UnitOfMeasures.ToCode(line.UomSnapshot),
            ReceivedQty = line.QuantityReceived,
            UnitPurchaseCostSnapshot = line.UnitPurchaseCostSnapshot,
            LineTotalSnapshot = line.LineTotalSnapshot,
            InventoryMovementId = line.InventoryMovementId,
            MultiplierToBaseSnapshot = line.MultiplierToBaseSnapshot
        };

    public static void ApplyMovementId(GoodsReceiptLine line, GoodsReceiptLineRecord record)
    {
        record.InventoryMovementId = line.InventoryMovementId;
    }
}
