using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class InventoryReorderChangeEntityMapper
{
    public static InventoryReorderChange ToDomain(InventoryReorderChangeRecord record) =>
        InventoryReorderChange.Rehydrate(
            InventoryReorderChangeId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            InventoryAccountId.From(record.InventoryAccountId),
            CatalogProductId.From(record.ProductId),
            record.PreviousReorderLevel,
            record.NewReorderLevel,
            record.PreviousReorderQuantity,
            record.NewReorderQuantity,
            record.Reason,
            record.ChangedBy,
            record.ChangedAtUtc);

    public static InventoryReorderChangeRecord ToRecord(InventoryReorderChange change) =>
        new()
        {
            Id = change.Id.Value,
            OrganizationId = change.OrganizationId.Value,
            InventoryAccountId = change.InventoryAccountId.Value,
            ProductId = change.ProductId.Value,
            PreviousReorderLevel = change.PreviousReorderLevel,
            NewReorderLevel = change.NewReorderLevel,
            PreviousReorderQuantity = change.PreviousReorderQuantity,
            NewReorderQuantity = change.NewReorderQuantity,
            Reason = change.Reason,
            ChangedBy = change.ChangedBy,
            ChangedAtUtc = change.ChangedAtUtc
        };
}
