using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CatalogEntityMapper
{
    public static ProductCategory ToDomain(ProductCategoryRecord record) =>
        ProductCategory.Rehydrate(
            ProductCategoryId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            record.NormalizedName,
            Enum.Parse<ProductCategoryStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ProductCategoryRecord ToRecord(ProductCategory category) =>
        new()
        {
            Id = category.Id.Value,
            OrganizationId = category.OrganizationId.Value,
            Name = category.Name,
            NormalizedName = category.NormalizedName,
            Status = category.Status.ToString(),
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };

    public static void ApplyToRecord(ProductCategory category, ProductCategoryRecord record)
    {
        record.Name = category.Name;
        record.NormalizedName = category.NormalizedName;
        record.Status = category.Status.ToString();
        record.UpdatedAtUtc = category.UpdatedAtUtc;
        // OrganizationId is immutable — never rewritten from the aggregate.
    }

    public static CatalogProduct ToDomain(CatalogProductRecord record) =>
        CatalogProduct.Rehydrate(
            CatalogProductId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            record.Description,
            record.Sku,
            record.NormalizedSku,
            record.Barcode,
            record.CategoryId is null ? null : ProductCategoryId.From(record.CategoryId.Value),
            UnitOfMeasures.Parse(record.UnitOfMeasure),
            record.SellingPrice,
            Enum.Parse<CatalogProductStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static CatalogProductRecord ToRecord(CatalogProduct product) =>
        new()
        {
            Id = product.Id.Value,
            OrganizationId = product.OrganizationId.Value,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            NormalizedSku = product.NormalizedSku,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId?.Value,
            UnitOfMeasure = UnitOfMeasures.ToCode(product.UnitOfMeasure),
            SellingPrice = product.SellingPrice,
            Status = product.Status.ToString(),
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };

    public static void ApplyToRecord(CatalogProduct product, CatalogProductRecord record)
    {
        record.Name = product.Name;
        record.Description = product.Description;
        record.Sku = product.Sku;
        record.NormalizedSku = product.NormalizedSku;
        record.Barcode = product.Barcode;
        record.CategoryId = product.CategoryId?.Value;
        record.UnitOfMeasure = UnitOfMeasures.ToCode(product.UnitOfMeasure);
        record.SellingPrice = product.SellingPrice;
        record.Status = product.Status.ToString();
        record.UpdatedAtUtc = product.UpdatedAtUtc;
        // OrganizationId is immutable — never rewritten from the aggregate.
    }
}
