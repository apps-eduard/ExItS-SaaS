using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Web.Services;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebShellAuthorizationTests
{
    [Fact]
    public void Owner_sees_management_sections()
    {
        var shell = new OrgWebShellState { MembershipRole = "OrganizationOwner" };
        Assert.True(shell.CanSee("overview"));
        Assert.True(shell.CanSee("profile"));
        Assert.True(shell.CanSee("branches"));
        Assert.True(shell.CanSee("staff"));
        Assert.True(shell.CanSee("subscription"));
        Assert.True(shell.CanSee("notifications"));
        Assert.True(shell.CanSee("settings"));
    }

    [Fact]
    public void Cashier_does_not_see_staff_or_settings()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewCatalog),
                nameof(UtangCapability.CreateSale),
                nameof(UtangCapability.ViewShifts),
                nameof(UtangCapability.ViewRegisters)
            ]
        };

        Assert.True(shell.CanSee("products"));
        Assert.True(shell.CanSee("shifts"));
        Assert.False(shell.CanSee("staff"));
        Assert.False(shell.CanSee("profile"));
        Assert.False(shell.CanSee("reports"));
        Assert.False(shell.CanSee("settings"));
        Assert.False(shell.CanSee("inventory"));
    }

    [Fact]
    public void Reporting_user_sees_reports_not_staff()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewReports),
                nameof(UtangCapability.ViewDashboard)
            ]
        };

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
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewInventory),
                nameof(UtangCapability.ManageInventory)
            ]
        };

        Assert.True(shell.CanSee("inventory"));
        Assert.False(shell.CanSee("reports"));
        Assert.False(shell.CanSee("staff"));
    }
}
