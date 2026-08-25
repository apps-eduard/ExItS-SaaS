using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Api.Common;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Catalog;

public sealed class PlatformMerchantCatalogClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : IPlatformMerchantCatalogClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PlatformMerchantCatalogTemplateDto?> GetPublishedTemplateAsync(
        Guid templateId,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        EnsureBaseAddress();
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/catalog/templates/{templateId:D}", platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content
            .ReadFromJsonAsync<PlatformMerchantCatalogTemplateDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PlatformMerchantGlobalProductDto?> GetActiveProductAsync(
        Guid productId,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        EnsureBaseAddress();
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/catalog/products/{productId:D}", platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content
            .ReadFromJsonAsync<PlatformMerchantGlobalProductDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PlatformMerchantGlobalProductDto>> GetActiveProductsAsync(
        IReadOnlyList<Guid> productIds,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var results = new System.Collections.Concurrent.ConcurrentBag<PlatformMerchantGlobalProductDto>();
        try
        {
            await Parallel.ForEachAsync(
                    ids,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 8,
                        CancellationToken = cancellationToken
                    },
                    async (id, ct) =>
                    {
                        var product = await GetActiveProductAsync(id, platformSessionToken, ct).ConfigureAwait(false);
                        if (product is not null)
                        {
                            results.Add(product);
                        }
                    })
                .ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            throw UnwrapAggregate(ex);
        }

        return results.ToList();
    }

    public async Task<PagedResult<PlatformMerchantGlobalProductDto>> SearchActiveProductsAsync(
        string? search,
        Guid? categoryId,
        string? businessTypeCode,
        string? barcode,
        string? sku,
        int? page,
        int? pageSize,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        EnsureBaseAddress();
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"q={Uri.EscapeDataString(search.Trim())}");
        }

        if (categoryId is Guid cid)
        {
            query.Add($"categoryId={cid:D}");
        }

        if (!string.IsNullOrWhiteSpace(businessTypeCode))
        {
            query.Add($"businessTypeCode={Uri.EscapeDataString(businessTypeCode.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            query.Add($"barcode={Uri.EscapeDataString(barcode.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            query.Add($"sku={Uri.EscapeDataString(sku.Trim())}");
        }

        if (page is int p)
        {
            query.Add($"page={p}");
        }

        if (pageSize is int ps)
        {
            query.Add($"pageSize={ps}");
        }

        var path = "api/v1/catalog/products/search" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        using var request = CreateRequest(HttpMethod.Get, path, platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<PlatformMerchantGlobalProductDto>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return result ?? new PagedResult<PlatformMerchantGlobalProductDto>([], 0, page ?? 1, pageSize ?? 20);
    }

    public async Task<PagedResult<PlatformMerchantGlobalCategoryDto>> ListActiveCategoriesAsync(
        string? search,
        string? businessTypeCode,
        Guid? parentId,
        int? page,
        int? pageSize,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        EnsureBaseAddress();
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(businessTypeCode))
        {
            query.Add($"businessTypeCode={Uri.EscapeDataString(businessTypeCode.Trim())}");
        }

        if (parentId is Guid pid)
        {
            query.Add($"parentId={pid:D}");
        }

        if (page is int p)
        {
            query.Add($"page={p}");
        }

        if (pageSize is int ps)
        {
            query.Add($"pageSize={ps}");
        }

        var path = "api/v1/catalog/categories" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        using var request = CreateRequest(HttpMethod.Get, path, platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<PlatformMerchantGlobalCategoryDto>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return result ?? new PagedResult<PlatformMerchantGlobalCategoryDto>([], 0, page ?? 1, pageSize ?? 20);
    }

    public async Task<IReadOnlyList<PlatformGlobalProductImageMetaDto>> ListProductImageMetaAsync(
        IReadOnlyList<Guid> productIds,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().Take(50).ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        EnsureBaseAddress();
        var path = "api/v1/catalog/products/image-meta?ids=" + string.Join(",", ids.Select(id => id.ToString("D")));
        using var request = CreateRequest(HttpMethod.Get, path, platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var result = await response.Content
            .ReadFromJsonAsync<List<PlatformGlobalProductImageMetaDto>>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return result ?? [];
    }

    public async Task<ProductImageBytes?> GetProductImageAsync(
        Guid productId,
        string variant,
        string? platformSessionToken,
        CancellationToken cancellationToken = default)
    {
        EnsureBaseAddress();
        using var request = CreateRequest(
            HttpMethod.Get,
            $"api/v1/catalog/products/{productId:D}/image/{Uri.EscapeDataString(variant)}",
            platformSessionToken);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return null;
        }

        await EnsureMerchantCatalogSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            return null;
        }

        var version = 0;
        if (response.Headers.TryGetValues("X-ExItS-Image-Version", out var values)
            && int.TryParse(values.FirstOrDefault(), out var parsed))
        {
            version = parsed;
        }

        return new ProductImageBytes(bytes, "image/webp", version);
    }

    private void EnsureBaseAddress()
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for merchant catalog discovery.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, string? platformSessionToken)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var token = platformSessionToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            var http = httpContextAccessor.HttpContext?.Request;
            if (http is not null)
            {
                var header = http.Headers["X-ExItS-Session-Token"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    token = header.Trim();
                }
                else if (http.Cookies.TryGetValue(".ExItS.Platform.Auth", out var cookieToken)
                         && !string.IsNullOrWhiteSpace(cookieToken))
                {
                    token = cookieToken.Trim();
                }
            }
        }

        // Merchant catalog routes (/api/v1/catalog/*) authenticate via PlatformSession only.
        // Product access Bearer tokens are rejected with 401 by Platform session auth.
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
            request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
        }

        return request;
    }

    private static async Task EnsureMerchantCatalogSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var (title, detail, errorCode) = TryReadProblem(body);
        var message = FirstNonBlank(detail, title)
                      ?? $"Platform catalog request failed with {(int)status} {status}.";

        if (IsTransientStatus(status))
        {
            throw new PlatformMerchantCatalogTransientException(message);
        }

        throw new PlatformMerchantCatalogRequestException(status, message, errorCode);
    }

    private static bool IsTransientStatus(HttpStatusCode status) =>
        status is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (int)status >= 500;

    private static (string? Title, string? Detail, string? ErrorCode) TryReadProblem(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (null, null, null);
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            string? title = root.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            string? detail = root.TryGetProperty("detail", out var detailEl) ? detailEl.GetString() : null;
            string? errorCode = null;
            if (root.TryGetProperty("errorCode", out var codeEl))
            {
                errorCode = codeEl.GetString();
            }
            else if (root.TryGetProperty("extensions", out var extensions)
                     && extensions.ValueKind == JsonValueKind.Object
                     && extensions.TryGetProperty("errorCode", out var extCode))
            {
                errorCode = extCode.GetString();
            }

            return (title, detail, errorCode);
        }
        catch (JsonException)
        {
            return (null, null, null);
        }
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static Exception UnwrapAggregate(AggregateException ex)
    {
        var flattened = ex.Flatten();
        return flattened.InnerExceptions.Count == 1
            ? flattened.InnerExceptions[0]
            : flattened;
    }
}
