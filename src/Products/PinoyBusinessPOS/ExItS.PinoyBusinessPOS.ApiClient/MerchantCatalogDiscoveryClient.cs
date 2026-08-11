using System.Globalization;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Platform merchant discovery via <c>/api/v1/catalog/*</c> on <see cref="IPosApiClient"/> (Platform base URL).
/// </summary>
public sealed class MerchantCatalogDiscoveryClient(IPosApiClient api) : IMerchantCatalogDiscoveryClient
{
    public Task<ApiResult<PlatformPagedResult<PlatformMerchantCatalogTemplateSummaryDto>>> ListPublishedTemplatesAsync(
        string? businessTypeCode = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        Guid? businessTypeId = null,
        CancellationToken ct = default)
    {
        var path = new StringBuilder("/api/v1/catalog/templates?");
        path.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        path.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(path, "businessTypeCode", businessTypeCode);
        AppendOptional(path, "search", search);
        if (businessTypeId is { } id && id != Guid.Empty)
        {
            path.Append("&businessTypeId=").Append(id.ToString("D"));
        }
        return api.GetAsync<PlatformPagedResult<PlatformMerchantCatalogTemplateSummaryDto>>(path.ToString(), ct);
    }

    public Task<ApiResult<PlatformMerchantCatalogTemplateDto>> GetPublishedTemplateAsync(
        Guid templateId,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformMerchantCatalogTemplateDto>($"/api/v1/catalog/templates/{templateId:D}", ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMerchantGlobalProductDto>>> SearchActiveProductsAsync(
        string? search = null,
        Guid? categoryId = null,
        string? businessTypeCode = null,
        string? barcode = null,
        string? sku = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var path = new StringBuilder("/api/v1/catalog/products/search?");
        path.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        path.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(path, "q", search);
        AppendOptional(path, "businessTypeCode", businessTypeCode);
        AppendOptional(path, "barcode", barcode);
        AppendOptional(path, "sku", sku);
        if (categoryId is not null && categoryId.Value != Guid.Empty)
        {
            path.Append("&categoryId=").Append(categoryId.Value.ToString("D"));
        }

        return api.GetAsync<PlatformPagedResult<PlatformMerchantGlobalProductDto>>(path.ToString(), ct);
    }

    public Task<ApiResult<PlatformMerchantGlobalProductDto>> GetActiveProductAsync(
        Guid productId,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformMerchantGlobalProductDto>($"/api/v1/catalog/products/{productId:D}", ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMerchantGlobalCategoryDto>>> ListActiveCategoriesAsync(
        string? search = null,
        string? businessTypeCode = null,
        Guid? parentId = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var path = new StringBuilder("/api/v1/catalog/categories?");
        path.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        path.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(path, "search", search);
        AppendOptional(path, "businessTypeCode", businessTypeCode);
        if (parentId is not null && parentId.Value != Guid.Empty)
        {
            path.Append("&parentId=").Append(parentId.Value.ToString("D"));
        }

        return api.GetAsync<PlatformPagedResult<PlatformMerchantGlobalCategoryDto>>(path.ToString(), ct);
    }

    private static void AppendOptional(StringBuilder query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value.Trim()));
        }
    }
}
