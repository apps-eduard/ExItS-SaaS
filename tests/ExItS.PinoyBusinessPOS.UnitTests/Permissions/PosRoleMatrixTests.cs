using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Permissions;

public sealed class PosRoleMatrixTests
{
    [Theory]
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
    [InlineData(PosRole.ReportingUser, PosOperationalReportKind.Overview, true)]
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
}
