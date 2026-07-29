using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiClient(HttpClient httpClient) : IPlatformApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<ApiCallResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct = default) =>
        GetAsync<PortfolioSummaryDto>("/api/v1/platform/admin/portfolio-summary", ct);
    public Task<ApiCallResult<PagedResult<ProductDto>>> GetProductsAsync(int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductDto>>($"/api/v1/platform/catalog/products?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<ProductDto>> GetProductAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<ProductDto>($"/api/v1/platform/catalog/products/{id}", ct);
    public Task<ApiCallResult<ProductOverviewDto>> GetProductOverviewAsync(string productCode, CancellationToken ct = default) =>
        GetAsync<ProductOverviewDto>($"/api/v1/platform/admin/products/{Escape(productCode)}/overview", ct);
    public Task<ApiCallResult<PagedResult<OrganizationDto>>> GetOrganizationsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationDto>>($"/api/v1/platform/organizations?{Query(("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<OrganizationDto>> GetOrganizationAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationDto>($"/api/v1/platform/organizations/{id}", ct);
    public Task<ApiCallResult<OrganizationCommercialSummaryDto>> GetOrganizationCommercialSummaryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationCommercialSummaryDto>($"/api/v1/platform/admin/organizations/{id}/commercial-summary", ct);
    public Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetSubscriptionsAsync(string? status = null, string? productCode = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<SubscriptionDto>>($"/api/v1/platform/subscriptions?{Query(("status", status), ("productCode", productCode), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<SubscriptionDto>> GetSubscriptionAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/v1/platform/subscriptions/{id}", ct);
    public Task<ApiCallResult<PagedResult<PaymentDto>>> GetPaymentsAsync(string? status = null, string? productCode = null, Guid? organizationId = null, string? method = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<PaymentDto>>($"/api/v1/platform/payments?{Query(("status", status), ("productCode", productCode), ("organizationId", organizationId), ("method", method), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PaymentDto>> GetPaymentAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PaymentDto>($"/api/v1/platform/payments/{id}", ct);
    public Task<ApiCallResult<PagedResult<EntitlementLatestSummaryDto>>> GetLatestEntitlementsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementLatestSummaryDto>>($"/api/v1/platform/admin/entitlements/latest?{Query(("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PagedResult<EntitlementSnapshotDto>>> GetEntitlementHistoryAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementSnapshotDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots/latest", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetEntitlementAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/entitlements/snapshots/{id}", ct);
    public Task<ApiCallResult<PagedResult<FeatureOverrideDto>>> GetFeatureOverridesAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<FeatureOverrideDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/feature-overrides", ct);

    private async Task<ApiCallResult<T>> GetAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(path, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
                return data is null
                    ? ApiCallResult<T>.Failed(new PlatformApiException(response.StatusCode, "Invalid API response", "The API returned no content."))
                    : ApiCallResult<T>.Success(data);
            }

            var error = await ToExceptionAsync(response, ct).ConfigureAwait(false);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiCallResult<T>.NotFound(error),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallResult<T>.Validation(error),
                _ => ApiCallResult<T>.Failed(error)
            };
        }
        catch (HttpRequestException ex) { return ApiCallResult<T>.Unavailable(new PlatformApiException(null, "Platform API unavailable", ex.Message, innerException: ex)); }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested) { return ApiCallResult<T>.Unavailable(new PlatformApiException(null, "Platform API timed out", ex.Message, innerException: ex)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
    }

    private static async Task<PlatformApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        string? title = null, detail = null;
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            title = document.RootElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
            detail = document.RootElement.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
        }
        catch (JsonException) { }
        response.Headers.TryGetValues("X-Correlation-ID", out var ids);
        return new PlatformApiException(response.StatusCode, title ?? response.ReasonPhrase ?? "Platform API request failed", detail, ids?.FirstOrDefault());
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static string Query(params (string Key, object? Value)[] values) =>
        string.Join("&", values.Where(v => v.Value is not null && !string.IsNullOrWhiteSpace(v.Value.ToString())).Select(v => $"{Escape(v.Key)}={Escape(v.Value!.ToString()!)}"));
}
