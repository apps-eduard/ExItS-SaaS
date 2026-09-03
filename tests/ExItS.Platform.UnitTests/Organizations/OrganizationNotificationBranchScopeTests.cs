using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationNotificationBranchScopeTests
{
    private static readonly Guid Iloilo = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Cebu = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void SUPBRREQ10_branch_workspace_sees_own_plus_org_wide_only()
    {
        Assert.True(OrganizationNotificationBranchScope.IsVisible(Iloilo, Iloilo));
        Assert.False(OrganizationNotificationBranchScope.IsVisible(Iloilo, Cebu));
        Assert.True(OrganizationNotificationBranchScope.IsVisible(null, Cebu));
        Assert.True(OrganizationNotificationBranchScope.IsVisible(Iloilo, null));
    }

    [Fact]
    public void SUPBRREQ11_only_supplier_connection_requested_is_branch_targetable()
    {
        Assert.True(OrganizationBusinessNotificationTypes.IsBranchTargetable(
            SupplierConnectionNotificationTypes.Requested));
        Assert.False(OrganizationBusinessNotificationTypes.IsBranchTargetable(
            SupplierConnectionNotificationTypes.Accepted));
        Assert.False(OrganizationBusinessNotificationTypes.IsBranchTargetable(
            CustomerOrderNotificationTypes.Submitted));
        Assert.False(OrganizationBusinessNotificationTypes.IsBranchTargetable(
            ConnectedPurchaseOrderNotificationTypes.Submitted));
    }
}
