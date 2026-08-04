using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class GlobalCatalogEntityMapper
{
    public static GlobalCategory ToDomain(GlobalCategoryRecord record) =>
        GlobalCategory.Rehydrate(
            GlobalCategoryId.From(record.Id),
            record.Name,
            record.ParentId is null ? null : GlobalCategoryId.From(record.ParentId.Value),
            record.IconReference,
            record.SortOrder,
            Enum.Parse<GlobalCategoryStatus>(record.Status),
            record.BusinessTypes.Select(b => Enum.Parse<BusinessType>(b.BusinessType)),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static GlobalCategoryRecord ToRecord(GlobalCategory category) =>
        new()
        {
            Id = category.Id.Value,
            Name = category.Name,
            NormalizedName = category.Name.ToUpperInvariant(),
            ParentId = category.ParentId?.Value,
            IconReference = category.IconReference,
            SortOrder = category.SortOrder,
            Status = category.Status.ToString(),
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc,
            BusinessTypes = category.BusinessTypes
                .Select(t => new GlobalCategoryBusinessTypeRecord
                {
                    CategoryId = category.Id.Value,
                    BusinessType = t.ToString()
                })
                .ToList()
        };

    public static void ApplyToRecord(GlobalCategory category, GlobalCategoryRecord record)
    {
        record.Name = category.Name;
        record.NormalizedName = category.Name.ToUpperInvariant();
        record.ParentId = category.ParentId?.Value;
        record.IconReference = category.IconReference;
        record.SortOrder = category.SortOrder;
        record.Status = category.Status.ToString();
        record.UpdatedAtUtc = category.UpdatedAtUtc;
        record.BusinessTypes.Clear();
        foreach (var type in category.BusinessTypes)
        {
            record.BusinessTypes.Add(new GlobalCategoryBusinessTypeRecord
            {
                CategoryId = category.Id.Value,
                BusinessType = type.ToString()
            });
        }
    }

    public static GlobalProduct ToDomain(GlobalProductRecord record) =>
        GlobalProduct.Rehydrate(
            GlobalProductId.From(record.Id),
            record.Name,
            record.Description,
            record.Sku,
            record.Barcode,
            record.GlobalCategoryId is null ? null : GlobalCategoryId.From(record.GlobalCategoryId.Value),
            Enum.Parse<ProductUnit>(record.Unit),
            record.SuggestedPrice,
            record.SuggestedCost,
            record.ImageReference,
            Enum.Parse<GlobalProductStatus>(record.Status),
            record.SearchTags ?? [],
            record.BusinessTypes.Select(b => Enum.Parse<BusinessType>(b.BusinessType)),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static GlobalProductRecord ToRecord(GlobalProduct product) =>
        new()
        {
            Id = product.Id.Value,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Barcode = product.Barcode,
            GlobalCategoryId = product.GlobalCategoryId?.Value,
            Unit = product.Unit.ToString(),
            SuggestedPrice = product.SuggestedPrice,
            SuggestedCost = product.SuggestedCost,
            ImageReference = product.ImageReference,
            Status = product.Status.ToString(),
            SearchTags = product.SearchTags.ToArray(),
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc,
            BusinessTypes = product.BusinessTypes
                .Select(t => new GlobalProductBusinessTypeRecord
                {
                    ProductId = product.Id.Value,
                    BusinessType = t.ToString()
                })
                .ToList()
        };

    public static void ApplyToRecord(GlobalProduct product, GlobalProductRecord record)
    {
        record.Name = product.Name;
        record.Description = product.Description;
        record.Sku = product.Sku;
        record.Barcode = product.Barcode;
        record.GlobalCategoryId = product.GlobalCategoryId?.Value;
        record.Unit = product.Unit.ToString();
        record.SuggestedPrice = product.SuggestedPrice;
        record.SuggestedCost = product.SuggestedCost;
        record.ImageReference = product.ImageReference;
        record.Status = product.Status.ToString();
        record.SearchTags = product.SearchTags.ToArray();
        record.UpdatedAtUtc = product.UpdatedAtUtc;
        record.BusinessTypes.Clear();
        foreach (var type in product.BusinessTypes)
        {
            record.BusinessTypes.Add(new GlobalProductBusinessTypeRecord
            {
                ProductId = product.Id.Value,
                BusinessType = type.ToString()
            });
        }
    }
}
