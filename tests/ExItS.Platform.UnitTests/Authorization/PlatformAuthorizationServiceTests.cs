using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Authorization;

public sealed class PlatformAuthorizationServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static PlatformAuthorizationService CreateService(
        InMemoryPlatformRoleAssignmentRepository repository,
        bool grantDevelopmentOperatorFullAccess) =>
        new(repository, Options.Create(new DevelopmentAuthorizationOptions
        {
            GrantDevelopmentOperatorFullAccess = grantDevelopmentOperatorFullAccess
        }));

    [Fact]
    public async Task DevelopmentOperator_receives_all_permissions_when_option_enabled()
    {
        var service = CreateService(new InMemoryPlatformRoleAssignmentRepository(), grantDevelopmentOperatorFullAccess: true);
        var actor = new PlatformActorContext("development-operator:unauthenticated", AuditActorType.DevelopmentOperator, null, null);

        var permissions = await service.ResolvePermissionsForActorAsync(actor);

        Assert.Equal(PlatformPermission.All.Count, permissions.Count);
        foreach (var permission in PlatformPermission.All)
        {
            Assert.Contains(permission, permissions);
        }
    }

    [Fact]
    public async Task DevelopmentOperator_receives_no_permissions_when_option_disabled()
    {
        var service = CreateService(new InMemoryPlatformRoleAssignmentRepository(), grantDevelopmentOperatorFullAccess: false);
        var actor = new PlatformActorContext("development-operator:unauthenticated", AuditActorType.DevelopmentOperator, null, null);

        var permissions = await service.ResolvePermissionsForActorAsync(actor);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task EnsurePermissionForActorAsync_denies_development_operator_when_option_disabled()
    {
        var service = CreateService(new InMemoryPlatformRoleAssignmentRepository(), grantDevelopmentOperatorFullAccess: false);
        var actor = new PlatformActorContext("development-operator:unauthenticated", AuditActorType.DevelopmentOperator, null, null);

        var result = await service.EnsurePermissionForActorAsync(actor, PlatformPermission.ManagePlatformUsers);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.AuthorizationDenied, result.ErrorCode);
    }

    [Fact]
    public async Task PlatformUser_receives_permissions_from_platform_wide_role_assignment()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var userId = PlatformUserId.New();
        await repository.AddAsync(PlatformRoleAssignment.Grant(
            userId, PlatformSystemRole.BillingAdministrator, organizationId: null, "dev-admin", T0));

        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);

        var permissions = await service.ResolvePermissionsAsync(userId);

        Assert.Contains(PlatformPermission.ManageSubscriptions, permissions);
        Assert.Contains(PlatformPermission.ManageManualPayments, permissions);
        Assert.DoesNotContain(PlatformPermission.ManagePlatformUsers, permissions);
    }

    [Fact]
    public async Task PlatformUser_with_no_assignments_has_no_permissions()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);

        var permissions = await service.ResolvePermissionsAsync(PlatformUserId.New());

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task Organization_scoped_assignment_applies_only_to_matching_organization()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var userId = PlatformUserId.New();
        var org1 = PlatformOrganizationId.New();
        var org2 = PlatformOrganizationId.New();
        await repository.AddAsync(PlatformRoleAssignment.Grant(
            userId, PlatformSystemRole.PlatformSupport, org1, "dev-admin", T0));

        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);

        var org1Permissions = await service.ResolvePermissionsAsync(userId, org1);
        var org2Permissions = await service.ResolvePermissionsAsync(userId, org2);
        var unscopedPermissions = await service.ResolvePermissionsAsync(userId);

        Assert.Contains(PlatformPermission.ManageMemberships, org1Permissions);
        Assert.Empty(org2Permissions);
        Assert.Empty(unscopedPermissions);
    }

    [Fact]
    public async Task Platform_wide_assignment_applies_to_every_organization_scope()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var userId = PlatformUserId.New();
        await repository.AddAsync(PlatformRoleAssignment.Grant(
            userId, PlatformSystemRole.PlatformAdministrator, organizationId: null, "dev-admin", T0));

        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);

        var anyOrgPermissions = await service.ResolvePermissionsAsync(userId, PlatformOrganizationId.New());

        Assert.Equal(PlatformPermission.All.Count, anyOrgPermissions.Count);
    }

    [Fact]
    public async Task Revoked_assignment_no_longer_grants_permissions()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var userId = PlatformUserId.New();
        var assignment = PlatformRoleAssignment.Grant(
            userId, PlatformSystemRole.PlatformSupport, organizationId: null, "dev-admin", T0);
        await repository.AddAsync(assignment);

        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);
        Assert.True(await service.HasPermissionAsync(userId, PlatformPermission.ManageMemberships));

        assignment.Revoke("dev-admin", "no longer needed", T0.AddMinutes(1));
        await repository.UpdateAsync(assignment);

        Assert.False(await service.HasPermissionAsync(userId, PlatformPermission.ManageMemberships));
    }

    [Fact]
    public async Task HasPermissionAsync_throws_for_unrecognized_permission_code()
    {
        var service = CreateService(new InMemoryPlatformRoleAssignmentRepository(), grantDevelopmentOperatorFullAccess: true);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.HasPermissionAsync(PlatformUserId.New(), "not.a.real.permission"));
    }

    [Fact]
    public async Task EnsurePermissionAsync_succeeds_when_permission_present_and_fails_with_message_when_absent()
    {
        var repository = new InMemoryPlatformRoleAssignmentRepository();
        var userId = PlatformUserId.New();
        await repository.AddAsync(PlatformRoleAssignment.Grant(
            userId, PlatformSystemRole.PlatformSupport, organizationId: null, "dev-admin", T0));
        var service = CreateService(repository, grantDevelopmentOperatorFullAccess: true);

        var granted = await service.EnsurePermissionAsync(userId, PlatformPermission.ManageMemberships);
        Assert.True(granted.IsSuccess);

        var denied = await service.EnsurePermissionAsync(userId, PlatformPermission.ManageOrganizations);
        Assert.False(denied.IsSuccess);
        Assert.Equal(DomainErrorCodes.AuthorizationDenied, denied.ErrorCode);
    }

    [Fact]
    public async Task Unauthenticated_actor_type_without_platform_user_id_has_no_permissions()
    {
        var service = CreateService(new InMemoryPlatformRoleAssignmentRepository(), grantDevelopmentOperatorFullAccess: true);
        var actor = new PlatformActorContext("system:scheduler", AuditActorType.System, null, null);

        var permissions = await service.ResolvePermissionsForActorAsync(actor);

        Assert.Empty(permissions);
    }
}
