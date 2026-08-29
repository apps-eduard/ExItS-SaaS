using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class WasteLossEntityMapper
{
    public static WasteLoss ToDomain(WasteLossRecord record, IReadOnlyList<WasteLossLineRecord> lines) =>
        WasteLoss.Rehydrate(
            WasteLossId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.WasteLossNumber,
            record.ReferenceNumber,
            record.OccurredAtUtc,
            WasteLossReasons.Parse(record.Reason),
            record.Notes,
            WasteLossStatuses.Parse(record.Status),
            ProductionCostStatuses.Parse(record.CostStatus),
            record.TotalCostSnapshot,
            record.CreatedByUserId,
            record.CreatedAtUtc,
            record.VoidedByUserId,
            record.VoidedAtUtc,
            record.IdempotencyKey,
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList());

    public static WasteLossLine ToDomain(WasteLossLineRecord record) =>
        WasteLossLine.Rehydrate(
            WasteLossLineId.From(record.Id),
            WasteLossId.From(record.WasteLossId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.ProductUnitId is null ? null : ProductUnitId.From(record.ProductUnitId.Value),
            record.InventoryLotId is null ? null : InventoryLotId.From(record.InventoryLotId.Value),
            record.LineNumber,
            record.QuantityEntered,
            record.MultiplierToBase,
            record.BaseQuantity,
            record.NameSnapshot,
            record.UnitLabelSnapshot,
            record.UnitCostSnapshot,
            record.LineCostSnapshot,
            record.InventoryMovementId);

    public static WasteLossRecord ToRecord(WasteLoss wasteLoss) =>
        new()
        {
            Id = wasteLoss.Id.Value,
            OrganizationId = wasteLoss.OrganizationId.Value,
            BranchId = wasteLoss.BranchId?.Value,
            WasteLossNumber = wasteLoss.WasteLossNumber,
            ReferenceNumber = wasteLoss.ReferenceNumber,
            OccurredAtUtc = wasteLoss.OccurredAtUtc,
            Reason = WasteLossReasons.ToCode(wasteLoss.Reason),
            Notes = wasteLoss.Notes,
            Status = WasteLossStatuses.ToCode(wasteLoss.Status),
            CostStatus = ProductionCostStatuses.ToCode(wasteLoss.CostStatus),
            TotalCostSnapshot = wasteLoss.TotalCostSnapshot,
            CreatedByUserId = wasteLoss.CreatedByUserId,
            CreatedAtUtc = wasteLoss.CreatedAtUtc,
            VoidedByUserId = wasteLoss.VoidedByUserId,
            VoidedAtUtc = wasteLoss.VoidedAtUtc,
            IdempotencyKey = wasteLoss.IdempotencyKey
        };

    public static WasteLossLineRecord ToRecord(WasteLossLine line) =>
        new()
        {
            Id = line.Id.Value,
            WasteLossId = line.WasteLossId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            ProductUnitId = line.ProductUnitId?.Value,
            InventoryLotId = line.InventoryLotId?.Value,
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

    public static void Apply(WasteLoss wasteLoss, WasteLossRecord record)
    {
        record.BranchId = wasteLoss.BranchId?.Value;
        record.WasteLossNumber = wasteLoss.WasteLossNumber;
        record.ReferenceNumber = wasteLoss.ReferenceNumber;
        record.OccurredAtUtc = wasteLoss.OccurredAtUtc;
        record.Reason = WasteLossReasons.ToCode(wasteLoss.Reason);
        record.Notes = wasteLoss.Notes;
        record.Status = WasteLossStatuses.ToCode(wasteLoss.Status);
        record.CostStatus = ProductionCostStatuses.ToCode(wasteLoss.CostStatus);
        record.TotalCostSnapshot = wasteLoss.TotalCostSnapshot;
        record.CreatedByUserId = wasteLoss.CreatedByUserId;
        record.CreatedAtUtc = wasteLoss.CreatedAtUtc;
        record.VoidedByUserId = wasteLoss.VoidedByUserId;
        record.VoidedAtUtc = wasteLoss.VoidedAtUtc;
        record.IdempotencyKey = wasteLoss.IdempotencyKey;
    }
}
