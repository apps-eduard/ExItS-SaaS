using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Web.Services;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

/// <summary>
/// Organization Owner effective management matrix for Org Web (server authority is mirrored in shell gates).
/// Owner ≠ automatic POS Cashier / CreateSale.
/// </summary>
public sealed class OrganizationOwnerManagementMatrixTests
{
    [Fact]
    public void OrganizationOwnerCanLoadOverview()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("overview"));
    }

    [Fact]
    public void OrganizationOwnerCanManageBranches()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("branches"));
    }

    [Fact]
    public void OrganizationOwnerCanManageStaff()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("staff"));
        Assert.True(shell.CanSee("roles"));
    }

    [Fact]
    public void OrganizationOwnerCanManageCatalog()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("products"));
    }

    [Fact]
    public void OrganizationOwnerCanManageInventory()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("inventory"));
    }

    [Fact]
    public void OrganizationOwnerCanViewSalesAndReports()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("sales"));
        Assert.True(shell.CanSee("reports"));
        Assert.True(shell.CanSee("shifts"));
    }

    [Fact]
    public void OrganizationOwnerCanManageSettings()
    {
        var shell = OwnerShell();
        Assert.True(shell.CanSee("settings"));
        Assert.True(shell.CanSee("sales-documents"));
        Assert.True(shell.CanSee("subscription"));
        Assert.True(shell.CanSee("ownership-transfer"));
        Assert.True(shell.CanSee("devices"));
        Assert.True(shell.CanSee("registers"));
    }

    [Fact]
    public void OrganizationOwnerCannotCheckoutWithoutPosRole()
    {
        var shell = OwnerShell();
        Assert.False(shell.Can(UtangCapability.CreateSale));
        Assert.False(shell.Can(UtangCapability.EnterPos));
        Assert.DoesNotContain(
            nameof(UtangCapability.CreateSale),
            shell.AllowedCapabilities,
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrganizationManagerHasManagementSubset()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationAdministrator",
            PosRole = "OrganizationAdministrator",
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewDashboard),
                nameof(UtangCapability.ManageCatalog),
                nameof(UtangCapability.ViewInventory),
                nameof(UtangCapability.ViewReports),
                nameof(UtangCapability.ViewShifts),
                nameof(UtangCapability.ViewRegisters),
                nameof(UtangCapability.ViewOperationalSetup)
            ]
        };

        Assert.True(shell.IsOrgManager);
        Assert.True(shell.CanSee("overview"));
        Assert.True(shell.CanSee("branches"));
        Assert.True(shell.CanSee("staff"));
        Assert.True(shell.CanSee("products"));
        Assert.False(shell.CanSee("ownership-transfer"));
        Assert.False(shell.CanSee("subscription"));
        Assert.False(shell.CanSee("sales-documents"));
        Assert.False(shell.Can(UtangCapability.CreateSale));
    }

    [Fact]
    public void OrganizationCashierCannotAccessOrgWeb()
    {
        var shell = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.Cashier,
            AllowedCapabilities = [nameof(UtangCapability.CreateSale)]
        };

        Assert.False(shell.CanAccessOrganizationWeb);
        Assert.False(shell.CanSee("overview"));
    }

    [Fact]
    public void MultiOrgEffectiveAuthorization_recomputes_per_membership()
    {
        var ownerOrg = OwnerShell();
        Assert.True(ownerOrg.CanSee("ownership-transfer"));

        var managerOrg = new OrgWebShellState
        {
            MembershipRole = "OrganizationAdministrator",
            PosRole = PosRoleCodes.StoreManager,
            AllowedCapabilities = [nameof(UtangCapability.ViewDashboard)]
        };
        Assert.True(managerOrg.CanAccessOrganizationWeb);
        Assert.False(managerOrg.CanSee("ownership-transfer"));

        var cashierOrg = new OrgWebShellState
        {
            MembershipRole = "OrganizationMember",
            PosRole = PosRoleCodes.Cashier,
            AllowedCapabilities = [nameof(UtangCapability.CreateSale)]
        };
        Assert.False(cashierOrg.CanAccessOrganizationWeb);
    }

    [Fact]
    public void Hydrator_binds_bearer_from_session_grant_not_evaluate_access()
    {
        var root = FindRepoRoot();
        var hydrator = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Services",
            "WebHostServices.cs"));

        Assert.Contains("IssueTokenAsync", hydrator, StringComparison.Ordinal);
        Assert.Contains("Organization management authority", hydrator, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EvaluateAccessAsync(", hydrator, StringComparison.Ordinal);
        Assert.Contains("ProductAccessAllowed != false", hydrator, StringComparison.Ordinal);
    }

    private static OrgWebShellState OwnerShell() =>
        new()
        {
            MembershipRole = "OrganizationOwner",
            PosRole = "OrganizationOwner",
            AllowedCapabilities =
            [
                nameof(UtangCapability.ViewDashboard),
                nameof(UtangCapability.ManageCatalog),
                nameof(UtangCapability.ManageInventory),
                nameof(UtangCapability.ViewSales),
                nameof(UtangCapability.ViewReports),
                nameof(UtangCapability.ViewShifts),
                nameof(UtangCapability.ManageRegisters),
                nameof(UtangCapability.ViewOperationalSetup),
                nameof(UtangCapability.ManageOperationalSetup),
                nameof(UtangCapability.ViewCustomersAndHistory)
            ]
        };

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
