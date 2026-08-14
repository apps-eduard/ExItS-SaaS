using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CatalogProductUnitEntityMapper
{
    public static CatalogProductUnit ToDomain(CatalogProductUnitRecord record) =>
        CatalogProductUnit.Rehydrate(
            ProductUnitId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            (ProductUnitKind)record.Kind,
            record.DisplayName,
            record.ShortLabel,
            record.MultiplierToBase,
            record.SellingPrice,
            record.AllowsCustomQuantity,
            record.IsActive,
            record.SortOrder,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static CatalogProductUnitRecord ToRecord(CatalogProductUnit unit) =>
        new()
        {
            Id = unit.Id.Value,
            OrganizationId = unit.OrganizationId.Value,
            ProductId = unit.ProductId.Value,
            Kind = (int)unit.Kind,
            DisplayName = unit.DisplayName,
            ShortLabel = unit.ShortLabel,
            MultiplierToBase = unit.MultiplierToBase,
            SellingPrice = unit.SellingPrice,
            AllowsCustomQuantity = unit.AllowsCustomQuantity,
            IsActive = unit.IsActive,
            SortOrder = unit.SortOrder,
            CreatedAtUtc = unit.CreatedAtUtc,
            UpdatedAtUtc = unit.UpdatedAtUtc
        };

    public static void ApplyToRecord(CatalogProductUnit unit, CatalogProductUnitRecord record)
    {
        record.Kind = (int)unit.Kind;
        record.DisplayName = unit.DisplayName;
        record.ShortLabel = unit.ShortLabel;
        record.MultiplierToBase = unit.MultiplierToBase;
        record.SellingPrice = unit.SellingPrice;
        record.AllowsCustomQuantity = unit.AllowsCustomQuantity;
        record.IsActive = unit.IsActive;
        record.SortOrder = unit.SortOrder;
        record.UpdatedAtUtc = unit.UpdatedAtUtc;
    }
}
