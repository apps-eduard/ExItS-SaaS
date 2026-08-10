using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// MAUI → Platform merchant discovery for published templates and active global products.
/// Calls <c>/api/v1/catalog/*</c> on the Platform API base URL.
/// </summary>
public interface IMerchantCatalogDiscoveryClient
{
    Task<ApiResult<PlatformPagedResult<PlatformMerchantCatalogTemplateSummaryDto>>> ListPublishedTemplatesAsync(
        string? businessType = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        Guid? primaryBusinessTypeId = null,
        CancellationToken ct = default);

    Task<ApiResult<PlatformMerchantCatalogTemplateDto>> GetPublishedTemplateAsync(
        Guid templateId,
        CancellationToken ct = default);

    Task<ApiResult<PlatformPagedResult<PlatformMerchantGlobalProductDto>>> SearchActiveProductsAsync(
        string? search = null,
        Guid? categoryId = null,
        string? businessType = null,
        string? barcode = null,
        string? sku = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PlatformMerchantGlobalProductDto>> GetActiveProductAsync(
        Guid productId,
        CancellationToken ct = default);

    Task<ApiResult<PlatformPagedResult<PlatformMerchantGlobalCategoryDto>>> ListActiveCategoriesAsync(
        string? search = null,
        string? businessType = null,
        Guid? parentId = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);
}
