using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class InventoryEntityMapper
{
    public static InventoryAccount ToDomain(InventoryAccountRecord record) =>
        InventoryAccount.Rehydrate(
            InventoryAccountId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.IsTracked,
            record.ReorderLevel,
            record.ReorderQuantity,
            record.OnHandQuantity,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static InventoryAccountRecord ToRecord(InventoryAccount account) =>
        new()
        {
            Id = account.Id.Value,
            OrganizationId = account.OrganizationId.Value,
            ProductId = account.ProductId.Value,
            IsTracked = account.IsTracked,
            ReorderLevel = account.ReorderLevel,
            ReorderQuantity = account.ReorderQuantity,
            OnHandQuantity = account.OnHandQuantity,
            CreatedAtUtc = account.CreatedAtUtc,
            UpdatedAtUtc = account.UpdatedAtUtc
        };

    public static void ApplyToRecord(InventoryAccount account, InventoryAccountRecord record)
    {
        record.IsTracked = account.IsTracked;
        record.ReorderLevel = account.ReorderLevel;
        record.ReorderQuantity = account.ReorderQuantity;
        record.OnHandQuantity = account.OnHandQuantity;
        record.UpdatedAtUtc = account.UpdatedAtUtc;
    }

    public static StockMovement ToDomain(StockMovementRecord record) =>
        StockMovement.Rehydrate(
            StockMovementId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            InventoryAccountId.From(record.InventoryAccountId),
            StockMovementTypes.Parse(record.MovementType),
            record.QuantityEffect,
            record.Reason,
            StockMovementSourceTypes.Parse(record.SourceType),
            record.SourceId,
            record.RecordedAtUtc,
            record.RecordedBy,
            record.BranchId,
            record.InventoryLotId is null ? null : InventoryLotId.From(record.InventoryLotId.Value));

    public static StockMovementRecord ToRecord(StockMovement movement) =>
        new()
        {
            Id = movement.Id.Value,
            OrganizationId = movement.OrganizationId.Value,
            ProductId = movement.ProductId.Value,
            InventoryAccountId = movement.InventoryAccountId.Value,
            MovementType = StockMovementTypes.ToCode(movement.MovementType),
            QuantityEffect = movement.QuantityEffect,
            Reason = movement.Reason,
            SourceType = StockMovementSourceTypes.ToCode(movement.SourceType),
            SourceId = movement.SourceId,
            RecordedAtUtc = movement.RecordedAtUtc,
            RecordedBy = movement.RecordedBy,
            BranchId = movement.BranchId,
            InventoryLotId = movement.InventoryLotId?.Value
        };

    public static InventoryLot ToDomain(InventoryLotRecord record) =>
        InventoryLot.Rehydrate(
            InventoryLotId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value),
            record.LotNumber,
            record.NormalizedLotNumber,
            record.ExpirationDate,
            record.QuantityOnHand,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static InventoryLotRecord ToRecord(InventoryLot lot) =>
        new()
        {
            Id = lot.Id.Value,
            OrganizationId = lot.OrganizationId.Value,
            ProductId = lot.ProductId.Value,
            BranchId = lot.BranchId?.Value,
            LotNumber = lot.LotNumber,
            NormalizedLotNumber = lot.NormalizedLotNumber,
            ExpirationDate = lot.ExpirationDate,
            QuantityOnHand = lot.QuantityOnHand,
            CreatedAtUtc = lot.CreatedAtUtc,
            UpdatedAtUtc = lot.UpdatedAtUtc
        };

    public static void ApplyToRecord(InventoryLot lot, InventoryLotRecord record)
    {
        record.QuantityOnHand = lot.QuantityOnHand;
        record.UpdatedAtUtc = lot.UpdatedAtUtc;
    }

    public static InventoryLotMovement ToDomain(InventoryLotMovementRecord record) =>
        InventoryLotMovement.Rehydrate(
            record.Id,
            PosOrganizationId.From(record.OrganizationId),
            InventoryLotId.From(record.LotId),
            CatalogProductId.From(record.ProductId),
            StockMovementTypes.Parse(record.MovementType),
            record.QuantityEffect,
            StockMovementSourceTypes.Parse(record.SourceType),
            record.SourceId,
            record.StockMovementId,
            record.RecordedAtUtc,
            record.RecordedBy);

    public static InventoryLotMovementRecord ToRecord(InventoryLotMovement movement) =>
        new()
        {
            Id = movement.Id,
            OrganizationId = movement.OrganizationId.Value,
            LotId = movement.LotId.Value,
            ProductId = movement.ProductId.Value,
            MovementType = StockMovementTypes.ToCode(movement.MovementType),
            QuantityEffect = movement.QuantityEffect,
            SourceType = StockMovementSourceTypes.ToCode(movement.SourceType),
            SourceId = movement.SourceId,
            StockMovementId = movement.StockMovementId,
            RecordedAtUtc = movement.RecordedAtUtc,
            RecordedBy = movement.RecordedBy
        };
}
