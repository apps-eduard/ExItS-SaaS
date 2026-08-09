using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Permissions;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class RoleHomeResolverTests
{
    [Theory]
    [InlineData("Owner", RoleHomeResolver.OwnerHome)]
    [InlineData("Admin", RoleHomeResolver.OwnerHome)]
    [InlineData("StoreManager", RoleHomeResolver.ManagerHome)]
    [InlineData("Cashier", RoleHomeResolver.CashierHome)]
    [InlineData("", RoleHomeResolver.OrgEssentials)]
    public async Task ResolvePosHome_maps_effective_role(string role, string expected)
    {
        var client = new FakePermissions(string.IsNullOrEmpty(role) ? null : role, status: string.IsNullOrEmpty(role) ? "None" : "Active");
        var sut = new RoleHomeResolver(client, new SellingModeService(), FakeUser.WithOrg());
        Assert.Equal(expected, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_returns_personal_when_no_organization_bound()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Active"), mode, FakeUser.Personal());
        Assert.Equal(RoleHomeResolver.PersonalHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_personal_default_ignores_forged_organization_id()
    {
        var sut = new RoleHomeResolver(
            new FakePermissions("Owner", status: "Active"),
            new SellingModeService(),
            FakeUser.PersonalWithForgedOrganization());
        Assert.Equal(RoleHomeResolver.PersonalHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_org_scoped_staff_still_routes_to_org_essentials_without_pos()
    {
        var sut = new RoleHomeResolver(
            new FakePermissions("Cashier", status: "Active"),
            new SellingModeService(),
            FakeUser.OrgScopedStaffNoPosAccess());
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_denies_inactive_assignment()
    {
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Revoked"), new SellingModeService(), FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_owner_can_work_as_manager_or_cashier_ui()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Active"), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());

        mode.EnterWorkingAs(RoleHomeResolver.CashierHome);
        Assert.Equal(RoleHomeResolver.CashierHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_when_effective_role_unavailable()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.OwnerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_routes_to_org_when_effective_status_none_even_with_working_as()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions(role: null, status: "None"), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_routes_organization_owner_without_pos_access_to_org_overview()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Active"), mode, FakeUser.WithOrgNoPosAccess());
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_entitlement_without_pos_access_never_reaches_a_dashboard_or_access_denied()
    {
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), new SellingModeService(), FakeUser.WithOrgNoPosAccess());
        var home = await sut.ResolvePosHomeAsync();

        Assert.Equal(RoleHomeResolver.OrgEssentials, home);
        Assert.NotEqual(RoleHomeResolver.AccessDenied, home);
    }

    [Fact]
    public async Task ResolvePosHome_pos_access_without_pos_role_does_not_authorize_role_dashboard()
    {
        var sut = new RoleHomeResolver(new FakePermissions(role: "", status: "Active"), new SellingModeService(), FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_denies_when_role_lookup_is_server_rejected()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(FakePermissions.Forbidden(), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_revoked_assignment_denies_even_with_working_as()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Owner", status: "Revoked"), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.AccessDenied, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_recalculates_when_organization_context_changes()
    {
        var user = FakeUser.WithOrg();
        var sut = new RoleHomeResolver(new FakePermissions("StoreManager", status: "Active"), new SellingModeService(), user);
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());

        user.SwitchToOrganizationWithoutPosAccess();
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());

        user.SwitchToPersonal();
        Assert.Equal(RoleHomeResolver.PersonalHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_returns_role_home_after_pos_access_is_enabled()
    {
        var user = FakeUser.WithOrgNoPosAccess();
        var sut = new RoleHomeResolver(new FakePermissions("Cashier", status: "Active"), new SellingModeService(), user);
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());

        user.GrantPosAccess();
        Assert.Equal(RoleHomeResolver.CashierHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_without_preferred_routes_to_org_when_effective_api_unavailable()
    {
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), new SellingModeService(), FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.OrgEssentials, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_when_effective_role_unparseable()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("NotARealRole", status: "Active"), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.OwnerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_uses_preferred_home_for_unknown_pos_role_codes()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.ManagerHome);
        var sut = new RoleHomeResolver(new FakePermissions("InventoryStaff", status: "Active"), mode, FakeUser.WithOrg());
        Assert.Equal(RoleHomeResolver.ManagerHome, await sut.ResolvePosHomeAsync());
    }

    [Fact]
    public async Task ResolvePosHome_staff_cannot_override_with_working_as()
    {
        var mode = new SellingModeService();
        mode.EnterWorkingAs(RoleHomeResolver.OwnerHome);
        var sut = new RoleHomeResolver(new FakePermissions("Cashier", status: "Active"), mode, FakeUser.WithOrg());
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

    [Fact]
    public async Task ResolvePosHome_uses_offline_grant_snapshot_without_permissions_call()
    {
        var permissions = FakePermissions.Unavailable();
        var grant = new FakeOfflineGrant("Cashier");
        var sut = new RoleHomeResolver(permissions, new SellingModeService(), FakeUser.WithOrg(), grant);
        Assert.Equal(RoleHomeResolver.CashierHome, await sut.ResolvePosHomeAsync());
        Assert.Equal(0, permissions.GetEffectiveCalls);
    }

    [Fact]
    public void ResolveFromOfflineGrantSnapshot_defaults_to_owner_when_role_missing()
    {
        var grant = new FakeOfflineGrant(roleCode: null);
        var sut = new RoleHomeResolver(FakePermissions.Unavailable(), new SellingModeService(), FakeUser.WithOrg(), grant);
        Assert.Equal(RoleHomeResolver.OwnerHome, sut.ResolveFromOfflineGrantSnapshot());
    }

    private sealed class FakeUser : ICurrentUserContext
    {
        private FakeUser(AuthSession? session) => Session = session;

        public static FakeUser WithOrg() => new(new AuthSession(
            UserId: Guid.NewGuid(),
            DisplayName: "Test",
            Username: "test",
            Email: "test@example.com",
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Org",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: true,
            AccessReasonCode: "allowed"));

        public static FakeUser WithOrgNoPosAccess()
        {
            var user = WithOrg();
            user.Session = user.Session! with { HasPosAccess = false, AccessReasonCode = "assignment_missing" };
            return user;
        }

        public void GrantPosAccess() =>
            Session = Session! with { HasPosAccess = true, AccessReasonCode = "allowed" };

        public void SwitchToOrganizationWithoutPosAccess() =>
            Session = Session! with
            {
                OrganizationId = Guid.NewGuid(),
                OrganizationDisplayName = "Other org",
                HasPosAccess = false,
                AccessReasonCode = "assignment_missing"
            };

        public void SwitchToPersonal() =>
            Session = Session! with
            {
                OrganizationId = null,
                OrganizationDisplayName = null,
                HasPosAccess = false,
                AccessReasonCode = null
            };

        public static FakeUser Personal() => new(new AuthSession(
            UserId: Guid.NewGuid(),
            DisplayName: "Test",
            Username: "test",
            Email: "test@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccountClass: "Personal",
            OrganizationContextLocked: false));

        public static FakeUser PersonalWithForgedOrganization() => new(new AuthSession(
            UserId: Guid.NewGuid(),
            DisplayName: "Test",
            Username: "test",
            Email: "test@example.com",
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Stale Org",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccountClass: "Personal",
            OrganizationContextLocked: false));

        public static FakeUser OrgScopedStaffNoPosAccess() => new(new AuthSession(
            UserId: Guid.NewGuid(),
            DisplayName: "Staff",
            Username: "staff1",
            Email: "staff1@ORG000001.exits.local",
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Store",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
            HasPosAccess: false,
            AccessReasonCode: "assignment_missing",
            AccountClass: "Organization",
            OrganizationContextLocked: true));

        public AuthSession? Session { get; private set; }
        public bool IsAuthenticated => Session is not null;
        public bool HasPosAccess => Session?.HasPosAccess == true;
        public event Func<Task>? Changed;
        public void Set(AuthSession? session) => Session = session;
        public void Clear() => Session = null;
    }

    private sealed class FakePermissions(
        string? role,
        string status,
        bool unavailable = false,
        bool forbidden = false) : IPosPermissionClient
    {
        public static FakePermissions Unavailable() => new(null, "None", unavailable: true);

        public static FakePermissions Forbidden() => new(null, "None", forbidden: true);

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

        public int GetEffectiveCalls { get; private set; }

        public Task<ApiResult<PosEffectivePermissionsDto>> GetEffectiveAsync(CancellationToken ct = default)
        {
            GetEffectiveCalls++;
            if (unavailable)
            {
                return Task.FromResult(ApiResult<PosEffectivePermissionsDto>.Unavailable());
            }

            if (forbidden)
            {
                return Task.FromResult(ApiResult<PosEffectivePermissionsDto>.Failure(ApiCallStatus.Forbidden));
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

    private sealed class FakeOfflineGrant(string? roleCode) : IOfflineOperatingGrantService
    {
        public bool IsUnlockedThisProcess => true;

        public OfflineOperatingGrant? ActiveUnlockedGrant { get; } = new(
            SchemaVersion: OfflineOperatingGrant.CurrentSchemaVersion,
            UserId: Guid.NewGuid(),
            OrganizationId: Guid.NewGuid(),
            OrganizationDisplayName: "Org",
            DeviceId: "device",
            RoleCode: roleCode,
            EnabledFeatureCodes: Array.Empty<string>(),
            SubscriptionStatus: "Active",
            DisplayName: "Test",
            Username: "test",
            Email: "test@example.com",
            IssuedAtUtc: DateTimeOffset.UtcNow,
            LastOnlineValidatedAtUtc: DateTimeOffset.UtcNow,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(12),
            ScopeKind: OfflineGrantScopeKind.Organization);

        public Task EstablishFromOnlineSessionAsync(AuthSession session, string deviceId, string? roleCode, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task ClearAsync(CancellationToken ct = default) => Task.CompletedTask;

        public void LockThisProcess()
        {
        }

        public Task<bool> HasPinConfiguredAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<OfflinePinSetupResult> SetPinAsync(string pin, CancellationToken ct = default) =>
            Task.FromResult(new OfflinePinSetupResult(true));

        public Task<OfflineColdStartOffer> EvaluateColdStartOfferAsync(CancellationToken ct = default) =>
            Task.FromResult(new OfflineColdStartOffer(true, ActiveUnlockedGrant, null));

        public Task<OfflinePinUnlockResult> UnlockWithPinAsync(string pin, CancellationToken ct = default) =>
            Task.FromResult(new OfflinePinUnlockResult(OfflinePinUnlockStatus.Succeeded, ActiveUnlockedGrant));

        public Task<OfflineOperatingGrant?> PeekStoredGrantAsync(CancellationToken ct = default) =>
            Task.FromResult(ActiveUnlockedGrant);
    }
}
