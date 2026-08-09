using ExItS.Platform.Domain.Authorization;

namespace ExItS.Platform.UnitTests.PrivacyCompliance;

public sealed class PrivacyComplianceAuthorizationCatalogTests
{
    [Fact]
    public void Platform_administrator_has_view_and_manage_privacy_compliance()
    {
        var permissions = PlatformRolePermissionCatalog.GetPermissions(PlatformSystemRole.PlatformAdministrator);
        Assert.Contains(PlatformPermission.ViewPrivacyCompliance, permissions);
        Assert.Contains(PlatformPermission.ManagePrivacyCompliance, permissions);
    }

    [Fact]
    public void Platform_auditor_has_view_only()
    {
        var permissions = PlatformRolePermissionCatalog.GetPermissions(PlatformSystemRole.PlatformAuditor);
        Assert.Contains(PlatformPermission.ViewPrivacyCompliance, permissions);
        Assert.DoesNotContain(PlatformPermission.ManagePrivacyCompliance, permissions);
    }

    [Fact]
    public void Platform_support_does_not_receive_privacy_compliance_manage()
    {
        var permissions = PlatformRolePermissionCatalog.GetPermissions(PlatformSystemRole.PlatformSupport);
        Assert.DoesNotContain(PlatformPermission.ViewPrivacyCompliance, permissions);
        Assert.DoesNotContain(PlatformPermission.ManagePrivacyCompliance, permissions);
    }

    [Fact]
    public void Permission_codes_are_listed_in_All()
    {
        Assert.Contains(PlatformPermission.ViewPrivacyCompliance, PlatformPermission.All);
        Assert.Contains(PlatformPermission.ManagePrivacyCompliance, PlatformPermission.All);
    }
}
