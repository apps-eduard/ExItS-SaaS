using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.UnitTests.Authorization;

public sealed class PlatformRolePermissionCatalogTests
{
    [Fact]
    public void PlatformAdministrator_holds_every_defined_permission()
    {
        var permissions = PlatformRolePermissionCatalog.GetPermissions(PlatformSystemRole.PlatformAdministrator);

        Assert.Equal(PlatformPermission.All.Count, permissions.Count);
        foreach (var permission in PlatformPermission.All)
        {
            Assert.True(PlatformRolePermissionCatalog.RoleHasPermission(PlatformSystemRole.PlatformAdministrator, permission));
        }
    }

    [Theory]
    [InlineData(PlatformPermission.ViewPortfolio)]
    [InlineData(PlatformPermission.ManageOrganizations)]
    [InlineData(PlatformPermission.ManageSubscriptions)]
    [InlineData(PlatformPermission.ManageManualPayments)]
    [InlineData(PlatformPermission.ViewAuditRecords)]
    public void BillingAdministrator_holds_billing_related_permissions(string permission)
    {
        Assert.True(PlatformRolePermissionCatalog.RoleHasPermission(PlatformSystemRole.BillingAdministrator, permission));
    }

    [Theory]
    [InlineData(PlatformPermission.ManagePlatformUsers)]
    [InlineData(PlatformPermission.ManageMemberships)]
    [InlineData(PlatformPermission.ManageProductAccess)]
    [InlineData(PlatformPermission.ManageEntitlementOverrides)]
    public void BillingAdministrator_does_not_hold_operational_or_identity_permissions(string permission)
    {
        Assert.False(PlatformRolePermissionCatalog.RoleHasPermission(PlatformSystemRole.BillingAdministrator, permission));
    }

    [Theory]
    [InlineData(PlatformPermission.ViewPortfolio)]
    [InlineData(PlatformPermission.ManageMemberships)]
    [InlineData(PlatformPermission.ManageProductAccess)]
    [InlineData(PlatformPermission.ViewAuditRecords)]
    public void PlatformSupport_holds_support_related_permissions(string permission)
    {
        Assert.True(PlatformRolePermissionCatalog.RoleHasPermission(PlatformSystemRole.PlatformSupport, permission));
    }

    [Theory]
    [InlineData(PlatformPermission.ManagePlatformUsers)]
    [InlineData(PlatformPermission.ManageOrganizations)]
    [InlineData(PlatformPermission.ManageSubscriptions)]
    [InlineData(PlatformPermission.ManageManualPayments)]
    [InlineData(PlatformPermission.ManageEntitlementOverrides)]
    public void PlatformSupport_does_not_hold_billing_or_identity_permissions(string permission)
    {
        Assert.False(PlatformRolePermissionCatalog.RoleHasPermission(PlatformSystemRole.PlatformSupport, permission));
    }

    [Fact]
    public void GetPermissions_throws_for_undefined_role()
    {
        Assert.Throws<DomainException>(() => PlatformRolePermissionCatalog.GetPermissions((PlatformSystemRole)999));
    }

    [Fact]
    public void Every_platform_system_role_is_covered_by_the_catalog()
    {
        foreach (var role in Enum.GetValues<PlatformSystemRole>())
        {
            var permissions = PlatformRolePermissionCatalog.GetPermissions(role);
            Assert.NotEmpty(permissions);
            foreach (var permission in permissions)
            {
                Assert.Contains(permission, PlatformPermission.All);
            }
        }
    }
}
