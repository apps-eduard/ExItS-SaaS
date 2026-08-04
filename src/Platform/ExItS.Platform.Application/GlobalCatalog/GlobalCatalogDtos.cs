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
}
