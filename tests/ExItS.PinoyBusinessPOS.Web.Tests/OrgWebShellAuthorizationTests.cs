using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Web.Services;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebShellAuthorizationTests
{
    [Fact]
    public void Owner_sees_management_sections()
    {
        var shell = new OrgWebShellState { MembershipRole = "OrganizationOwner" };
        Assert.True(shell.CanAccessOrganizationWeb);
        Assert.True(shell.HasOrganizationManagementAuthority);
        Assert.True(shell.CanSee("overview"));
        Assert.True(shell.CanSee("profile"));
        Assert.True(shell.CanSee("branches"));
        Assert.True(shell.CanSee("staff"));
        Assert.True(shell.CanSee("subscription"));
        Assert.True(shell.CanSee("ownership-transfer"));
        Assert.True(shell.CanSee("sales-documents"));
        Assert.True(shell.CanSee("settings"));
    }

    [Fact]
    public void StoreManager_alone_is_denied_organization_web_admin_host()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.StoreManager,
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewDashboard),
                nameof(UtangCapability.ViewCatalog),
                nameof(UtangCapability.ViewInventory),
                nameof(UtangCapability.ViewReports),
                nameof(UtangCapability.ViewShifts),
                nameof(UtangCapability.ViewRegisters),
                nameof(UtangCapability.ViewCustomersAndHistory),
                nameof(UtangCapability.ViewOperationalSetup),
                nameof(UtangCapability.CreateSale),
                nameof(UtangCapability.ManageInventory)
            ]
        };

        Assert.True(shell.IsPosOperationsManager);
        Assert.False(shell.HasOrganizationManagementAuthority);
        Assert.False(shell.CanAccessOrganizationWeb);
        Assert.False(shell.CanSee("overview"));
        Assert.False(shell.CanSee("branches"));
        Assert.False(shell.CanSee("staff"));
        Assert.False(shell.CanSee("products"));
        Assert.False(shell.CanSee("reports"));
    }

    [Fact]
    public void OrganizationAdministrator_sees_day_to_day_admin_not_owner_only()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationAdministrator",
            PosRole = null,
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewDashboard),
                nameof(UtangCapability.ViewCatalog)
            ]
        };

        Assert.True(shell.IsOrganizationAdministrator);
        Assert.True(shell.HasOrganizationManagementAuthority);
        Assert.False(shell.IsPosOperationsManager);
        Assert.True(shell.CanAccessOrganizationWeb);
        Assert.True(shell.CanSee("overview"));
        Assert.True(shell.CanSee("branches"));
        Assert.True(shell.CanSee("staff"));
        Assert.False(shell.CanSee("ownership-transfer"));
        Assert.False(shell.CanSee("subscription"));
    }

    [Fact]
    public void Cashier_is_denied_organization_web()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.Cashier,
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewCatalog),
                nameof(UtangCapability.CreateSale),
                nameof(UtangCapability.ViewShifts),
                nameof(UtangCapability.ViewRegisters)
            ]
        };

        Assert.True(shell.IsCashierDenied);
        Assert.False(shell.CanAccessOrganizationWeb);
        Assert.False(shell.CanSee("products"));
        Assert.False(shell.CanSee("shifts"));
        Assert.False(shell.CanSee("staff"));
        Assert.False(shell.CanSee("overview"));
        Assert.False(shell.CanSee("settings"));
    }

    [Fact]
    public void Reporting_user_sees_reports_not_staff()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.ReportingUser,
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewReports),
                nameof(UtangCapability.ViewDashboard)
            ]
        };

        Assert.True(shell.CanAccessOrganizationWeb);
        Assert.True(shell.CanSee("overview"));
        Assert.True(shell.CanSee("reports"));
        Assert.False(shell.CanSee("staff"));
        Assert.False(shell.CanSee("products"));
    }

    [Fact]
    public void Inventory_staff_sees_inventory_not_sales_reports()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.InventoryStaff,
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewInventory),
                nameof(UtangCapability.ManageInventory)
            ]
        };

        Assert.True(shell.CanAccessOrganizationWeb);
        Assert.True(shell.CanSee("inventory"));
        Assert.False(shell.CanSee("reports"));
        Assert.False(shell.CanSee("staff"));
    }

    [Fact]
    public void OrgWebUi_sanitizes_development_header_errors()
    {
        var sanitized = OrgWebUi.Error(
            new ApiError(
                Title: "Forbidden",
                Detail: "Development-stage organization, actor, and commercial headers are unavailable outside Development/Testing.",
                ErrorCode: "pos.development_headers.unavailable",
                CorrelationId: null,
                StatusCode: 403));
        Assert.DoesNotContain("Development-stage", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Platform Admin", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sign out and sign in again", sanitized, StringComparison.OrdinalIgnoreCase);
    }
}
