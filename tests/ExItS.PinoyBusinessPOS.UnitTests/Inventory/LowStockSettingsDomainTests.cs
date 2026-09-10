using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class LowStockSettingsDomainTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Primary = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Product = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Actor = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void MonitoringModes_Derive_FromProductOverridePresence()
    {
        Assert.Equal(InventoryReorderMonitoringModes.BranchDefault, InventoryReorderMonitoringModes.Derive(null));

        var notMonitored = InventoryBranchReorderSetting.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            CatalogProductId.From(Product),
            null,
            null,
            UnitOfMeasure.Piece,
            Actor,
            DateTimeOffset.UtcNow);
        Assert.Equal(InventoryReorderMonitoringModes.NotMonitored, InventoryReorderMonitoringModes.Derive(notMonitored));

        var custom = InventoryBranchReorderSetting.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            CatalogProductId.From(Product),
            5m,
            10m,
            UnitOfMeasure.Piece,
            Actor,
            DateTimeOffset.UtcNow);
        Assert.Equal(InventoryReorderMonitoringModes.Custom, InventoryReorderMonitoringModes.Derive(custom));
    }

    [Fact]
    public void ResolveReorder_UsesProductOverride_ThenBranchDefault_ThenPrimaryAccount()
    {
        var context = new BranchInventoryContext(Org, Branch, Primary, OrganizationGovernance: true);
        var account = InventoryAccount.Rehydrate(
            InventoryAccountId.From(Product),
            PosOrganizationId.From(Org),
            CatalogProductId.From(Product),
            isTracked: true,
            reorderLevel: 99m,
            reorderQuantity: 50m,
            onHandQuantity: 20m,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var overrideSetting = InventoryBranchReorderSetting.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            CatalogProductId.From(Product),
            3m,
            6m,
            UnitOfMeasure.Piece,
            Actor,
            DateTimeOffset.UtcNow);
        var branchDefault = InventoryBranchReorderDefault.Create(
            PosOrganizationId.From(Org),
            PosBranchId.From(Branch),
            5m,
            10m,
            Actor,
            DateTimeOffset.UtcNow);

        var fromOverride = BranchInventoryReadService.ResolveReorderConfiguration(
            context,
            overrideSetting,
            account,
            branchDefault);
        Assert.Equal(3m, fromOverride.ReorderLevel);
        Assert.Equal(6m, fromOverride.ReorderQuantity);

        var fromDefault = BranchInventoryReadService.ResolveReorderConfiguration(
            context,
            null,
            account,
            branchDefault);
        Assert.Equal(5m, fromDefault.ReorderLevel);
        Assert.Equal(10m, fromDefault.ReorderQuantity);

        var primaryContext = new BranchInventoryContext(Org, Primary, Primary, OrganizationGovernance: true);
        var fromPrimary = BranchInventoryReadService.ResolveReorderConfiguration(
            primaryContext,
            null,
            account,
            null);
        Assert.Equal(99m, fromPrimary.ReorderLevel);
        Assert.Equal(50m, fromPrimary.ReorderQuantity);
    }

    [Fact]
    public void DerivedStockStatus_OutOfStock_TakesPriority_OverLowStock()
    {
        Assert.Equal(
            InventoryStockStatus.OutOfStock,
            InventoryStockStatuses.Derive(isTracked: true, onHand: 0m, reorderLevel: 10m));
        Assert.Equal(
            InventoryStockStatus.LowStock,
            InventoryStockStatuses.Derive(isTracked: true, onHand: 5m, reorderLevel: 10m));
        Assert.Equal(
            InventoryStockStatus.InStock,
            InventoryStockStatuses.Derive(isTracked: true, onHand: 11m, reorderLevel: 10m));
    }

    [Fact]
    public void NotMonitored_DoesNotSuggestReorder()
    {
        Assert.False(InventoryStockStatuses.IsReorderSuggested(2m, null));
        Assert.Null(InventoryStockStatuses.SuggestedOrderQuantity(2m, null, 24m));
    }
}
