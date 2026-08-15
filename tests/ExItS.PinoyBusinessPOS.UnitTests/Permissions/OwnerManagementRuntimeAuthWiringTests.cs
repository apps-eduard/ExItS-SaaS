using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Permissions;

/// <summary>
/// Closer-to-runtime wiring than isolated matrix lookups: management authority allows
/// overview/management caps and still denies CreateSale without a selling role.
/// </summary>
public sealed class OwnerManagementRuntimeAuthWiringTests
{
    [Fact]
    public void OwnerManagementTokenAcceptedForOverviewCapability()
    {
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.ViewDashboard));
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.ViewReports));
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.ManageCatalog));
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.ManageInventory));
    }

    [Fact]
    public void OwnerCreateSaleDeniedWithoutSellingRole()
    {
        Assert.False(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.CreateSale));
        Assert.False(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: true,
            UtangCapability.EnterPos));
    }

    [Fact]
    public void ManagerManagementStillWorksWithoutOwnerOnlyCaps()
    {
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: false,
            UtangCapability.ViewDashboard));
        Assert.True(PosRoleMatrix.AllowsOrganizationManagement(
            isExactOwner: false,
            UtangCapability.ManageCatalog));
        var ownerCaps = PosRoleMatrix.OrganizationManagementCapabilities(isExactOwner: true);
        var managerCaps = PosRoleMatrix.OrganizationManagementCapabilities(isExactOwner: false);
        Assert.True(ownerCaps.Count >= managerCaps.Count);
        Assert.DoesNotContain(UtangCapability.CreateSale, managerCaps);
        Assert.DoesNotContain(UtangCapability.CreateSale, ownerCaps);
    }

    [Fact]
    public void CashierOrgWebStillDeniedAtMatrix()
    {
        Assert.True(PosRoleMatrix.Allows(PosRole.Cashier, UtangCapability.CreateSale));
        Assert.False(PosRoleMatrix.Allows(PosRole.Cashier, UtangCapability.ManageCatalog));
        Assert.False(PosRoleMatrix.Allows(PosRole.Cashier, UtangCapability.ViewDashboard));
    }
}
