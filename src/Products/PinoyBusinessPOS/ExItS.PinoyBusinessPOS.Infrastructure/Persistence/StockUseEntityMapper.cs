using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class StockUseEntityMapper
{
    public static StockUse ToDomain(StockUseRecord record, IReadOnlyList<StockUseLineRecord> lines) =>
        StockUse.Rehydrate(
            StockUseId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.StockUseNumber,
            record.ReferenceNumber,
            record.OccurredAtUtc,
            StockUseReasons.Parse(record.Reason),
            record.Notes,
            StockUseStatuses.Parse(record.Status),
            record.CreatedByUserId,
            record.CreatedAtUtc,
            record.VoidedByUserId,
            record.VoidedAtUtc,
            record.IdempotencyKey,
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList());

    public static StockUseLine ToDomain(StockUseLineRecord record) =>
        StockUseLine.Rehydrate(
            StockUseLineId.From(record.Id),
            StockUseId.From(record.StockUseId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.ProductUnitId is null ? null : ProductUnitId.From(record.ProductUnitId.Value),
            record.LineNumber,
            record.QuantityEntered,
            record.MultiplierToBase,
            record.BaseQuantity,
            record.NameSnapshot,
            record.UnitLabelSnapshot,
            record.UnitCostSnapshot,
            record.LineCostSnapshot,
            record.InventoryMovementId);

    public static StockUseRecord ToRecord(StockUse stockUse) =>
        new()
        {
            Id = stockUse.Id.Value,
            OrganizationId = stockUse.OrganizationId.Value,
            BranchId = stockUse.BranchId?.Value,
            StockUseNumber = stockUse.StockUseNumber,
            ReferenceNumber = stockUse.ReferenceNumber,
            OccurredAtUtc = stockUse.OccurredAtUtc,
            Reason = StockUseReasons.ToCode(stockUse.Reason),
            Notes = stockUse.Notes,
            Status = StockUseStatuses.ToCode(stockUse.Status),
            CreatedByUserId = stockUse.CreatedByUserId,
            CreatedAtUtc = stockUse.CreatedAtUtc,
            VoidedByUserId = stockUse.VoidedByUserId,
            VoidedAtUtc = stockUse.VoidedAtUtc,
            IdempotencyKey = stockUse.IdempotencyKey
        };

    public static StockUseLineRecord ToRecord(StockUseLine line) =>
        new()
        {
            Id = line.Id.Value,
            StockUseId = line.StockUseId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            ProductUnitId = line.ProductUnitId?.Value,
            LineNumber = line.LineNumber,
            QuantityEntered = line.QuantityEntered,
            MultiplierToBase = line.MultiplierToBase,
            BaseQuantity = line.BaseQuantity,
            NameSnapshot = line.NameSnapshot,
            UnitLabelSnapshot = line.UnitLabelSnapshot,
            UnitCostSnapshot = line.UnitCostSnapshot,
            LineCostSnapshot = line.LineCostSnapshot,
            InventoryMovementId = line.InventoryMovementId
        };

    public static void Apply(StockUse stockUse, StockUseRecord record)
    {
        record.BranchId = stockUse.BranchId?.Value;
        record.StockUseNumber = stockUse.StockUseNumber;
        record.ReferenceNumber = stockUse.ReferenceNumber;
        record.OccurredAtUtc = stockUse.OccurredAtUtc;
        record.Reason = StockUseReasons.ToCode(stockUse.Reason);
        record.Notes = stockUse.Notes;
        record.Status = StockUseStatuses.ToCode(stockUse.Status);
        record.CreatedByUserId = stockUse.CreatedByUserId;
        record.CreatedAtUtc = stockUse.CreatedAtUtc;
        record.VoidedByUserId = stockUse.VoidedByUserId;
        record.VoidedAtUtc = stockUse.VoidedAtUtc;
        record.IdempotencyKey = stockUse.IdempotencyKey;
    }
}
