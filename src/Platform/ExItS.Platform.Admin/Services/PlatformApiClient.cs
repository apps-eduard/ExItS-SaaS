using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Admin.Models;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiClient(HttpClient httpClient) : IPlatformApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string DevActor = "dev-admin";

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

    public Task<ApiCallResult<PagedResult<PlatformUserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformUserDto>>($"/api/v1/platform/users?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}", ct);
    public Task<ApiCallResult<PlatformUserDto>> GetUserAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PlatformUserDto>($"/api/v1/platform/users/{id}", ct);
    public Task<ApiCallResult<PlatformUserDto>> CreateUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, "/api/v1/platform/users", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> UpdateUserAsync(Guid id, UpdatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Put, $"/api/v1/platform/users/{id}", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> SuspendUserAsync(Guid id, string? reason = null, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/suspend", new LifecycleReasonRequest(reason), ct);
    public Task<ApiCallResult<PlatformUserDto>> ReactivateUserAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/reactivate", null, ct);
    public Task<ApiCallResult<PlatformUserDto>> DisableUserAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/disable", null, ct);

    public Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetOrganizationMembersAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationMembershipDto>>($"/api/v1/platform/organizations/{organizationId}/members?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<PagedResult<OrganizationMembershipDto>>> GetUserMembershipsAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationMembershipDto>>($"/api/v1/platform/users/{userId}/memberships?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> AddOrganizationMemberAsync(Guid organizationId, AddMemberRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/members", request, ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> ChangeMembershipRoleAsync(Guid membershipId, ChangeRoleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Put, $"/api/v1/platform/memberships/{membershipId}/role", request with { ActorReference = request.ActorReference ?? DevActor }, ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> SuspendMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/suspend", WithActor(request), ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> ReactivateMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/reactivate", WithActor(request), ct);
    public Task<ApiCallResult<OrganizationMembershipDto>> RevokeMembershipAsync(Guid membershipId, MembershipLifecycleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationMembershipDto>(HttpMethod.Post, $"/api/v1/platform/memberships/{membershipId}/revoke", WithActor(request), ct);

    public Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetOrganizationProductAccessAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductAccessAssignmentDto>>($"/api/v1/platform/organizations/{organizationId}/product-access?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<PagedResult<ProductAccessAssignmentDto>>> GetUserProductAccessAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductAccessAssignmentDto>>($"/api/v1/platform/users/{userId}/product-access?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<ProductAccessAssignmentDto>> GrantProductAccessAsync(Guid organizationId, GrantProductAccessRequest request, CancellationToken ct = default) =>
        SendAsync<ProductAccessAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/product-access",
            request with { GrantedByActor = string.IsNullOrWhiteSpace(request.GrantedByActor) ? DevActor : request.GrantedByActor }, ct);
    public Task<ApiCallResult<ProductAccessAssignmentDto>> RevokeProductAccessAsync(Guid assignmentId, RevokeProductAccessRequest request, CancellationToken ct = default) =>
        SendAsync<ProductAccessAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/product-access/{assignmentId}/revoke",
            request with { RevokedByActor = string.IsNullOrWhiteSpace(request.RevokedByActor) ? DevActor : request.RevokedByActor }, ct);
    public Task<ApiCallResult<EffectiveProductAccessResultDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<EffectiveProductAccessResultDto>($"/api/v1/platform/access/evaluate?{Query(("userId", userId), ("organizationId", organizationId), ("productCode", productCode))}", ct);

    private static MembershipLifecycleRequest WithActor(MembershipLifecycleRequest request) =>
        request with { ActorReference = string.IsNullOrWhiteSpace(request.ActorReference) ? DevActor : request.ActorReference };

    private async Task<ApiCallResult<T>> GetAsync<T>(string path, CancellationToken ct) =>
        await SendAsync<T>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);

    private async Task<ApiCallResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
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
                HttpStatusCode.Conflict => ApiCallResult<T>.Failed(error),
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
