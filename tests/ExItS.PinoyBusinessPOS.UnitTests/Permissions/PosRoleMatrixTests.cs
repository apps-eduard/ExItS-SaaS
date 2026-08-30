using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Permissions;

public sealed class PosRoleMatrixTests
{
    [Theory]
    [InlineData(PosRole.Owner, UtangCapability.ManageCatalog, true)]
    [InlineData(PosRole.StoreManager, UtangCapability.ManageCatalog, true)]
    [InlineData(PosRole.Cashier, UtangCapability.ManageCatalog, false)]
    [InlineData(PosRole.Cashier, UtangCapability.ViewCatalog, true)]
    [InlineData(PosRole.InventoryStaff, UtangCapability.ManageCatalog, false)]
    [InlineData(PosRole.ReportingUser, UtangCapability.ManageCatalog, false)]
    [InlineData(PosRole.Owner, UtangCapability.ManagePermissions, true)]
    [InlineData(PosRole.Admin, UtangCapability.ManagePermissions, true)]
    [InlineData(PosRole.StoreManager, UtangCapability.ManagePermissions, false)]
    [InlineData(PosRole.StoreManager, UtangCapability.ViewPermissions, true)]
    [InlineData(PosRole.StoreManager, UtangCapability.VoidSale, true)]
    [InlineData(PosRole.Cashier, UtangCapability.VoidSale, false)]
    [InlineData(PosRole.Cashier, UtangCapability.ProcessReturn, false)]
    [InlineData(PosRole.Cashier, UtangCapability.CreateSale, true)]
    [InlineData(PosRole.Cashier, UtangCapability.ViewReports, false)]
    [InlineData(PosRole.Cashier, UtangCapability.ViewOperationalSetup, true)]
    [InlineData(PosRole.Cashier, UtangCapability.ManageOperationalSetup, false)]
    [InlineData(PosRole.StoreManager, UtangCapability.ViewOperationalSetup, true)]
    [InlineData(PosRole.StoreManager, UtangCapability.ManageOperationalSetup, false)]
    [InlineData(PosRole.Cashier, UtangCapability.ApplyCommercialDiscount, false)]
    [InlineData(PosRole.StoreManager, UtangCapability.ApplyCommercialDiscount, true)]
    [InlineData(PosRole.Cashier, UtangCapability.OverrideSalePrice, false)]
    [InlineData(PosRole.Cashier, UtangCapability.OverrideSalePriceUnlimited, false)]
    [InlineData(PosRole.StoreManager, UtangCapability.OverrideSalePrice, true)]
    [InlineData(PosRole.StoreManager, UtangCapability.OverrideSalePriceUnlimited, false)]
    [InlineData(PosRole.Owner, UtangCapability.OverrideSalePrice, true)]
    [InlineData(PosRole.Owner, UtangCapability.OverrideSalePriceUnlimited, true)]
    [InlineData(PosRole.Admin, UtangCapability.OverrideSalePriceUnlimited, true)]
    [InlineData(PosRole.InventoryStaff, UtangCapability.CreateSale, false)]
    [InlineData(PosRole.InventoryStaff, UtangCapability.ManageInventory, true)]
    [InlineData(PosRole.ReportingUser, UtangCapability.ViewReports, true)]
    [InlineData(PosRole.ReportingUser, UtangCapability.CreateSale, false)]
    public void Role_allows_expected_capabilities(PosRole role, UtangCapability capability, bool expected) =>
        Assert.Equal(expected, PosRoleMatrix.Allows(role, capability));

    [Theory]
    [InlineData(PosRole.Cashier, PosOperationalReportKind.SalesSummary, false)]
    [InlineData(PosRole.Cashier, PosOperationalReportKind.ShiftSummary, true)]
    [InlineData(PosRole.InventoryStaff, PosOperationalReportKind.InventoryStatus, true)]
    [InlineData(PosRole.InventoryStaff, PosOperationalReportKind.Expenses, false)]
    [InlineData(PosRole.InventoryStaff, PosOperationalReportKind.SupplierPayables, true)]
    [InlineData(PosRole.ReportingUser, PosOperationalReportKind.Overview, true)]
    [InlineData(PosRole.ReportingUser, PosOperationalReportKind.SupplierPayables, true)]
    [InlineData(PosRole.Cashier, PosOperationalReportKind.SupplierPayables, false)]
    public void Report_access_matches_matrix(PosRole role, PosOperationalReportKind kind, bool expected) =>
        Assert.Equal(expected, PosRoleMatrix.AllowsReport(role, kind));

    [Fact]
    public void Admin_cannot_assign_Owner() =>
        Assert.False(PosRoleMatrix.CanAssignRole(PosRole.Admin, PosRole.Owner));

    [Fact]
    public void Owner_can_assign_all_roles()
    {
        foreach (var role in Enum.GetValues<PosRole>())
        {
            Assert.True(PosRoleMatrix.CanAssignRole(PosRole.Owner, role));
        }
    }

    [Theory]
    [InlineData(true, UtangCapability.ViewDashboard, true)]
    [InlineData(true, UtangCapability.ManageCatalog, true)]
    [InlineData(true, UtangCapability.ManageInventory, true)]
    [InlineData(true, UtangCapability.ViewSales, true)]
    [InlineData(true, UtangCapability.ViewReports, true)]
    [InlineData(true, UtangCapability.ManageRegisters, true)]
    [InlineData(true, UtangCapability.CreateSale, false)]
    [InlineData(true, UtangCapability.EnterPos, false)]
    [InlineData(false, UtangCapability.ViewDashboard, true)]
    [InlineData(false, UtangCapability.ManageCatalog, true)]
    [InlineData(false, UtangCapability.ManagePermissions, false)]
    [InlineData(false, UtangCapability.CreateSale, false)]
    [InlineData(false, UtangCapability.EnterPos, false)]
    [InlineData(false, UtangCapability.OverrideSalePrice, false)]
    [InlineData(false, UtangCapability.OverrideSalePriceUnlimited, false)]
    [InlineData(true, UtangCapability.OverrideSalePrice, false)]
    [InlineData(true, UtangCapability.OverrideSalePriceUnlimited, false)]
    public void Organization_management_authority_excludes_checkout(
        bool isExactOwner,
        UtangCapability capability,
        bool expected) =>
        Assert.Equal(expected, PosRoleMatrix.AllowsOrganizationManagement(isExactOwner, capability));

    [Fact]
    public void Organization_owner_management_capabilities_never_include_create_sale()
    {
        var caps = PosRoleMatrix.OrganizationManagementCapabilities(isExactOwner: true);
        Assert.Contains(UtangCapability.ViewDashboard, caps);
        Assert.Contains(UtangCapability.ManageCatalog, caps);
        Assert.DoesNotContain(UtangCapability.CreateSale, caps);
        Assert.DoesNotContain(UtangCapability.EnterPos, caps);
    }
}
