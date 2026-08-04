using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

public interface IGlobalCategoryRepository
{
    Task<GlobalCategory?> GetByIdAsync(GlobalCategoryId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithNameUnderParentAsync(
        string name,
        GlobalCategoryId? parentId,
        GlobalCategoryId? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task AddAsync(GlobalCategory category, CancellationToken cancellationToken = default);
    Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default);
}

public interface IGlobalProductRepository
{
    Task<GlobalProduct?> GetByIdAsync(GlobalProductId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithBarcodeAsync(
        string barcode,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsWithSkuAsync(
        string sku,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GlobalProduct> Items, int TotalCount)> ListAsync(
        GlobalProductStatus? status,
        GlobalCategoryId? categoryId,
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default);
    Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default);
}

public interface ICatalogTemplateRepository
{
    Task<CatalogTemplate?> GetByIdAsync(CatalogTemplateId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithSlugAsync(
        string slug,
        CatalogTemplateId? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CatalogTemplate> Items, int TotalCount)> ListAsync(
        CatalogTemplateStatus? status,
        BusinessType? primaryBusinessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(CatalogTemplate template, CancellationToken cancellationToken = default);
}

public interface ICatalogImportJobRepository
{
    Task<CatalogImportJob?> GetByIdAsync(CatalogImportJobId id, CancellationToken cancellationToken = default);

    Task<CatalogImportJob?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CatalogImportJob> Items, int TotalCount)> ListAsync(
        CatalogImportJobStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogImportErrorDto>> ListErrorsAsync(
        CatalogImportJobId id,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Claims the next Queued job, or a stale Processing job past the heartbeat threshold.</summary>
    Task<CatalogImportJob?> ClaimNextAsync(
        DateTimeOffset utcNow,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default);

    Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default);
}

public interface ICatalogImportFileParser
{
    Task<IReadOnlyList<CatalogImportRawRow>> ParseAsync(
        Stream content,
        CatalogImportFileFormat format,
        CancellationToken cancellationToken = default);
}
