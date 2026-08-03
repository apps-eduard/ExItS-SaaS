using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PlatformAccessClient(IPosApiClient api) : IPlatformAccessClient
{
    public Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformUserDto>($"/api/v1/platform/users/{userId:D}", ct);

    public Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<PlatformOrganizationDto>($"/api/v1/platform/organizations/{organizationId:D}", ct);

    public Task<ApiResult<PlatformOrganizationDto>> UpdateOrganizationAsync(
        Guid organizationId,
        UpdatePlatformOrganizationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformOrganizationDto>(HttpMethod.Put, $"/api/v1/platform/organizations/{organizationId:D}", request, ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformPagedResult<PlatformMembershipDto>>(
            $"/api/v1/platform/users/{userId:D}/memberships?page=1&pageSize=100&status=Active",
            ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetOrganizationMembersAsync(
        Guid organizationId,
        int page = 1,
        int pageSize = 50,
        string? status = null,
        CancellationToken ct = default)
    {
        var path =
            $"/api/v1/platform/organizations/{organizationId:D}/members?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        return api.GetAsync<PlatformPagedResult<PlatformMembershipDto>>(path, ct);
    }

    public Task<ApiResult<PlatformMembershipDto>> SuspendMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/suspend",
            request,
            ct);

    public Task<ApiResult<PlatformMembershipDto>> RevokeMembershipAsync(
        Guid membershipId,
        PlatformMembershipLifecycleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformMembershipDto>(
            HttpMethod.Post,
            $"/api/v1/platform/memberships/{membershipId:D}/revoke",
            request,
            ct);

    public Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(
        Guid userId,
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<EffectiveAccessDto>(
            $"/api/v1/platform/access/evaluate?userId={userId:D}&organizationId={organizationId:D}&productCode={Uri.EscapeDataString(productCode)}",
            ct);

    public Task<ApiResult<PlatformAccessTokenIssueDto>> IssueTokenAsync(
        IssuePlatformAccessTokenRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token", request, ct);

    public Task<ApiResult<PlatformAccessTokenIssueDto>> BindTokenAsync(
        BindPlatformAccessTokenRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIssueDto>(HttpMethod.Post, "/api/v1/platform/auth/token/bind", request, ct);

    public Task<ApiResult<PlatformAccessTokenIntrospectionDto>> IntrospectTokenAsync(
        string? token = null,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformAccessTokenIntrospectionDto>(
            HttpMethod.Post,
            "/api/v1/platform/auth/introspect",
            new { token },
            ct);

    public Task<ApiResult<object>> RevokeAccessTokenAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/token/revoke", null, ct);

    public Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>(
            "/api/v1/platform/auth/organizations",
            ct);

    public Task<ApiResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(
        RegisterPersonalAccountRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PersonalRegistrationAckDto>(HttpMethod.Post, "/api/v1/platform/auth/register", request, ct);

    public Task<ApiResult<object>> ActivatePersonalAccountAsync(
        ActivatePersonalAccountRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/activate-account", request, ct);

    public Task<ApiResult<PlatformLoginResultDto>> LoginAsync(
        PlatformLoginRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformLoginResultDto>(HttpMethod.Post, "/api/v1/platform/auth/login", request, ct);

    public Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Post, "/api/v1/platform/auth/logout", null, ct);

    public Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(
        SelectAccountProfileRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<PlatformLoginResultDto>(HttpMethod.Post, "/api/v1/platform/auth/account-profiles/select", request, ct);

    public Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(
        StartBusinessRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<StartBusinessResultDto>(HttpMethod.Post, "/api/v1/personal/start-business", request, ct);

    public Task<ApiResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(
        Guid organizationId,
        CreateInvitationRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<OrganizationInvitationDto>(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{organizationId:D}/invitations",
            request,
            ct);

    public Task<ApiResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(
        Guid organizationId,
        string? status = null,
        CancellationToken ct = default)
    {
        var path = $"/api/v1/organizations/{organizationId:D}/product-local-roles";
        if (!string.IsNullOrWhiteSpace(status))
        {
            path += $"?status={Uri.EscapeDataString(status.Trim())}";
        }

        return api.GetAsync<IReadOnlyList<ProductLocalRoleGrantDto>>(path, ct);
    }

    public Task<ApiResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(
        Guid organizationId,
        AssignProductLocalRoleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ProductLocalRoleGrantDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/product-local-roles",
            request,
            ct);

    public Task<ApiResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(
        Guid organizationId,
        Guid grantId,
        RevokeProductLocalRoleRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<ProductLocalRoleGrantDto>(
            HttpMethod.Post,
            $"/api/v1/organizations/{organizationId:D}/product-local-roles/{grantId:D}/revoke",
            request,
            ct);

    public Task<ApiResult<PlatformSubscriptionDto>> GetCurrentSubscriptionAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformSubscriptionDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/subscriptions/current?productCode={Uri.EscapeDataString(productCode)}",
            ct);

    public Task<ApiResult<PlatformEntitlementSnapshotDto>> GetLatestEntitlementAsync(
        Guid organizationId,
        string productCode,
        CancellationToken ct = default) =>
        api.GetAsync<PlatformEntitlementSnapshotDto>(
            $"/api/v1/platform/organizations/{organizationId:D}/products/{Uri.EscapeDataString(productCode)}/entitlements/snapshots/latest",
            ct);

    public Task<ApiResult<object>> SetOrganizationContextAsync(
        SetOrganizationContextRequest request,
        CancellationToken ct = default) =>
        api.SendAsync<object>(HttpMethod.Put, "/api/v1/platform/auth/organization-context", request, ct);
}
