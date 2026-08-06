using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

public interface ICatalogImportJobRepository
{
    Task<CatalogImportJob?> GetByIdAsync(
        PosOrganizationId organizationId,
        CatalogImportJobId jobId,
        CancellationToken cancellationToken = default);

    Task<CatalogImportJob?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<CatalogImportJob?> ClaimNextAsync(
        DateTimeOffset utcNow,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CatalogImportItemResult> Items, int TotalCount)> ListItemsAsync(
        PosOrganizationId organizationId,
        CatalogImportJobId jobId,
        PosCatalogImportItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default);

    Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default);
}

/// <summary>POS → Platform merchant discovery client for published templates and active products.</summary>
/// <remarks>
/// Platform merchant catalog routes authenticate with <c>PlatformSession</c> (not product Bearer tokens).
/// Callers should pass the Platform session token; the typed client also reads
/// <c>X-ExItS-Session-Token</c> from the current POS request when present.
/// </remarks>
public interface IPlatformMerchantCatalogClient
{
    Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
        Guid templateId,
        string? platformSessionToken,
        CancellationToken cancellationToken = default);

    Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(
        Guid productId,
        string? platformSessionToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(
        IReadOnlyList<Guid> productIds,
        string? platformSessionToken,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PlatformMerchantGlobalProductDto>> SearchActiveProductsAsync(
        string? search,
        Guid? categoryId,
        string? businessType,
        string? barcode,
        string? sku,
        int? page,
        int? pageSize,
        string? platformSessionToken,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PlatformMerchantGlobalCategoryDto>> ListActiveCategoriesAsync(
        string? search,
        string? businessType,
        Guid? parentId,
        int? page,
        int? pageSize,
        string? platformSessionToken,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformMerchantCatalogTemplateProductDto(
    Guid Id,
    Guid GlobalProductId,
    int SortOrder,
    bool IsFeatured,
    bool IsFirstBatch,
    string? ProductName = null,
    string? Sku = null,
    string? Barcode = null,
    string? Brand = null,
    Guid? CategoryId = null,
    string? CategoryName = null,
    string? Status = null,
    string? Unit = null,
    decimal? CostPrice = null,
    decimal? SellingPrice = null);

public sealed record PlatformMerchantCatalogTemplateSummaryDto(
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

public sealed record PlatformMerchantCatalogTemplateDto(
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
    IReadOnlyList<PlatformMerchantCatalogTemplateProductDto> Products,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlatformMerchantGlobalProductDto(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    string? Barcode,
    string? Brand,
    Guid? GlobalCategoryId,
    string Unit,
    decimal? CostPrice,
    decimal? SellingPrice,
    string? ImageReference,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlatformMerchantGlobalCategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    string? IconReference,
    int SortOrder,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ImportTemplateBatchRequest(
    Guid PlatformTemplateId,
    int BatchNumber = 1,
    string? IdempotencyKey = null);

public sealed record ImportSelectedProductsRequest(
    IReadOnlyList<Guid> PlatformGlobalProductIds,
    string? IdempotencyKey = null);

public sealed record PosCatalogImportJobDto(
    Guid JobId,
    Guid OrganizationId,
    string JobKind,
    Guid? PlatformTemplateId,
    int? BatchNumber,
    string CatalogSource,
    string Status,
    int TotalCount,
    int ProcessedCount,
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    string? CurrentStage,
    string? ErrorSummary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record PosCatalogImportItemDto(
    Guid ItemId,
    Guid PlatformGlobalProductId,
    int SortOrder,
    string Name,
    string? Sku,
    string? Barcode,
    string UnitOfMeasure,
    decimal SuggestedPrice,
    string Status,
    Guid? LocalProductId,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset? ProcessedAtUtc);
