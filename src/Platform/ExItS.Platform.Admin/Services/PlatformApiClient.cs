using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ExItS.Platform.Admin.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace ExItS.Platform.Admin.Services;

public sealed class PlatformApiClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    PlatformCircuitSession circuitSession) : IPlatformApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string DevActor = "dev-admin";
    private const string SessionTokenHeader = "X-ExItS-Session-Token";

    public Task<ApiCallResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct = default) =>
        GetAsync<PortfolioSummaryDto>("/api/v1/platform/admin/portfolio-summary", ct);
    public Task<ApiCallResult<PagedResult<ProductDto>>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<ProductDto>>(
            $"/api/v1/platform/catalog/products?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);
    public Task<ApiCallResult<ProductDto>> GetProductAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<ProductDto>($"/api/v1/platform/catalog/products/{id}", ct);
    public Task<ApiCallResult<ProductDto>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, "/api/v1/platform/catalog/products", request, ct);
    public Task<ApiCallResult<ProductDto>> RenameProductAsync(Guid id, RenameCatalogRequest request, CancellationToken ct = default) =>
        SendAsync<ProductDto>(HttpMethod.Patch, $"/api/v1/platform/catalog/products/{id}/rename", request, ct);
    public Task<ApiCallResult<ProductDto>> ActivateProductAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{id}/activate", null, ct);
    public Task<ApiCallResult<ProductDto>> DeactivateProductAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{id}/deactivate", null, ct);
    public Task<ApiCallResult<ProductDto>> RetireProductAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<ProductDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{id}/retire", null, ct);
    public Task<ApiCallResult<ProductOverviewDto>> GetProductOverviewAsync(string productCode, CancellationToken ct = default) =>
        GetAsync<ProductOverviewDto>($"/api/v1/platform/admin/products/{Escape(productCode)}/overview", ct);
    public Task<ApiCallResult<PagedResult<PlanDto>>> GetPlansAsync(
        int page = 1,
        int pageSize = 20,
        string? productCode = null,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<PlanDto>>(
            $"/api/v1/platform/catalog/plans?{Query(("page", page), ("pageSize", pageSize), ("productCode", productCode), ("status", status), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);
    public Task<ApiCallResult<IReadOnlyList<PlanDto>>> GetCommercialPlansAsync(string? productCode = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PlanDto>>($"/api/v1/commercial/plans?{Query(("productCode", productCode))}", ct);
    public Task<ApiCallResult<PlanDto>> GetPlanAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PlanDto>($"/api/v1/platform/catalog/plans/{id}", ct);
    public Task<ApiCallResult<PlanDto>> CreatePlanAsync(string productCode, CreatePlanRequest request, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans", request, ct);
    public Task<ApiCallResult<PlanDto>> RenamePlanAsync(string productCode, Guid planId, RenameCatalogRequest request, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Patch, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/rename", request, ct);
    public Task<ApiCallResult<PlanDto>> UpdatePlanCommercialAsync(string productCode, Guid planId, UpdatePlanCommercialRequest request, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Patch, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/commercial", request, ct);
    public Task<ApiCallResult<PlanDto>> ActivatePlanAsync(string productCode, Guid planId, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/activate", null, ct);
    public Task<ApiCallResult<PlanDto>> DeactivatePlanAsync(string productCode, Guid planId, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/deactivate", null, ct);
    public Task<ApiCallResult<PlanDto>> RetirePlanAsync(string productCode, Guid planId, CancellationToken ct = default) =>
        SendAsync<PlanDto>(HttpMethod.Post, $"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/retire", null, ct);
    public Task<ApiCallResult<IReadOnlyList<PlanVersionDto>>> GetPlanVersionsAsync(string productCode, Guid planId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PlanVersionDto>>($"/api/v1/platform/catalog/products/{Escape(productCode)}/plans/{planId}/versions", ct);
    public Task<ApiCallResult<PagedResult<OrganizationDto>>> GetOrganizationsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationDto>>(
            $"/api/v1/platform/organizations?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);
    public Task<ApiCallResult<OrganizationDto>> GetOrganizationAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationDto>($"/api/v1/platform/organizations/{id}", ct);
    public Task<ApiCallResult<OrganizationCatalogVisibilityDto>> GetOrganizationCatalogAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken ct = default) =>
        GetAsync<OrganizationCatalogVisibilityDto>(
            $"/api/v1/platform/organizations/{organizationId}/catalog?{Query(("page", page), ("pageSize", pageSize), ("search", search))}",
            ct);
    public Task<ApiCallResult<OrganizationDto>> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, "/api/v1/platform/organizations", request, ct);
    public Task<ApiCallResult<OrganizationDto>> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{id}", request, ct);
    public Task<ApiCallResult<OrganizationDto>> UpdateOrganizationBrandingAsync(Guid id, UpdateOrganizationBrandingRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{id}/branding", request, ct);
    public Task<ApiCallResult<OrganizationDto>> SuspendOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/suspend", null, ct);
    public Task<ApiCallResult<OrganizationDto>> ReactivateOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/reactivate", null, ct);
    public Task<ApiCallResult<OrganizationDto>> CloseOrganizationAsync(Guid id, CancellationToken ct = default) =>
        SendAsync<OrganizationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{id}/close", null, ct);
    public Task<ApiCallResult<OrganizationCommercialSummaryDto>> GetOrganizationCommercialSummaryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<OrganizationCommercialSummaryDto>($"/api/v1/platform/admin/organizations/{id}/commercial-summary", ct);
    public Task<ApiCallResult<OrganizationCurrentPlanDto>> GetOrganizationCurrentPlanAsync(Guid organizationId, string? productCode = null, CancellationToken ct = default) =>
        GetAsync<OrganizationCurrentPlanDto>($"/api/v1/platform/organizations/{organizationId}/current-plan?{Query(("productCode", productCode))}", ct);
    public Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetSubscriptionsAsync(
        string? status = null,
        string? productCode = null,
        Guid? organizationId = null,
        string? search = null,
        bool? isTrial = null,
        Guid? planId = null,
        string? sortBy = null,
        bool sortDesc = true,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<SubscriptionDto>>(
            $"/api/v1/platform/subscriptions?{Query(
                ("status", status),
                ("productCode", productCode),
                ("organizationId", organizationId),
                ("search", search),
                ("isTrial", isTrial),
                ("planId", planId),
                ("sortBy", sortBy),
                ("sortDesc", sortDesc),
                ("page", page),
                ("pageSize", pageSize))}",
            ct);
    public Task<ApiCallResult<SubscriptionDto>> GetSubscriptionAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/v1/platform/subscriptions/{id}", ct);
    public Task<ApiCallResult<PagedResult<PaymentDto>>> GetPaymentsAsync(string? status = null, string? productCode = null, Guid? organizationId = null, string? method = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<PaymentDto>>($"/api/v1/platform/payments?{Query(("status", status), ("productCode", productCode), ("organizationId", organizationId), ("method", method), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PaymentDto>> GetPaymentAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PaymentDto>($"/api/v1/platform/payments/{id}", ct);
    public Task<ApiCallResult<PagedResult<EntitlementLatestSummaryDto>>> GetLatestEntitlementsAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementLatestSummaryDto>>(
            $"/api/v1/platform/admin/entitlements/latest?{Query(("page", page), ("pageSize", pageSize), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);
    public Task<ApiCallResult<PagedResult<EntitlementSnapshotDto>>> GetEntitlementHistoryAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<EntitlementSnapshotDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots/latest", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GetEntitlementAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<EntitlementSnapshotDto>($"/api/v1/platform/entitlements/snapshots/{id}", ct);
    public Task<ApiCallResult<PagedResult<FeatureOverrideDto>>> GetFeatureOverridesAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<PagedResult<FeatureOverrideDto>>($"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/feature-overrides", ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> GenerateEntitlementSnapshotAsync(Guid organizationId, string productCode, int? expectedNextVersion = null, CancellationToken ct = default) =>
        SendAsync<EntitlementSnapshotDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/snapshots",
            new GenerateEntitlementSnapshotRequest(expectedNextVersion),
            ct);
    public Task<ApiCallResult<EntitlementSnapshotDto>> ReconcileEntitlementSnapshotAsync(Guid organizationId, string productCode, string? reason = null, CancellationToken ct = default) =>
        SendAsync<EntitlementSnapshotDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/entitlements/reconcile",
            new ReconcileEntitlementSnapshotRequest(reason),
            ct);
    public Task<ApiCallResult<FeatureOverrideDto>> CreateFeatureOverrideAsync(Guid organizationId, string productCode, CreateFeatureOverrideRequest request, CancellationToken ct = default) =>
        SendAsync<FeatureOverrideDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId}/products/{Escape(productCode)}/feature-overrides",
            request,
            ct);
    public Task<ApiCallResult<FeatureOverrideDto>> RevokeFeatureOverrideAsync(Guid overrideId, RevokeFeatureOverrideRequest request, CancellationToken ct = default) =>
        SendAsync<FeatureOverrideDto>(HttpMethod.Post, $"/api/v1/platform/feature-overrides/{overrideId}/revoke", request, ct);

    public Task<ApiCallResult<PagedResult<PlatformUserDto>>> GetUsersAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null, string? directory = null, string? sortBy = null, bool? sortDesc = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformUserDto>>($"/api/v1/platform/users?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search), ("directory", directory), ("sortBy", sortBy), ("sortDesc", sortDesc))}", ct);
    public Task<ApiCallResult<PlatformUserDto>> GetUserAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PlatformUserDto>($"/api/v1/platform/users/{id}", ct);
    public Task<ApiCallResult<PlatformUserDto>> CreateUserAsync(CreatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, "/api/v1/platform/users", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> UpdateUserAsync(Guid id, UpdatePlatformUserRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Put, $"/api/v1/platform/users/{id}", request, ct);
    public Task<ApiCallResult<PlatformUserDto>> SuspendUserAsync(Guid id, string? reason = null, bool global = false, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/suspend", new LifecycleReasonRequest(reason, global), ct);
    public Task<ApiCallResult<PlatformUserDto>> ReactivateUserAsync(Guid id, ReactivatePlatformUserRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(HttpMethod.Post, $"/api/v1/platform/users/{id}/reactivate", request ?? new ReactivatePlatformUserRequest(), ct);
    public Task<ApiCallResult<PlatformUserDto>> DeactivateUserAsync(
        Guid id,
        string reason,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(
            HttpMethod.Post,
            $"/api/v1/platform/users/{id}/deactivate",
            new LifecycleReasonRequest(reason, ActorPassword: actorPassword, MfaCode: mfaCode),
            ct);
    public Task<ApiCallResult<PlatformUserDto>> MoveUserToSuspendedAsync(
        Guid id,
        string reason,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken ct = default) =>
        SendAsync<PlatformUserDto>(
            HttpMethod.Post,
            $"/api/v1/platform/users/{id}/move-to-suspended",
            new LifecycleReasonRequest(reason, ActorPassword: actorPassword, MfaCode: mfaCode),
            ct);
    public Task<ApiCallResult<PlatformUserDto>> DisableUserAsync(Guid id, string reason, CancellationToken ct = default) =>
        DeactivateUserAsync(id, reason, ct: ct);

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

    public Task<ApiCallResult<PagedResult<OrganizationInvitationDto>>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationInvitationDto>>($"/api/v1/platform/organizations/{organizationId}/invitations?{Query(("page", page), ("pageSize", pageSize), ("status", status))}", ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(Guid organizationId, CreateInvitationRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/invitations", request, ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> ResendOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/invitations/{invitationId}/resend", null, ct);
    public Task<ApiCallResult<OrganizationInvitationDto>> RevokeOrganizationInvitationAsync(Guid invitationId, CancellationToken ct = default) =>
        SendAsync<OrganizationInvitationDto>(HttpMethod.Post, $"/api/v1/platform/invitations/{invitationId}/revoke", null, ct);
    public Task<ApiCallResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsync(
        string token,
        string password,
        string? displayName = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken ct = default) =>
        SendAsync<AcceptOrganizationInvitationResultDto>(
            HttpMethod.Post,
            "/api/v1/platform/invitations/accept",
            new { token, password, displayName, firstName, lastName },
            ct);

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
    public Task<ApiCallResult<IReadOnlyList<EnabledProductDto>>> GetEnabledProductsAsync(Guid organizationId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EnabledProductDto>>($"/api/v1/organizations/{organizationId}/enabled-products", ct);
    public Task<ApiCallResult<ProductAuthorizationResultDto>> EvaluateProductAuthorizationAsync(Guid organizationId, string productCode, Guid? userId = null, CancellationToken ct = default) =>
        GetAsync<ProductAuthorizationResultDto>($"/api/v1/organizations/{organizationId}/product-authorization?{Query(("productCode", productCode), ("userId", userId))}", ct);
    public Task<ApiCallResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(Guid organizationId, string? status = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProductLocalRoleGrantDto>>($"/api/v1/organizations/{organizationId}/product-local-roles?{Query(("status", status))}", ct);
    public Task<ApiCallResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(Guid organizationId, AssignProductLocalRoleRequest request, CancellationToken ct = default) =>
        SendAsync<ProductLocalRoleGrantDto>(HttpMethod.Post, $"/api/v1/organizations/{organizationId}/product-local-roles", request, ct);
    public Task<ApiCallResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(Guid organizationId, Guid grantId, RevokeProductLocalRoleRequest request, CancellationToken ct = default) =>
        SendAsync<ProductLocalRoleGrantDto>(HttpMethod.Post, $"/api/v1/organizations/{organizationId}/product-local-roles/{grantId}/revoke", request, ct);
    public Task<ApiCallResult<ProductLaunchResultDto>> LaunchProductAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        SendAsync<ProductLaunchResultDto>(HttpMethod.Post, $"/api/v1/organizations/{organizationId}/products/{Escape(productCode)}/launch", null, ct);

    public Task<ApiCallResult<PagedResult<SubscriptionDto>>> GetOrganizationSubscriptionsAsync(Guid organizationId, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<SubscriptionDto>>($"/api/v1/platform/organizations/{organizationId}/subscriptions?{Query(("status", status), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<SubscriptionDto>> GetCurrentSubscriptionAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
        GetAsync<SubscriptionDto>($"/api/v1/platform/organizations/{organizationId}/subscriptions/current?productCode={Escape(productCode)}", ct);
    public Task<ApiCallResult<SubscriptionDto>> StartTrialAsync(Guid organizationId, StartTrialRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/trials", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> CreatePaidSubscriptionAsync(Guid organizationId, CreatePaidSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> ActivateSubscriptionAsync(Guid subscriptionId, ActivateSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/activate", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> EnterGracePeriodAsync(Guid subscriptionId, GracePeriodRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/grace-period", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> MarkPastDueAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/past-due", new SubscriptionLifecycleRequest(expectedVersion), ct);
    public Task<ApiCallResult<SubscriptionDto>> SuspendSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/suspend", new SubscriptionLifecycleRequest(expectedVersion), ct);
    public Task<ApiCallResult<SubscriptionDto>> ReactivateSubscriptionAsync(Guid subscriptionId, ReactivateSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/reactivate", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> CancelSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/cancel", new SubscriptionLifecycleRequest(expectedVersion), ct);
    public Task<ApiCallResult<SubscriptionDto>> ExpireSubscriptionAsync(Guid subscriptionId, int? expectedVersion = null, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/subscriptions/{subscriptionId}/expire", new SubscriptionLifecycleRequest(expectedVersion), ct);
    public Task<ApiCallResult<SubscriptionDto>> UpgradeSubscriptionAsync(Guid organizationId, Guid subscriptionId, UpgradeSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/upgrade", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> DowngradeSubscriptionAsync(Guid organizationId, Guid subscriptionId, DowngradeSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/downgrade", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> ConvertTrialSubscriptionAsync(Guid organizationId, Guid subscriptionId, ConvertTrialSubscriptionRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/convert-trial", request, ct);
    public Task<ApiCallResult<SubscriptionDto>> StartOrganizationCommercialSubscriptionAsync(Guid organizationId, StartOrganizationCommercialRequest request, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/from-catalog", request, ct);
    public Task<ApiCallResult<PlanChangeImpactPreviewDto>> PreviewPlanChangeAsync(
        Guid organizationId,
        Guid subscriptionId,
        Guid? planId = null,
        string? planKey = null,
        int? activeBranchCount = null,
        CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (planId is Guid id)
        {
            qs.Add($"planId={id:D}");
        }

        if (!string.IsNullOrWhiteSpace(planKey))
        {
            qs.Add($"planKey={Uri.EscapeDataString(planKey)}");
        }

        if (activeBranchCount is int branches)
        {
            qs.Add($"activeBranchCount={branches}");
        }

        var suffix = qs.Count == 0 ? string.Empty : "?" + string.Join("&", qs);
        return GetAsync<PlanChangeImpactPreviewDto>(
            $"/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/plan-change-preview{suffix}",
            ct);
    }
    public Task<ApiCallResult<SubscriptionDto>> ApplyPendingPlanChangeAsync(Guid organizationId, Guid subscriptionId, CancellationToken ct = default) =>
        SendAsync<SubscriptionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/apply-pending-plan", null, ct);
    public Task<ApiCallResult<SimulateLocalValidationPaymentResultDto>> SimulateLocalValidationPaymentAsync(SimulateLocalValidationPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<SimulateLocalValidationPaymentResultDto>(HttpMethod.Post, "/api/v1/platform/local-validation/payments/simulate", request, ct);
    public Task<ApiCallResult<StartBusinessResultDto>> StartBusinessAsync(StartBusinessRequest request, CancellationToken ct = default) =>
        SendAsync<StartBusinessResultDto>(HttpMethod.Post, "/api/v1/personal/start-business", request, ct);
    public Task<ApiCallResult<bool>> GetLocalValidationEnabledAsync(CancellationToken ct = default) =>
        GetAsync<bool>("/api/v1/platform/local-validation/enabled", ct);

    public Task<ApiCallResult<PaymentDto>> CreateManualPaymentAsync(CreateManualPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, "/api/v1/platform/payments/manual", request, ct);
    public Task<ApiCallResult<PaymentDto>> ConfirmPaymentAsync(Guid paymentId, ConfirmPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/confirm",
            request with { ConfirmedBy = string.IsNullOrWhiteSpace(request.ConfirmedBy) ? DevActor : request.ConfirmedBy }, ct);
    public Task<ApiCallResult<PaymentDto>> RejectPaymentAsync(Guid paymentId, RejectPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/reject",
            request with { RejectedBy = string.IsNullOrWhiteSpace(request.RejectedBy) ? DevActor : request.RejectedBy }, ct);
    public Task<ApiCallResult<PaymentDto>> VoidPaymentAsync(Guid paymentId, VoidPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/void",
            request with { VoidedBy = string.IsNullOrWhiteSpace(request.VoidedBy) ? DevActor : request.VoidedBy }, ct);
    public Task<ApiCallResult<PaymentActivationResultDto>> ConfirmPaymentAndActivateAsync(Guid paymentId, ActivateSubscriptionForPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<PaymentActivationResultDto>(HttpMethod.Post, $"/api/v1/platform/payments/{paymentId}/activate-subscription",
            request with { ConfirmedBy = string.IsNullOrWhiteSpace(request.ConfirmedBy) ? DevActor : request.ConfirmedBy }, ct);

    public Task<ApiCallResult<PagedResult<AuditRecordDto>>> GetAuditRecordsAsync(AuditQuery query, CancellationToken ct = default) =>
        GetAsync<PagedResult<AuditRecordDto>>($"/api/v1/platform/audit?{Query(
            ("fromUtc", query.OccurredFromUtc?.ToString("o")),
            ("toUtc", query.OccurredToUtc?.ToString("o")),
            ("actor", query.ActorIdentifier),
            ("action", query.ActionCode),
            ("organizationId", query.OrganizationId),
            ("productCode", query.ProductCode),
            ("outcome", query.Outcome),
            ("correlationId", query.CorrelationId),
            ("page", query.Page),
            ("pageSize", query.PageSize))}", ct);
    public Task<ApiCallResult<AuditRecordDto>> GetAuditRecordAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<AuditRecordDto>($"/api/v1/platform/audit/{id}", ct);
    public Task<ApiCallResult<ResolvedPermissionsDto>> GetMyAuthorizationAsync(Guid? organizationId = null, CancellationToken ct = default) =>
        GetAsync<ResolvedPermissionsDto>($"/api/v1/platform/authorization/me?{Query(("organizationId", organizationId))}", ct);
    public Task<ApiCallResult<IReadOnlyList<PlatformRoleCatalogEntryDto>>> GetAuthorizationRolesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PlatformRoleCatalogEntryDto>>("/api/v1/platform/authorization/roles", ct);
    public Task<ApiCallResult<IReadOnlyList<PermissionCatalogEntryDto>>> GetPlatformPermissionsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PermissionCatalogEntryDto>>("/api/v1/platform/authorization/permissions", ct);
    public Task<ApiCallResult<IReadOnlyList<PermissionCatalogEntryDto>>> GetOrganizationPermissionsCatalogAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PermissionCatalogEntryDto>>("/api/v1/platform/authorization/organization-permissions", ct);
    public Task<ApiCallResult<PagedResult<PlatformRoleDefinitionDto>>> GetPlatformRoleDefinitionsAsync(int page = 1, int pageSize = 20, string? kind = null, string? status = null, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformRoleDefinitionDto>>($"/api/v1/platform/authorization/role-definitions?{Query(("page", page), ("pageSize", pageSize), ("kind", kind), ("status", status), ("search", search))}", ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> GetPlatformRoleDefinitionAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PlatformRoleDefinitionDto>($"/api/v1/platform/authorization/role-definitions/{id}", ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> CreatePlatformRoleDefinitionAsync(CreateRoleDefinitionRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformRoleDefinitionDto>(HttpMethod.Post, "/api/v1/platform/authorization/role-definitions", request, ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> UpdatePlatformRoleDefinitionAsync(Guid id, UpdateRoleDefinitionRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformRoleDefinitionDto>(HttpMethod.Put, $"/api/v1/platform/authorization/role-definitions/{id}", request, ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> ActivatePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/authorization/role-definitions/{id}/activate", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> DeactivatePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/authorization/role-definitions/{id}/deactivate", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<PlatformRoleDefinitionDto>> RetirePlatformRoleDefinitionAsync(Guid id, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/authorization/role-definitions/{id}/retire", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<PagedResult<PlatformRoleAssignmentDto>>> GetPlatformRoleAssignmentsAsync(Guid? platformUserId = null, string? role = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformRoleAssignmentDto>>($"/api/v1/platform/authorization/assignments?{Query(("platformUserId", platformUserId), ("role", role), ("status", status), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PlatformRoleAssignmentDto>> AssignPlatformSystemRoleAsync(AssignSystemRoleRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformRoleAssignmentDto>(HttpMethod.Post, "/api/v1/platform/authorization/assignments", request, ct);
    public Task<ApiCallResult<PlatformRoleAssignmentDto>> RevokePlatformSystemRoleAsync(Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformRoleAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/authorization/assignments/{assignmentId}/revoke", request ?? new RevokeRoleAssignmentRequest(), ct);
    public Task<ApiCallResult<PagedResult<PlatformCustomRoleAssignmentDto>>> GetPlatformCustomRoleAssignmentsAsync(Guid? platformUserId = null, Guid? roleDefinitionId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<PlatformCustomRoleAssignmentDto>>($"/api/v1/platform/authorization/custom-assignments?{Query(("platformUserId", platformUserId), ("roleDefinitionId", roleDefinitionId), ("status", status), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<PlatformCustomRoleAssignmentDto>> AssignPlatformCustomRoleAsync(AssignCustomRoleRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCustomRoleAssignmentDto>(HttpMethod.Post, "/api/v1/platform/authorization/custom-assignments", request, ct);
    public Task<ApiCallResult<PlatformCustomRoleAssignmentDto>> RevokePlatformCustomRoleAsync(Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default) =>
        SendAsync<PlatformCustomRoleAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/authorization/custom-assignments/{assignmentId}/revoke", request ?? new RevokeRoleAssignmentRequest(), ct);
    public Task<ApiCallResult<EffectivePlatformPermissionsDto>> GetEffectivePlatformPermissionsAsync(Guid userId, Guid? organizationId = null, CancellationToken ct = default) =>
        GetAsync<EffectivePlatformPermissionsDto>($"/api/v1/platform/authorization/users/{userId}/effective-permissions?{Query(("organizationId", organizationId))}", ct);
    public Task<ApiCallResult<PagedResult<OrganizationRoleDefinitionDto>>> GetOrganizationRoleDefinitionsAsync(Guid organizationId, int page = 1, int pageSize = 20, string? status = null, string? search = null, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationRoleDefinitionDto>>($"/api/v1/platform/organizations/{organizationId}/role-definitions?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}", ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> GetOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, CancellationToken ct = default) =>
        GetAsync<OrganizationRoleDefinitionDto>($"/api/v1/platform/organizations/{organizationId}/role-definitions/{roleId}", ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> CreateOrganizationRoleDefinitionAsync(Guid organizationId, CreateRoleDefinitionRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-definitions", request, ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> UpdateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, UpdateRoleDefinitionRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationRoleDefinitionDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{organizationId}/role-definitions/{roleId}", request, ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> ActivateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<OrganizationRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-definitions/{roleId}/activate", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> DeactivateOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<OrganizationRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-definitions/{roleId}/deactivate", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<OrganizationRoleDefinitionDto>> RetireOrganizationRoleDefinitionAsync(Guid organizationId, Guid roleId, RoleLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<OrganizationRoleDefinitionDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-definitions/{roleId}/retire", request ?? new RoleLifecycleRequest(), ct);
    public Task<ApiCallResult<PagedResult<OrganizationCustomRoleAssignmentDto>>> GetOrganizationRoleAssignmentsAsync(Guid organizationId, Guid? platformUserId = null, Guid? roleDefinitionId = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        GetAsync<PagedResult<OrganizationCustomRoleAssignmentDto>>($"/api/v1/platform/organizations/{organizationId}/role-assignments?{Query(("platformUserId", platformUserId), ("roleDefinitionId", roleDefinitionId), ("status", status), ("page", page), ("pageSize", pageSize))}", ct);
    public Task<ApiCallResult<OrganizationCustomRoleAssignmentDto>> AssignOrganizationCustomRoleAsync(Guid organizationId, AssignOrgCustomRoleRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationCustomRoleAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-assignments", request, ct);
    public Task<ApiCallResult<OrganizationCustomRoleAssignmentDto>> RevokeOrganizationCustomRoleAsync(Guid organizationId, Guid assignmentId, RevokeRoleAssignmentRequest? request = null, CancellationToken ct = default) =>
        SendAsync<OrganizationCustomRoleAssignmentDto>(HttpMethod.Post, $"/api/v1/platform/organizations/{organizationId}/role-assignments/{assignmentId}/revoke", request ?? new RevokeRoleAssignmentRequest(), ct);
    public Task<ApiCallResult<EffectiveOrganizationPermissionsDto>> GetEffectiveOrganizationPermissionsAsync(Guid organizationId, Guid userId, CancellationToken ct = default) =>
        GetAsync<EffectiveOrganizationPermissionsDto>($"/api/v1/platform/organizations/{organizationId}/members/{userId}/effective-permissions", ct);

    public Task<ApiCallResult<PlatformCredentialStatusDto>> GetUserCredentialsAsync(Guid userId, CancellationToken ct = default) =>
        GetAsync<PlatformCredentialStatusDto>($"/api/v1/platform/users/{userId}/credentials", ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> SetUserPasswordAsync(Guid userId, SetUserPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Put, $"/api/v1/platform/users/{userId}/credentials/password", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> UnlockUserCredentialAsync(Guid userId, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, $"/api/v1/platform/users/{userId}/credentials/unlock", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> MarkUserEmailVerifiedAsync(Guid userId, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, $"/api/v1/platform/users/{userId}/credentials/email-verified", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/change-password", request, ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/forgot-password", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/reset-password", request, ct);
    public Task<ApiCallResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(RegisterPersonalAccountRequest request, CancellationToken ct = default) =>
        SendAsync<PersonalRegistrationAckDto>(HttpMethod.Post, "/api/v1/platform/auth/register", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ActivatePersonalAccountAsync(ActivatePersonalAccountRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/activate-account", request, ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> RequestEmailVerificationAsync(CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/email-verification/request", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmEmailVerificationAsync(ConfirmEmailVerificationRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/email-verification/confirm", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> GetMyCredentialsAsync(CancellationToken ct = default) =>
        GetAsync<PlatformCredentialStatusDto>("/api/v1/platform/auth/credentials", ct);
    public Task<ApiCallResult<CredentialWorkflowAckDto>> RequestRecoveryEmailAsync(RequestRecoveryEmailRequest request, CancellationToken ct = default) =>
        SendAsync<CredentialWorkflowAckDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/request", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ConfirmRecoveryEmailAsync(ConfirmRecoveryEmailRequest request, CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/confirm", request, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> SkipRecoveryEmailAsync(CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/skip", null, ct);
    public Task<ApiCallResult<PlatformCredentialStatusDto>> ClearRecoveryEmailAsync(CancellationToken ct = default) =>
        SendAsync<PlatformCredentialStatusDto>(HttpMethod.Post, "/api/v1/platform/auth/recovery-email/clear", null, ct);

    public Task<ApiCallResult<AuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default) =>
        GetAsync<AuthSessionInfoDto>("/api/v1/platform/auth/me", ct);

    public Task<ApiCallResult<IReadOnlyList<EligibleOrganizationDto>>> GetEligibleOrganizationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EligibleOrganizationDto>>("/api/v1/platform/auth/organizations", ct);

    public Task<ApiCallResult<OrganizationContextResultDto>> SetOrganizationContextAsync(SetOrganizationContextRequest request, CancellationToken ct = default) =>
        SendAsync<OrganizationContextResultDto>(HttpMethod.Put, "/api/v1/platform/auth/organization-context", request, ct);

    public Task<ApiCallResult<PersonalDashboardDto>> GetPersonalDashboardAsync(CancellationToken ct = default) =>
        GetAsync<PersonalDashboardDto>("/api/v1/personal/dashboard", ct);

    public Task<ApiCallResult<PersonalProfileDto>> GetPersonalProfileAsync(CancellationToken ct = default) =>
        GetAsync<PersonalProfileDto>("/api/v1/personal/profile", ct);

    public Task<ApiCallResult<PersonalAccountSettingsDto>> GetPersonalSettingsAsync(CancellationToken ct = default) =>
        GetAsync<PersonalAccountSettingsDto>("/api/v1/personal/settings", ct);

    public Task<ApiCallResult<IReadOnlyList<PersonalContactDto>>> GetPersonalUtangContactsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalContactDto>>("/api/v1/personal/utang/contacts", ct);

    public Task<ApiCallResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangLentAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/lent", ct);

    public Task<ApiCallResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangBorrowedAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>("/api/v1/personal/utang/relationships/borrowed", ct);

    public Task<ApiCallResult<IReadOnlyList<PersonalUtangInvitationDto>>> GetPersonalUtangInvitationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalUtangInvitationDto>>("/api/v1/personal/utang/invitations", ct);

    public Task<ApiCallResult<IReadOnlyList<PersonalInAppNotificationDto>>> GetPersonalNotificationsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PersonalInAppNotificationDto>>("/api/v1/personal/notifications", ct);

    public Task<ApiCallResult<AccessTokenIssueDto>> IssueAccessTokenAsync(IssueAccessTokenRequest request, CancellationToken ct = default) =>
        SendAsync<AccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token", request, ct);

    public Task<ApiCallResult<AccessTokenIntrospectionDto>> IntrospectAccessTokenAsync(IntrospectAccessTokenRequest request, CancellationToken ct = default) =>
        SendAsync<AccessTokenIntrospectionDto>(HttpMethod.Post, "/api/v1/platform/auth/introspect", request, ct);

    public Task<ApiCallResult<PagedResult<BusinessTypeDto>>> GetBusinessTypesAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<BusinessTypeDto>>(
            $"/api/v1/platform/global-catalog/business-types?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);

    public Task<ApiCallResult<BusinessTypeDto>> GetBusinessTypeAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<BusinessTypeDto>($"/api/v1/platform/global-catalog/business-types/{id}", ct);

    public Task<ApiCallResult<BusinessTypeDto>> CreateBusinessTypeAsync(CreateBusinessTypeRequest request, CancellationToken ct = default) =>
        SendAsync<BusinessTypeDto>(HttpMethod.Post, "/api/v1/platform/global-catalog/business-types", request, ct);

    public Task<ApiCallResult<BusinessTypeDto>> UpdateBusinessTypeAsync(Guid id, UpdateBusinessTypeRequest request, CancellationToken ct = default) =>
        SendAsync<BusinessTypeDto>(HttpMethod.Put, $"/api/v1/platform/global-catalog/business-types/{id}", request, ct);

    public Task<ApiCallResult<BusinessTypeDto>> SetBusinessTypeStatusAsync(Guid id, SetBusinessTypeStatusRequest request, CancellationToken ct = default) =>
        SendAsync<BusinessTypeDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/business-types/{id}/status", request, ct);

    public Task<ApiCallResult<PagedResult<GlobalCategoryDto>>> GetGlobalCategoriesAsync(
        int page = 1,
        int pageSize = 50,
        string? status = null,
        Guid? parentId = null,
        string? businessTypeCode = null,
        Guid? businessTypeId = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<GlobalCategoryDto>>(
            $"/api/v1/platform/global-catalog/categories?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("parentId", parentId), ("businessTypeCode", businessTypeCode), ("businessTypeId", businessTypeId), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);

    public Task<ApiCallResult<GlobalCategoryDto>> GetGlobalCategoryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<GlobalCategoryDto>($"/api/v1/platform/global-catalog/categories/{id}", ct);

    public Task<ApiCallResult<GlobalCategoryDto>> CreateGlobalCategoryAsync(CreateGlobalCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalCategoryDto>(HttpMethod.Post, "/api/v1/platform/global-catalog/categories", request, ct);

    public Task<ApiCallResult<GlobalCategoryDto>> UpdateGlobalCategoryAsync(Guid id, UpdateGlobalCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalCategoryDto>(HttpMethod.Put, $"/api/v1/platform/global-catalog/categories/{id}", request, ct);

    public Task<ApiCallResult<GlobalCategoryDto>> SetGlobalCategoryStatusAsync(Guid id, SetGlobalCategoryStatusRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalCategoryDto>(HttpMethod.Patch, $"/api/v1/platform/global-catalog/categories/{id}/status", request, ct);

    public Task<ApiCallResult<GlobalCategoryDto>> BulkAssignCategoryBusinessTypesAsync(
        Guid id,
        BulkAssignCategoryBusinessTypesRequest request,
        CancellationToken ct = default) =>
        SendAsync<GlobalCategoryDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/categories/{id}/business-types", request, ct);

    public Task<ApiCallResult<PagedResult<GlobalProductDto>>> GetGlobalProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? categoryId = null,
        string? businessTypeCode = null,
        Guid? businessTypeId = null,
        string? search = null,
        string? barcode = null,
        string? sku = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<GlobalProductDto>>(
            $"/api/v1/platform/global-catalog/products?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("categoryId", categoryId), ("businessTypeCode", businessTypeCode), ("businessTypeId", businessTypeId), ("search", search), ("barcode", barcode), ("sku", sku), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);

    public Task<ApiCallResult<GlobalProductDto>> GetGlobalProductAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<GlobalProductDto>($"/api/v1/platform/global-catalog/products/{id}", ct);

    public Task<ApiCallResult<GlobalProductDto>> CreateGlobalProductAsync(CreateGlobalProductRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalProductDto>(HttpMethod.Post, "/api/v1/platform/global-catalog/products", request, ct);

    public Task<ApiCallResult<GlobalProductDto>> UpdateGlobalProductAsync(Guid id, UpdateGlobalProductRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalProductDto>(HttpMethod.Put, $"/api/v1/platform/global-catalog/products/{id}", request, ct);

    public Task<ApiCallResult<GlobalProductDto>> SetGlobalProductStatusAsync(Guid id, SetGlobalProductStatusRequest request, CancellationToken ct = default) =>
        SendAsync<GlobalProductDto>(HttpMethod.Patch, $"/api/v1/platform/global-catalog/products/{id}/status", request, ct);

    public Task<ApiCallResult<PagedResult<CatalogTemplateSummaryDto>>> GetCatalogTemplatesAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? primaryBusinessTypeCode = null,
        Guid? primaryBusinessTypeId = null,
        string? search = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<CatalogTemplateSummaryDto>>(
            $"/api/v1/platform/global-catalog/templates?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("primaryBusinessTypeCode", primaryBusinessTypeCode), ("primaryBusinessTypeId", primaryBusinessTypeId), ("search", search), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);

    public Task<ApiCallResult<CatalogTemplateDto>> GetCatalogTemplateAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<CatalogTemplateDto>($"/api/v1/platform/global-catalog/templates/{id}", ct);

    public Task<ApiCallResult<CatalogTemplateDto>> CreateCatalogTemplateAsync(CreateCatalogTemplateRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, "/api/v1/platform/global-catalog/templates", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> UpdateCatalogTemplateAsync(Guid id, UpdateCatalogTemplateRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Put, $"/api/v1/platform/global-catalog/templates/{id}", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> PublishCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/publish", request ?? new CatalogTemplateLifecycleRequest(), ct);

    public Task<ApiCallResult<CatalogTemplateDto>> UnpublishCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/unpublish", request ?? new CatalogTemplateLifecycleRequest(), ct);

    public Task<ApiCallResult<CatalogTemplateDto>> ArchiveCatalogTemplateAsync(Guid id, CatalogTemplateLifecycleRequest? request = null, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/archive", request ?? new CatalogTemplateLifecycleRequest(), ct);

    public Task<ApiCallResult<CatalogTemplateDto>> AssignCatalogTemplateProductAsync(Guid id, AssignCatalogTemplateProductRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/products", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> BulkAssignCatalogTemplateProductsAsync(Guid id, BulkAssignCatalogTemplateProductsRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/products/bulk", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> BulkRemoveCatalogTemplateProductsAsync(Guid id, BulkRemoveCatalogTemplateProductsRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Post, $"/api/v1/platform/global-catalog/templates/{id}/products/bulk-remove", request, ct);

    public Task<ApiCallResult<PagedResult<GlobalProductDto>>> GetCatalogTemplateAvailableProductsAsync(
        Guid id,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        Guid? categoryId = null,
        string? search = null,
        string? barcode = null,
        string? sku = null,
        string? sortBy = null,
        bool? sortDesc = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<GlobalProductDto>>(
            $"/api/v1/platform/global-catalog/templates/{id}/available-products?{Query(("page", page), ("pageSize", pageSize), ("status", status), ("categoryId", categoryId), ("search", search), ("barcode", barcode), ("sku", sku), ("sortBy", sortBy), ("sortDesc", sortDesc))}",
            ct);

    public Task<ApiCallResult<CatalogTemplateDto>> ReorderCatalogTemplateProductsAsync(Guid id, ReorderCatalogTemplateProductsRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Put, $"/api/v1/platform/global-catalog/templates/{id}/products/order", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> UpdateCatalogTemplateProductFlagsAsync(Guid id, Guid productId, UpdateCatalogTemplateProductFlagsRequest request, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(HttpMethod.Patch, $"/api/v1/platform/global-catalog/templates/{id}/products/{productId}", request, ct);

    public Task<ApiCallResult<CatalogTemplateDto>> RemoveCatalogTemplateProductAsync(Guid id, Guid productId, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken ct = default) =>
        SendAsync<CatalogTemplateDto>(
            HttpMethod.Delete,
            $"/api/v1/platform/global-catalog/templates/{id}/products/{productId}?{Query(("expectedUpdatedAtUtc", expectedUpdatedAtUtc))}",
            null,
            ct);

    public Task<ApiCallResult<PagedResult<CatalogImportJobDto>>> GetCatalogImportsAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<CatalogImportJobDto>>(
            $"/api/v1/platform/global-catalog/products/imports?{Query(("page", page), ("pageSize", pageSize), ("status", status))}",
            ct);

    public Task<ApiCallResult<CatalogImportJobDto>> GetCatalogImportAsync(Guid jobId, CancellationToken ct = default) =>
        GetAsync<CatalogImportJobDto>($"/api/v1/platform/global-catalog/products/imports/{jobId}", ct);

    public async Task<ApiCallResult<CatalogImportJobDto>> UploadCatalogImportAsync(
        Stream content,
        string fileName,
        string? contentType,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/global-catalog/products/imports");
            var form = new MultipartFormDataContent();
            var streamContent = new StreamContent(content);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            form.Add(streamContent, "file", fileName);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                form.Add(new StringContent(idempotencyKey), "idempotencyKey");
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }

            request.Content = form;

            var sessionToken = await ResolveSessionTokenAsync();
            if (!string.IsNullOrWhiteSpace(sessionToken)
                && !request.Headers.Contains(SessionTokenHeader))
            {
                request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
            }

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CatalogImportJobDto>(JsonOptions, ct);
                return data is null
                    ? ApiCallResult<CatalogImportJobDto>.Failed(new PlatformApiException(response.StatusCode, "Invalid API response", "The API returned no content."))
                    : ApiCallResult<CatalogImportJobDto>.Success(data);
            }

            var error = await ToExceptionAsync(response, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiCallResult<CatalogImportJobDto>.NotFound(error),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallResult<CatalogImportJobDto>.Validation(error),
                HttpStatusCode.Conflict => ApiCallResult<CatalogImportJobDto>.Failed(error),
                _ => ApiCallResult<CatalogImportJobDto>.Failed(error)
            };
        }
        catch (HttpRequestException ex)
        {
            return ApiCallResult<CatalogImportJobDto>.Unavailable(new PlatformApiException(null, "Platform API unavailable", ex.Message, innerException: ex));
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return ApiCallResult<CatalogImportJobDto>.Unavailable(new PlatformApiException(null, "Platform API timed out", ex.Message, innerException: ex));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
    }

    public Task<ApiCallResult<CatalogImportJobDto>> ConfirmCatalogImportAsync(
        Guid jobId,
        Guid? targetTemplateId = null,
        CancellationToken ct = default)
    {
        object body = targetTemplateId is null
            ? new { }
            : new { targetTemplateId };
        return SendAsync<CatalogImportJobDto>(
            HttpMethod.Post,
            $"/api/v1/platform/global-catalog/products/imports/{jobId}/confirm",
            body,
            ct);
    }

    public Task<ApiCallResult<PagedResult<CatalogImportErrorDto>>> GetCatalogImportErrorsAsync(
        Guid jobId,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default) =>
        GetAsync<PagedResult<CatalogImportErrorDto>>(
            $"/api/v1/platform/global-catalog/products/imports/{jobId}/errors?{Query(("page", page), ("pageSize", pageSize))}",
            ct);

    public Task<ApiCallResult<PrivacyComplianceOverviewDto>> GetPrivacyComplianceOverviewAsync(CancellationToken ct = default) =>
        GetAsync<PrivacyComplianceOverviewDto>("/api/v1/platform/privacy-compliance/overview", ct);

    public Task<ApiCallResult<IReadOnlyList<ComplianceRequirementDto>>> ListPrivacyComplianceRequirementsAsync(string? category = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ComplianceRequirementDto>>(
            $"/api/v1/platform/privacy-compliance/requirements?{Query(("category", category))}",
            ct);

    public Task<ApiCallResult<ComplianceRequirementDto>> GetPrivacyComplianceRequirementAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<ComplianceRequirementDto>($"/api/v1/platform/privacy-compliance/requirements/{id}", ct);

    public Task<ApiCallResult<ComplianceRequirementDto>> UpdatePrivacyComplianceRequirementStatusAsync(
        Guid id,
        UpdateComplianceRequirementStatusRequest request,
        CancellationToken ct = default) =>
        SendAsync<ComplianceRequirementDto>(
            HttpMethod.Patch,
            $"/api/v1/platform/privacy-compliance/requirements/{id}/status",
            request,
            ct);

    public Task<ApiCallResult<ComplianceRequirementDto>> UpdatePrivacyComplianceRequirementDetailsAsync(
        Guid id,
        UpdateComplianceRequirementDetailsRequest request,
        CancellationToken ct = default) =>
        SendAsync<ComplianceRequirementDto>(
            HttpMethod.Patch,
            $"/api/v1/platform/privacy-compliance/requirements/{id}",
            request,
            ct);

    public Task<ApiCallResult<IReadOnlyList<ComplianceEvidenceDto>>> ListPrivacyComplianceEvidenceAsync(Guid requirementId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ComplianceEvidenceDto>>(
            $"/api/v1/platform/privacy-compliance/requirements/{requirementId}/evidence",
            ct);

    public Task<ApiCallResult<ComplianceEvidenceDto>> AddPrivacyComplianceEvidenceAsync(AddComplianceEvidenceRequest request, CancellationToken ct = default) =>
        SendAsync<ComplianceEvidenceDto>(HttpMethod.Post, "/api/v1/platform/privacy-compliance/evidence", request, ct);

    public Task<ApiCallResult<IReadOnlyList<ProcessingSystemDto>>> ListPrivacyComplianceSystemsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProcessingSystemDto>>("/api/v1/platform/privacy-compliance/systems", ct);

    public Task<ApiCallResult<EnsurePrivacyComplianceCatalogResultDto>> EnsurePrivacyComplianceCatalogAsync(CancellationToken ct = default) =>
        SendAsync<EnsurePrivacyComplianceCatalogResultDto>(
            HttpMethod.Post,
            "/api/v1/platform/privacy-compliance/ensure-catalog",
            new { },
            ct);

    public async Task<ApiCallResult<byte[]>> ExportPrivacyComplianceRequirementPdfAsync(
        Guid requirementId,
        string? companyName = null,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/platform/privacy-compliance/requirements/{requirementId}/export.pdf?{Query(("companyName", companyName))}");

            var sessionToken = await ResolveSessionTokenAsync();
            if (!string.IsNullOrWhiteSpace(sessionToken)
                && !request.Headers.Contains(SessionTokenHeader))
            {
                request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
            }

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                return ApiCallResult<byte[]>.Success(bytes);
            }

            var error = await ToExceptionAsync(response, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiCallResult<byte[]>.NotFound(error),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallResult<byte[]>.Validation(error),
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => ApiCallResult<byte[]>.Failed(error),
                _ => ApiCallResult<byte[]>.Failed(error)
            };
        }
        catch (HttpRequestException ex)
        {
            return ApiCallResult<byte[]>.Unavailable(new PlatformApiException(null, "Platform API unavailable", ex.Message, innerException: ex));
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return ApiCallResult<byte[]>.Unavailable(new PlatformApiException(null, "Platform API timed out", ex.Message, innerException: ex));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
    }

    public async Task<ApiCallResult<byte[]>> DownloadCatalogImportTemplateAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/v1/platform/global-catalog/products/imports/template.csv");

            var sessionToken = await ResolveSessionTokenAsync();
            if (!string.IsNullOrWhiteSpace(sessionToken)
                && !request.Headers.Contains(SessionTokenHeader))
            {
                request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
            }

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                return ApiCallResult<byte[]>.Success(bytes);
            }

            var error = await ToExceptionAsync(response, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiCallResult<byte[]>.NotFound(error),
                HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallResult<byte[]>.Validation(error),
                HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized => ApiCallResult<byte[]>.Failed(error),
                _ => ApiCallResult<byte[]>.Failed(error)
            };
        }
        catch (HttpRequestException ex)
        {
            return ApiCallResult<byte[]>.Unavailable(new PlatformApiException(null, "Platform API unavailable", ex.Message, innerException: ex));
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return ApiCallResult<byte[]>.Unavailable(new PlatformApiException(null, "Platform API timed out", ex.Message, innerException: ex));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
    }

    private static MembershipLifecycleRequest WithActor(MembershipLifecycleRequest request) =>
        request with { ActorReference = string.IsNullOrWhiteSpace(request.ActorReference) ? DevActor : request.ActorReference };

    private Task<ApiCallResult<T>> GetAsync<T>(string path, CancellationToken ct) =>
        SendAsync<T>(HttpMethod.Get, path, null, ct);

    private async Task<ApiCallResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            // Blazor Server circuit: preserve sync context across awaits.
            var sessionToken = await ResolveSessionTokenAsync();
            if (!string.IsNullOrWhiteSpace(sessionToken)
                && !request.Headers.Contains(SessionTokenHeader))
            {
                request.Headers.TryAddWithoutValidation(SessionTokenHeader, sessionToken);
            }

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                T? data;
                try
                {
                    data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
                }
                catch (JsonException ex)
                {
                    return ApiCallResult<T>.Failed(new PlatformApiException(
                        response.StatusCode,
                        "Invalid API response",
                        ex.Message,
                        innerException: ex));
                }

                return data is null
                    ? ApiCallResult<T>.Failed(new PlatformApiException(response.StatusCode, "Invalid API response", "The API returned no content."))
                    : ApiCallResult<T>.Success(data);
            }

            var error = await ToExceptionAsync(response, ct);
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
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            title = document.RootElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
            detail = document.RootElement.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(detail)
                && document.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() > 0)
                    {
                        var first = property.Value[0].GetString();
                        if (!string.IsNullOrWhiteSpace(first))
                        {
                            detail = $"{property.Name}: {first}";
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException) { }
        response.Headers.TryGetValues("X-Correlation-ID", out var ids);
        return new PlatformApiException(response.StatusCode, title ?? response.ReasonPhrase ?? "Platform API request failed", detail, ids?.FirstOrDefault());
    }

    private async Task<string?> ResolveSessionTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(circuitSession.SessionToken))
        {
            return circuitSession.SessionToken;
        }

        try
        {
            var fromHttp = httpContextAccessor.HttpContext is { } http
                ? PlatformBrowserSessionService.ResolveSessionToken(http)
                : null;
            if (!string.IsNullOrWhiteSpace(fromHttp))
            {
                circuitSession.SessionToken = fromHttp;
                return fromHttp;
            }
        }
        catch
        {
            // HttpContext can be disposed mid-circuit.
        }

        try
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            var fromAuth = state.User.FindFirstValue(PlatformBrowserSessionService.SessionTokenClaimType);
            if (!string.IsNullOrWhiteSpace(fromAuth))
            {
                circuitSession.SessionToken = fromAuth;
            }

            return fromAuth;
        }
        catch
        {
            return circuitSession.SessionToken;
        }
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static string Query(params (string Key, object? Value)[] values) =>
        string.Join("&", values.Where(v => v.Value is not null && !string.IsNullOrWhiteSpace(v.Value.ToString())).Select(v => $"{Escape(v.Key)}={Escape(v.Value!.ToString()!)}"));
}
