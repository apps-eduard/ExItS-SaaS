using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS catalog API client. Online-only for P8-WP01 — no offline cache or queued mutations.
/// </summary>
public interface IPosCatalogClient
{
    Task<ApiResult<PosProductCategoryPagedResult>> ListCategoriesAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosProductCategoryDto>> GetCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosProductCategoryDto>> CreateCategoryAsync(
        CreatePosProductCategoryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosProductCategoryDto>> UpdateCategoryAsync(
        Guid categoryId,
        UpdatePosProductCategoryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosProductCategoryDto>> DeactivateCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosProductCategoryDto>> ReactivateCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductPagedResult>> ListProductsAsync(
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        string? unitOfMeasure = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> GetProductAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> CreateProductAsync(
        CreatePosCatalogProductRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> UpdateProductAsync(
        Guid productId,
        UpdatePosCatalogProductRequest request,
        CancellationToken ct = default);

    /// <summary>Today's Prices: bulk current selling-price update (online-only).</summary>
    Task<ApiResult<UpdatePosCatalogProductPricesResponse>> UpdateProductPricesAsync(
        UpdatePosCatalogProductPricesRequest request,
        CancellationToken ct = default);

    Task<ApiResult<ConnectedBuyerAvailabilityQueryResultDto>> QueryConnectedBuyerAvailabilityAsync(
        string? query = null,
        Guid? categoryId = null,
        string? availabilityFilter = null,
        int page = 1,
        int pageSize = 25,
        bool uncategorizedOnly = false,
        CancellationToken ct = default);

    Task<ApiResult<BulkConnectedBuyerAvailabilityMutationResultDto>> BulkMutateConnectedBuyerAvailabilityAsync(
        BulkConnectedBuyerAvailabilityMutationRequest request,
        CancellationToken ct = default);

    Task<ApiResult<BulkDefaultConnectedPoPricingPreviewDto>> PreviewDefaultConnectedPoPricingAsync(
        BulkDefaultConnectedPoPricingRequest request,
        CancellationToken ct = default);

    Task<ApiResult<BulkConnectedBuyerAvailabilityMutationResultDto>> ApplyDefaultConnectedPoPricingAsync(
        BulkDefaultConnectedPoPricingRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> DeactivateProductAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> ReactivateProductAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> LookupBySkuAsync(
        string sku,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductDto>> LookupByBarcodeAsync(
        string barcode,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<ApiResult<PosCatalogProductImageDto>> UploadProductImageAsync(
        Guid productId,
        byte[] bytes,
        string fileName,
        CancellationToken ct = default);

    Task<ApiResult<bool>> RemoveProductImageAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<ProductImageBytes>> GetProductImageAsync(
        Guid productId,
        string variant,
        CancellationToken ct = default);
}
