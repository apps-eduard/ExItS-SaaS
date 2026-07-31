using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PlatformAccessClient(IPosApiClient api) : IPlatformAccessClient
{
    public Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformUserDto>($"/api/v1/platform/users/{userId:D}", ct);

    public Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        api.GetAsync<PlatformOrganizationDto>($"/api/v1/platform/organizations/{organizationId:D}", ct);

    public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        api.GetAsync<PlatformPagedResult<PlatformMembershipDto>>(
            $"/api/v1/platform/users/{userId:D}/memberships?page=1&pageSize=100&status=Active",
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

    public Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(
        CancellationToken ct = default) =>
        api.GetAsync<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>(
            "/api/v1/platform/auth/organizations",
            ct);
}
