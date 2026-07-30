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
            OnHandQuantity = account.OnHandQuantity,
            CreatedAtUtc = account.CreatedAtUtc,
            UpdatedAtUtc = account.UpdatedAtUtc
        };

    public static void ApplyToRecord(InventoryAccount account, InventoryAccountRecord record)
    {
        record.IsTracked = account.IsTracked;
        record.ReorderLevel = account.ReorderLevel;
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
            record.RecordedBy);

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
            RecordedBy = movement.RecordedBy
        };
}
