using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS catalog client. Online-only for P8-WP01: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no mutation is ever queued locally.
/// </summary>
public sealed class PosCatalogClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosCatalogClient
{
    private const string CategoriesPath = "/api/v1/pos/catalog/categories";
    private const string ProductsPath = "/api/v1/pos/catalog/products";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PosProductCategoryPagedResult>> ListCategoriesAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(CategoriesPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        AppendOptional(query, "status", status);
        return SendAsync<PosProductCategoryPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosProductCategoryDto>> GetCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        SendAsync<PosProductCategoryDto>(HttpMethod.Get, $"{CategoriesPath}/{categoryId:D}", null, ct);

    public Task<ApiResult<PosProductCategoryDto>> CreateCategoryAsync(
        CreatePosProductCategoryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosProductCategoryDto>(HttpMethod.Post, CategoriesPath, request, ct);

    public Task<ApiResult<PosProductCategoryDto>> UpdateCategoryAsync(
        Guid categoryId,
        UpdatePosProductCategoryRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosProductCategoryDto>(HttpMethod.Put, $"{CategoriesPath}/{categoryId:D}", request, ct);

    public Task<ApiResult<PosProductCategoryDto>> DeactivateCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default) =>
        SendAsync<PosProductCategoryDto>(HttpMethod.Post, $"{CategoriesPath}/{categoryId:D}/deactivate", null, ct);

    public Task<ApiResult<PosProductCategoryDto>> ReactivateCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default) =>
        SendAsync<PosProductCategoryDto>(HttpMethod.Post, $"{CategoriesPath}/{categoryId:D}/reactivate", null, ct);

    public Task<ApiResult<PosCatalogProductPagedResult>> ListProductsAsync(
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        string? unitOfMeasure = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(ProductsPath).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        AppendOptional(query, "status", status);
        AppendOptional(query, "unitOfMeasure", unitOfMeasure);
        if (categoryId is not null && categoryId.Value != Guid.Empty)
        {
            query.Append("&categoryId=").Append(categoryId.Value.ToString("D"));
        }

        return SendAsync<PosCatalogProductPagedResult>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosCatalogProductDto>> GetProductAsync(Guid productId, CancellationToken ct = default) =>
        SendAsync<PosCatalogProductDto>(HttpMethod.Get, $"{ProductsPath}/{productId:D}", null, ct);

    public Task<ApiResult<PosCatalogProductDto>> CreateProductAsync(
        CreatePosCatalogProductRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogProductDto>(HttpMethod.Post, ProductsPath, request, ct);

    public Task<ApiResult<PosCatalogProductDto>> UpdateProductAsync(
        Guid productId,
        UpdatePosCatalogProductRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogProductDto>(HttpMethod.Put, $"{ProductsPath}/{productId:D}", request, ct);

    public Task<ApiResult<UpdatePosCatalogProductPricesResponse>> UpdateProductPricesAsync(
        UpdatePosCatalogProductPricesRequest request,
        CancellationToken ct = default) =>
        SendAsync<UpdatePosCatalogProductPricesResponse>(HttpMethod.Post, $"{ProductsPath}/prices", request, ct);

    public Task<ApiResult<PosCatalogProductDto>> DeactivateProductAsync(
        Guid productId,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogProductDto>(HttpMethod.Post, $"{ProductsPath}/{productId:D}/deactivate", null, ct);

    public Task<ApiResult<PosCatalogProductDto>> ReactivateProductAsync(
        Guid productId,
        CancellationToken ct = default) =>
        SendAsync<PosCatalogProductDto>(HttpMethod.Post, $"{ProductsPath}/{productId:D}/reactivate", null, ct);

    public Task<ApiResult<PosCatalogProductDto>> LookupBySkuAsync(
        string sku,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var path = $"{ProductsPath}/by-sku/{Uri.EscapeDataString(sku?.Trim() ?? string.Empty)}";
        if (includeInactive)
        {
            path += "?includeInactive=true";
        }

        return SendAsync<PosCatalogProductDto>(HttpMethod.Get, path, null, ct);
    }

    public Task<ApiResult<PosCatalogProductDto>> LookupByBarcodeAsync(
        string barcode,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var path = $"{ProductsPath}/by-barcode/{Uri.EscapeDataString(barcode?.Trim() ?? string.Empty)}";
        if (includeInactive)
        {
            path += "?includeInactive=true";
        }

        return SendAsync<PosCatalogProductDto>(HttpMethod.Get, path, null, ct);
    }

    private static void AppendOptional(StringBuilder query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value.Trim()));
        }
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Offline", "No network connectivity detected.", null, null, null)
            };
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<TResponse>
                {
                    Status = Classify(response.StatusCode),
                    Error = ParseProblem(content, correlationId, (int)response.StatusCode)
                };
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                };
            }

            var data = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
            return data is null
                ? new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                }
                : ApiResult<TResponse>.Success(data);
        }
        catch (JsonException ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Invalid response", ex.Message, null, null, null)
            };
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Network unavailable", ex.Message, null, null, null)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse> { Status = ApiCallStatus.Cancelled };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Request timed out", ex.Message, null, null, null)
            };
        }
        catch (Exception ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Failed,
                Error = new ApiError("Request failed", ex.Message, null, null, null)
            };
        }
    }

    private static ApiError ParseProblem(string content, string? correlationId, int statusCode)
    {
        string? title = null;
        string? detail = null;
        string? errorCode = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    title = t.GetString();
                }

                if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    detail = d.GetString();
                }

                if (root.TryGetProperty("errorCode", out var e) && e.ValueKind == JsonValueKind.String)
                {
                    errorCode = e.GetString();
                }
            }
            catch (JsonException)
            {
            }
        }

        return new ApiError(title, detail, errorCode, correlationId, statusCode);
    }

    private static ApiCallStatus Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallStatus.Validation,
        HttpStatusCode.Conflict => ApiCallStatus.Conflict,
        HttpStatusCode.Unauthorized => ApiCallStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ApiCallStatus.Timeout,
        _ when (int)statusCode >= 500 => ApiCallStatus.Unavailable,
        _ => ApiCallStatus.Failed
    };
}
