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
}
