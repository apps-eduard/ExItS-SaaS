using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed record GlobalCategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    string? IconReference,
    int SortOrder,
    string Status,
    IReadOnlyList<string> BusinessTypes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record GlobalProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    Guid? GlobalCategoryId,
    string Unit,
    decimal? SuggestedPrice,
    decimal? SuggestedCost,
    string? ImageReference,
    string Status,
    IReadOnlyList<string> SearchTags,
    IReadOnlyList<string> BusinessTypes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateGlobalCategoryRequest(
    string Name,
    Guid? ParentId = null,
    string? IconReference = null,
    int SortOrder = 0,
    IReadOnlyList<string>? BusinessTypes = null);

public sealed record UpdateGlobalCategoryRequest(
    string Name,
    Guid? ParentId = null,
    string? IconReference = null,
    int SortOrder = 0,
    IReadOnlyList<string>? BusinessTypes = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record SetGlobalCategoryStatusRequest(
    string Status,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CreateGlobalProductRequest(
    string Name,
    string Unit,
    string? Description = null,
    string? Sku = null,
    string? Barcode = null,
    Guid? GlobalCategoryId = null,
    decimal? SuggestedPrice = null,
    decimal? SuggestedCost = null,
    string? ImageReference = null,
    IReadOnlyList<string>? SearchTags = null,
    IReadOnlyList<string>? BusinessTypes = null);

public sealed record UpdateGlobalProductRequest(
    string Name,
    string Unit,
    string? Description = null,
    string? Sku = null,
    string? Barcode = null,
    Guid? GlobalCategoryId = null,
    decimal? SuggestedPrice = null,
    decimal? SuggestedCost = null,
    string? ImageReference = null,
    IReadOnlyList<string>? SearchTags = null,
    IReadOnlyList<string>? BusinessTypes = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record SetGlobalProductStatusRequest(
    string Status,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CatalogTemplateProductDto(
    Guid Id,
    Guid GlobalProductId,
    int SortOrder,
    bool IsFeatured,
    bool IsFirstBatch);

public sealed record CatalogTemplateDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconReference,
    string PrimaryBusinessType,
    string Status,
    int DefaultBatchSize,
    string SelectionMode,
    DateTimeOffset? PublishedAtUtc,
    int ProductCount,
    int FirstBatchCount,
    IReadOnlyList<CatalogTemplateProductDto> Products,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CatalogTemplateSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconReference,
    string PrimaryBusinessType,
    string Status,
    int DefaultBatchSize,
    string SelectionMode,
    DateTimeOffset? PublishedAtUtc,
    int ProductCount,
    int FirstBatchCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateCatalogTemplateRequest(
    string Name,
    string PrimaryBusinessType,
    string? Slug = null,
    string? Description = null,
    string? IconReference = null,
    int? DefaultBatchSize = null,
    string? SelectionMode = null);

public sealed record UpdateCatalogTemplateRequest(
    string Name,
    string PrimaryBusinessType,
    string? Slug = null,
    string? Description = null,
    string? IconReference = null,
    int? DefaultBatchSize = null,
    string? SelectionMode = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record AssignCatalogTemplateProductRequest(
    Guid GlobalProductId,
    bool IsFeatured = false,
    bool IsFirstBatch = false,
    int? SortOrder = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record ReorderCatalogTemplateProductsRequest(
    IReadOnlyList<Guid> OrderedGlobalProductIds,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record UpdateCatalogTemplateProductFlagsRequest(
    bool? IsFeatured = null,
    bool? IsFirstBatch = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record CatalogTemplateLifecycleRequest(
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

internal static class GlobalCatalogDtoMaps
{
    public static GlobalCategoryDto Map(GlobalCategory category) =>
        new(
            category.Id.Value,
            category.Name,
            category.ParentId?.Value,
            category.IconReference,
            category.SortOrder,
            category.Status.ToString(),
            category.BusinessTypes.Select(t => t.ToString()).ToList(),
            category.CreatedAtUtc,
            category.UpdatedAtUtc);

    public static GlobalProductDto Map(GlobalProduct product) =>
        new(
            product.Id.Value,
            product.Name,
            product.Description,
            product.Sku,
            product.Barcode,
            product.GlobalCategoryId?.Value,
            product.Unit.ToString(),
            product.SuggestedPrice,
            product.SuggestedCost,
            product.ImageReference,
            product.Status.ToString(),
            product.SearchTags.ToList(),
            product.BusinessTypes.Select(t => t.ToString()).ToList(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc);

    public static CatalogTemplateProductDto Map(CatalogTemplateProduct product) =>
        new(
            product.Id,
            product.GlobalProductId.Value,
            product.SortOrder,
            product.IsFeatured,
            product.IsFirstBatch);

    public static CatalogTemplateDto Map(CatalogTemplate template) =>
        new(
            template.Id.Value,
            template.Name,
            template.Slug,
            template.Description,
            template.IconReference,
            template.PrimaryBusinessType.ToString(),
            template.Status.ToString(),
            template.DefaultBatchSize,
            template.SelectionMode.ToString(),
            template.PublishedAtUtc,
            template.ProductCount,
            template.FirstBatchCount,
            template.Products.Select(Map).ToList(),
            template.CreatedAtUtc,
            template.UpdatedAtUtc);

    public static CatalogTemplateSummaryDto MapSummary(CatalogTemplate template) =>
        new(
            template.Id.Value,
            template.Name,
            template.Slug,
            template.Description,
            template.IconReference,
            template.PrimaryBusinessType.ToString(),
            template.Status.ToString(),
            template.DefaultBatchSize,
            template.SelectionMode.ToString(),
            template.PublishedAtUtc,
            template.ProductCount,
            template.FirstBatchCount,
            template.CreatedAtUtc,
            template.UpdatedAtUtc);
}
