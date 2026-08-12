using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS catalog-import API client. Online-only — Platform outage fails discovery/import
/// but does not affect local selling via <see cref="IPosCatalogClient"/>.
/// </summary>
public interface IPosCatalogImportClient
{
    Task<ApiResult<PosCatalogImportJobDto>> ImportTemplateBatchAsync(
        ImportTemplateBatchRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogImportJobDto>> ImportSelectedProductsAsync(
        ImportSelectedProductsRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogImportJobDto>> ImportTemplateNextBatchAsync(
        Guid templateId,
        ImportTemplateBatchRequest? request = null,
        CancellationToken ct = default);

    Task<ApiResult<PosTemplateImportStatusDto>> GetTemplateImportStatusAsync(
        Guid templateId,
        CancellationToken ct = default);

    Task<ApiResult<ImportedGlobalProductsDto>> ListImportedGlobalProductsAsync(
        IReadOnlyList<Guid> platformGlobalProductIds,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogImportJobDto>> GetJobAsync(
        Guid jobId,
        CancellationToken ct = default);

    Task<ApiResult<PagedResult<PosCatalogImportItemDto>>> GetJobItemsAsync(
        Guid jobId,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}
