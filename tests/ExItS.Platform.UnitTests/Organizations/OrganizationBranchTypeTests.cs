using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationBranchTypeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_and_main_branch_default_to_retail()
    {
        var org = PlatformOrganizationId.New();
        var main = OrganizationBranch.CreateMainBranch(org, T0);
        var extra = OrganizationBranch.Create(org, "WH1", "Central Warehouse", T0);

        Assert.Equal(OrganizationBranchType.Retail, main.BranchType);
        Assert.True(main.AllowsRetailSales);
        Assert.Equal(OrganizationBranchType.Retail, extra.BranchType);
        Assert.True(extra.AllowsRetailSales);
    }

    [Fact]
    public void Warehouse_create_disables_customer_ordering_capabilities()
    {
        var branch = OrganizationBranch.Create(
            PlatformOrganizationId.New(),
            "WH1",
            "Central Warehouse",
            T0,
            branchType: OrganizationBranchType.Warehouse);

        Assert.Equal(OrganizationBranchType.Warehouse, branch.BranchType);
        Assert.False(branch.AllowsRetailSales);
        Assert.False(branch.CustomerOrderingEnabled);
        Assert.False(branch.PickupEnabled);
        Assert.False(branch.DeliveryEnabled);
    }

    [Fact]
    public void SetBranchType_to_warehouse_clears_storefront_flags()
    {
        var branch = OrganizationBranch.Create(
            PlatformOrganizationId.New(),
            "NORTH",
            "North",
            T0,
            latitude: 14.5m,
            longitude: 121.0m,
            pickupEnabled: true,
            deliveryEnabled: true,
            customerOrderingEnabled: true);

        branch.SetBranchType(OrganizationBranchType.Warehouse, T0.AddMinutes(1));

        Assert.Equal(OrganizationBranchType.Warehouse, branch.BranchType);
        Assert.False(branch.CustomerOrderingEnabled);
        Assert.False(branch.PickupEnabled);
        Assert.False(branch.DeliveryEnabled);
        Assert.False(branch.AllowsRetailSales);
    }

    [Fact]
    public void Warehouse_cannot_enable_customer_ordering()
    {
        var branch = OrganizationBranch.Create(
            PlatformOrganizationId.New(),
            "WH1",
            "Warehouse",
            T0,
            branchType: OrganizationBranchType.Warehouse);

        var ex = Assert.Throws<DomainException>(() =>
            branch.SetCustomerOrderingEnabled(true, T0.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.OrganizationBranchWarehouseCustomerOrderingForbidden, ex.ErrorCode);
    }
}
