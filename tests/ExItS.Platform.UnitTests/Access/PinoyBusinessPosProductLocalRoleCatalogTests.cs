using ExItS.Platform.Application.Access;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Access;

public sealed class PinoyBusinessPosProductLocalRoleCatalogTests
{
    [Fact]
    public void BuildDefinitions_includes_all_five_assignable_pos_roles()
    {
        var definitions = PinoyBusinessPosProductLocalRoleCatalog.BuildDefinitions();

        Assert.Equal(5, definitions.Count);
        Assert.Equal(ProductLocalRoleCodes.Owner, definitions[0].Code);
        Assert.Equal(ProductLocalRoleCodes.Manager, definitions[1].Code);
        Assert.Equal(ProductLocalRoleCodes.Cashier, definitions[2].Code);
        Assert.Equal(ProductLocalRoleCodes.InventoryStaff, definitions[3].Code);
        Assert.Equal(ProductLocalRoleCodes.ReportingUser, definitions[4].Code);
    }

    [Fact]
    public void BuildDefinitions_marks_pos_owner_distinct_from_organization_owner_in_copy()
    {
        var owner = PinoyBusinessPosProductLocalRoleCatalog.BuildDefinitions()
            .Single(d => d.Code == ProductLocalRoleCodes.Owner);

        Assert.Equal(ProductRoleDisplay.PosOwner, owner.DisplayName);
        Assert.Contains("does not transfer ownership", owner.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoleAllowsPermission_cashier_can_sell_but_not_write_off_debt()
    {
        Assert.True(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.Cashier, "sell.products"));
        Assert.False(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.Cashier, "customers.write_off"));
    }

    [Fact]
    public void RoleAllowsPermission_inventory_staff_has_inventory_and_purchasing_writes()
    {
        Assert.True(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.InventoryStaff, "inventory.adjust"));
        Assert.True(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.InventoryStaff, "purchasing.create"));
        Assert.False(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.InventoryStaff, "sell.products"));
    }

    [Fact]
    public void RoleAllowsPermission_reporting_user_is_read_only_for_operational_writes()
    {
        Assert.True(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.ReportingUser, "reports.sales"));
        Assert.False(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.ReportingUser, "inventory.adjust"));
        Assert.False(PinoyBusinessPosProductLocalRoleCatalog.RoleAllowsPermission(
            ProductLocalRoleCodes.ReportingUser, "sell.products"));
    }

    [Fact]
    public void BuildDefinitions_includes_permission_groups_for_each_role()
    {
        var manager = PinoyBusinessPosProductLocalRoleCatalog.BuildDefinitions()
            .Single(d => d.Code == ProductLocalRoleCodes.Manager);

        Assert.NotEmpty(manager.PermissionGroups);
        Assert.Contains(manager.PermissionGroups, g => g.Code == "selling");
        Assert.Contains(manager.PermissionGroups, g => g.Code == "reports");
    }
}
