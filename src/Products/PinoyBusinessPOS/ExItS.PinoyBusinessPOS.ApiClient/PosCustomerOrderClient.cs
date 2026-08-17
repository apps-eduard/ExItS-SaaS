using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosCustomerOrderClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosCustomerOrderClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<CustomerOrderPagedResult>> ListSellerOrdersAsync(
        Guid organizationId,
        string? status = null,
        string? fulfillmentType = null,
        Guid? branchId = null,
        string? orderNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(SellerPath(organizationId)).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "status", status);
        AppendOptional(query, "fulfillmentType", fulfillmentType);
        AppendOptional(query, "orderNumber", orderNumber);
        if (branchId is Guid id)
        {
            query.Append("&branchId=").Append(id.ToString("D"));
        }

        return SendAsync<CustomerOrderPagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<CustomerOrderDto>> GetSellerOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        SendAsync<CustomerOrderDto>(HttpMethod.Get, $"{SellerPath(organizationId)}/{orderId:D}", null, null, ct);

    public Task<ApiResult<CustomerOrderDto>> PlaceSellerOrderAsync(
        Guid organizationId,
        PlaceCustomerOrderRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.ClientOrderId is Guid orderId && orderId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                orderId,
                json,
                OfflineOperationTypes.CustomerOrderPlace);
        }

        return SendAsync<CustomerOrderDto>(HttpMethod.Post, SellerPath(organizationId), request, headers, ct);
    }

    public Task<ApiResult<QuoteCustomerOrderDeliveryDto>> QuoteDeliveryAsync(
        Guid organizationId,
        QuoteCustomerOrderDeliveryRequest request,
        CancellationToken ct = default) =>
        SendAsync<QuoteCustomerOrderDeliveryDto>(
            HttpMethod.Post,
            $"{SellerPath(organizationId)}/quote-delivery",
            request,
            null,
            ct);

    public Task<ApiResult<CustomerOrderDto>> AcceptAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        SendAsync<CustomerOrderDto>(
            HttpMethod.Post,
            $"{SellerPath(organizationId)}/{orderId:D}/accept",
            new { },
            PosMutationIdempotencyHelper.BuildHeaders(orderId, "{}", OfflineOperationTypes.CustomerOrderAccept),
            ct);

    public Task<ApiResult<CustomerOrderDto>> RejectAsync(
        Guid organizationId,
        Guid orderId,
        RejectCustomerOrderRequest request,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        return SendAsync<CustomerOrderDto>(
            HttpMethod.Post,
            $"{SellerPath(organizationId)}/{orderId:D}/reject",
            request,
            PosMutationIdempotencyHelper.BuildHeaders(orderId, json, OfflineOperationTypes.CustomerOrderReject),
            ct);
    }

    public Task<ApiResult<CustomerOrderDto>> CompleteAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        SendAsync<CustomerOrderDto>(
            HttpMethod.Post,
            $"{SellerPath(organizationId)}/{orderId:D}/complete",
            new { },
            PosMutationIdempotencyHelper.BuildHeaders(orderId, "{}", OfflineOperationTypes.CustomerOrderComplete),
            ct);

    public Task<ApiResult<CustomerOrderDto>> StartPreparingAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        PostFulfillmentAsync(organizationId, orderId, "start-preparing", ct);

    public Task<ApiResult<CustomerOrderDto>> MarkReadyAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        PostFulfillmentAsync(organizationId, orderId, "mark-ready", ct);

    public Task<ApiResult<CustomerOrderDto>> MarkOutForDeliveryAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        PostFulfillmentAsync(organizationId, orderId, "mark-out-for-delivery", ct);

    public Task<ApiResult<CustomerOrderDto>> MarkDeliveredAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        PostFulfillmentAsync(organizationId, orderId, "mark-delivered", ct);

    public Task<ApiResult<CustomerOrderDto>> MarkCollectedAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken ct = default) =>
        PostFulfillmentAsync(organizationId, orderId, "mark-collected", ct);

    public Task<ApiResult<CustomerOrderPagedResult>> ListMineAsync(
        string? partyType = null,
        Guid? buyerOrganizationId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder("/api/v1/pos/customer-orders/mine?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "partyType", partyType);
        if (buyerOrganizationId is Guid id)
        {
            query.Append("&buyerOrganizationId=").Append(id.ToString("D"));
        }

        return SendAsync<CustomerOrderPagedResult>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public Task<ApiResult<CustomerOrderDto>> GetMineAsync(
        Guid orderId,
        string? partyType = null,
        Guid? buyerOrganizationId = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"/api/v1/pos/customer-orders/mine/{orderId:D}?");
        AppendOptional(query, "partyType", partyType);
        if (buyerOrganizationId is Guid id)
        {
            query.Append("&buyerOrganizationId=").Append(id.ToString("D"));
        }

        return SendAsync<CustomerOrderDto>(HttpMethod.Get, query.ToString().TrimEnd('?'), null, null, ct);
    }

    public Task<ApiResult<CustomerOrderDto>> PlaceAsCustomerAsync(
        Guid sellerOrganizationId,
        PlaceCustomerOrderRequest request,
        CancellationToken ct = default)
    {
        IReadOnlyDictionary<string, string>? headers = null;
        if (request.ClientOrderId is Guid orderId && orderId != Guid.Empty)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            headers = PosMutationIdempotencyHelper.BuildHeaders(
                orderId,
                json,
                OfflineOperationTypes.CustomerOrderPlace);
        }

        return SendAsync<CustomerOrderDto>(
            HttpMethod.Post,
            $"/api/v1/pos/customer-orders/organizations/{sellerOrganizationId:D}",
            request,
            headers,
            ct);
    }

    public Task<ApiResult<CustomerStorefrontDto>> GetStorefrontAsync(
        Guid sellerOrganizationId,
        string? search = null,
        Guid? categoryId = null,
        int page = 1,
        int pageSize = 40,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(
            $"/api/v1/pos/customer-orders/organizations/{sellerOrganizationId:D}/storefront?");
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "search", search);
        if (categoryId is Guid id)
        {
            query.Append("&categoryId=").Append(id.ToString("D"));
        }

        return SendAsync<CustomerStorefrontDto>(HttpMethod.Get, query.ToString(), null, null, ct);
    }

    public async Task<ApiResult<ProductImageBytes>> GetStorefrontProductImageAsync(
        Guid sellerOrganizationId,
        Guid productId,
        string variant,
        CancellationToken ct = default)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return ApiResult<ProductImageBytes>.Offline(
                new ApiError("Offline", "No network connectivity detected.", null, null, null));
        }

        var path =
            $"/api/v1/pos/customer-orders/organizations/{sellerOrganizationId:D}/products/{productId:D}/image/{Uri.EscapeDataString(variant)}";
        try
        {
            using var response = await httpClient.GetAsync(path, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                    ? values.FirstOrDefault()
                    : null;
                return ClassifyFailure<ProductImageBytes>(response.StatusCode, body, correlationId);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/webp";
            return ApiResult<ProductImageBytes>.Success(new ProductImageBytes(bytes, contentType, 0));
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<ProductImageBytes>.Timeout(
                new ApiError("Timeout", "The request timed out.", null, null, null));
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<ProductImageBytes>.Unavailable(
                new ApiError("Unavailable", ex.Message, null, null, null));
        }
    }

    public Task<ApiResult<QuoteCustomerOrderDeliveryDto>> QuoteDeliveryAsCustomerAsync(
        Guid sellerOrganizationId,
        QuoteCustomerOrderDeliveryRequest request,
        CancellationToken ct = default) =>
        SendAsync<QuoteCustomerOrderDeliveryDto>(
            HttpMethod.Post,
            $"/api/v1/pos/customer-orders/organizations/{sellerOrganizationId:D}/quote-delivery",
            request,
            null,
            ct);

    private Task<ApiResult<CustomerOrderDto>> PostFulfillmentAsync(
        Guid organizationId,
        Guid orderId,
        string action,
        CancellationToken ct) =>
        SendAsync<CustomerOrderDto>(
            HttpMethod.Post,
            $"{SellerPath(organizationId)}/{orderId:D}/{action}",
            new { },
            null,
            ct);

    private static string SellerPath(Guid organizationId) =>
        $"/api/v1/pos/organizations/{organizationId:D}/customer-orders";

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
        IReadOnlyDictionary<string, string>? headers,
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

            if (headers is not null)
            {
                foreach (var pair in headers)
                {
                    request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                }
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault()
                : null;

            if (response.IsSuccessStatusCode)
            {
                var data = string.IsNullOrWhiteSpace(content)
                    ? default
                    : JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
                return ApiResult<TResponse>.Success(data!);
            }

            return ClassifyFailure<TResponse>(response.StatusCode, content, correlationId);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<TResponse>.Timeout(new ApiError("Timeout", "The request timed out.", null, null, null));
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<TResponse>.Unavailable(
                new ApiError("Unavailable", ex.Message, null, null, null));
        }
    }

    private static ApiResult<TResponse> ClassifyFailure<TResponse>(
        HttpStatusCode statusCode,
        string content,
        string? correlationId)
    {
        var error = ApiProblemParser.Parse(content, correlationId, (int)statusCode);
        return statusCode switch
        {
            HttpStatusCode.NotFound => ApiResult<TResponse>.NotFound(error),
            HttpStatusCode.BadRequest => ApiResult<TResponse>.Validation(error),
            HttpStatusCode.Conflict => ApiResult<TResponse>.Conflict(error),
            HttpStatusCode.Unauthorized => ApiResult<TResponse>.Unauthorized(error),
            HttpStatusCode.Forbidden => ApiResult<TResponse>.Forbidden(error),
            HttpStatusCode.TooManyRequests => ApiResult<TResponse>.RateLimited(error),
            _ => ApiResult<TResponse>.Failed(error)
        };
    }
}
