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
        var sut = new RoleHomeResolver(client, new SellingModeService());
        Assert.Equal(expected, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_denies_inactive_assignment()
    {
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Revoked"), new SellingModeService());
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_owner_can_work_as_manager_or_cashier_ui()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Active"), mode);
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());

        mode.EnterWorkingAs(RoleHomeResolver.CashierHome);
        Assert.Equal(RoleHomeResolver.CashierHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_when_effective_role_unavailable()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), mode);
        Assert.Equal(RoleHomeResolver.OwnerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_when_effective_status_none()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions(role: null, status: "None"), mode);
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_without_preferred_still_denies_when_effective_missing()
    {
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), new SellingModeService());
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_when_effective_role_unparseable()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("NotARealRole", status: "Active"), mode);
        Assert.Equal(RoleHomeResolver.OwnerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_for_unknown_pos_role_codes()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions("InventoryStaff", status: "Active"), mode);
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_staff_cannot_override_with_working_as()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Cashier", status: "Active"), mode);
        Assert.Equal(RoleHomeResolver.CashierHome, await sut.ResolvePosHomeAsync());
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
        Assert.Equal(RoleHomeResolver.OwnerHome, mode.ReturnRoute);
    }

    [Fact]
    public void SellingMode_enter_working_as_sets_preferred_home()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.CashierHome);
        Assert.False(mode.IsSellingMode);
        Assert.Equal(RoleHomeResolver.CashierHome, mode.PreferredHomeRoute);
        mode.Clear();
        Assert.Null(mode.PreferredHomeRoute);
    }

    private sealed class FakePermissions(string? role, string status, bool unavailable = false) : IPosPermissionClient
    {
        public static FakePermissions Unavailable() => new(null, "None", unavailable: true);

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

        public Task<ApiResult<PosEffectivePermissionsDto>> GetEffectiveAsync(CancellationToken ct = default)
        {
            if (unavailable)
            {
                return Task.FromResult(ApiResult<PosEffectivePermissionsDto>.Unavailable());
            }

            return Task.FromResult(ApiResult<PosEffectivePermissionsDto>.Success(new PosEffectivePermissionsDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                role,
                role,
                status,
                Array.Empty<string>(),
                Array.Empty<string>(),
                false,
                false)));
        }

        public Task<ApiResult<PosEffectivePermissionsDto>> GetActorEffectiveAsync(Guid actorId, CancellationToken ct = default) =>
            GetEffectiveAsync(ct);
    }
}
