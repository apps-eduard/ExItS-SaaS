using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Permissions;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class RoleHomeResolverTests
{
    [Theory]
    [InlineData("Owner", RoleHomeResolver.OwnerHome)]
    [InlineData("Admin", RoleHomeResolver.OwnerHome)]
    [InlineData("StoreManager", RoleHomeResolver.ManagerHome)]
    [InlineData("Cashier", RoleHomeResolver.CashierHome)]
    [InlineData("", RoleHomeResolver.AccessDenied)]
    public async Task ResolvePosHome_maps_effective_role(string role, string expected)
    {
        var client = new FakePermissions(string.IsNullOrEmpty(role) ? null : role, status: string.IsNullOrEmpty(role) ? "None" : "Active");
        var sut = new RoleHomeResolver(client);
        Assert.Equal(expected, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_denies_inactive_assignment()
    {
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Revoked"));
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public void SellingMode_preserves_return_route_without_changing_role()
    {
        var mode = new SellingModeService();
        mode.Enter(RoleHomeResolver.OwnerHome);
        Assert.True(mode.IsSellingMode);
        Assert.Equal(RoleHomeResolver.OwnerHome, mode.ReturnRoute);
        mode.Exit();
        Assert.False(mode.IsSellingMode);
        Assert.Null(mode.ReturnRoute);
    }

    private sealed class FakePermissions(string? role, string status) : IPosPermissionClient
    {
        public Task<ApiResult<IReadOnlyList<PosRoleDto>>> ListRolesAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PosRoleDto>>.Success(Array.Empty<PosRoleDto>()));

        public Task<ApiResult<PosRoleAssignmentListDto>> ListAssignmentsAsync(string? status = null, Guid? actorId = null, string? role = null, int? page = null, int? pageSize = null, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PosRoleAssignmentListDto>.Unavailable());

        public Task<ApiResult<PosRoleAssignmentDto>> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PosRoleAssignmentDto>.Unavailable());

        public Task<ApiResult<PosRoleAssignmentDto>> AssignAsync(AssignPosRoleRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PosRoleAssignmentDto>.Unavailable());

        public Task<ApiResult<PosRoleAssignmentDto>> RevokeAsync(Guid assignmentId, RevokePosRoleRequest? request = null, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PosRoleAssignmentDto>.Unavailable());

        public Task<ApiResult<PosEffectivePermissionsDto>> GetEffectiveAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PosEffectivePermissionsDto>.Success(new PosEffectivePermissionsDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                role,
                role,
                status,
                Array.Empty<string>(),
                Array.Empty<string>(),
                false,
                false)));

        public Task<ApiResult<PosEffectivePermissionsDto>> GetActorEffectiveAsync(Guid actorId, CancellationToken ct = default) =>
            GetEffectiveAsync(ct);
    }
}
