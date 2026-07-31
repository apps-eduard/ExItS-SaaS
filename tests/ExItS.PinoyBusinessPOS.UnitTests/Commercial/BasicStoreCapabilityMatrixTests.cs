using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.UnitTests.Commercial;

/// <summary>
/// P8-WP07 closeout: consolidated Basic Store <c>store-*</c> capability matrix across commercial states.
/// </summary>
public sealed class BasicStoreCapabilityMatrixTests
{
    private static readonly string[] AllStoreGrants =
    [
        PosFeatureCodes.StoreCatalogView,
        PosFeatureCodes.StoreCatalogManage,
        PosFeatureCodes.StoreSalesView,
        PosFeatureCodes.StoreSalesCreate,
        PosFeatureCodes.StoreSalesVoid,
        PosFeatureCodes.StoreInventoryView,
        PosFeatureCodes.StoreInventoryManage,
        PosFeatureCodes.StoreExpensesView,
        PosFeatureCodes.StoreExpensesManage,
        PosFeatureCodes.StoreSuppliersView,
        PosFeatureCodes.StoreSuppliersManage,
        PosFeatureCodes.StorePurchasingView,
        PosFeatureCodes.StorePurchasingManage,
        PosFeatureCodes.StoreShiftsView,
        PosFeatureCodes.StoreShiftsManage,
        PosFeatureCodes.StoreDashboardView,
        PosFeatureCodes.StoreReportsView
    ];

    public static TheoryData<string> FullStates { get; } = new(
        PosSubscriptionStatuses.Trialing,
        PosSubscriptionStatuses.Active,
        PosSubscriptionStatuses.GracePeriod);

    public static TheoryData<string> ContinuityStates { get; } = new(
        PosSubscriptionStatuses.PastDue,
        PosSubscriptionStatuses.Cancelled,
        PosSubscriptionStatuses.Expired);

    public static TheoryData<string?> FailClosedStates { get; } = new(
        PosSubscriptionStatuses.Suspended,
        null,
        "",
        "Unknown",
        "Stale");

    [Theory]
    [MemberData(nameof(FullStates))]
    public void Full_states_allow_all_store_view_and_manage_when_granted(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCatalog, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageCatalog, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSales, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateSale, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.VoidSale, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewInventory, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageInventory, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewExpenses, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageExpenses, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSuppliers, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageSuppliers, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewPurchasing, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManagePurchasing, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewShifts, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageShifts, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewDashboard, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewReports, status, AllStoreGrants));
    }

    [Theory]
    [MemberData(nameof(ContinuityStates))]
    public void Continuity_states_allow_store_views_deny_mutations(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCatalog, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSales, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewInventory, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewExpenses, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSuppliers, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewPurchasing, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewShifts, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewDashboard, status, AllStoreGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewReports, status, AllStoreGrants));

        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageCatalog, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateSale, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.VoidSale, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageInventory, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageExpenses, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageSuppliers, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManagePurchasing, status, AllStoreGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageShifts, status, AllStoreGrants));
    }

    [Theory]
    [MemberData(nameof(FailClosedStates))]
    public void Suspended_missing_stale_or_unknown_deny_every_store_capability(string? status)
    {
        foreach (var capability in new[]
                 {
                     UtangCapability.ViewCatalog,
                     UtangCapability.ManageCatalog,
                     UtangCapability.ViewSales,
                     UtangCapability.CreateSale,
                     UtangCapability.VoidSale,
                     UtangCapability.ViewInventory,
                     UtangCapability.ManageInventory,
                     UtangCapability.ViewExpenses,
                     UtangCapability.ManageExpenses,
                     UtangCapability.ViewSuppliers,
                     UtangCapability.ManageSuppliers,
                     UtangCapability.ViewPurchasing,
                     UtangCapability.ManagePurchasing,
                     UtangCapability.ViewShifts,
                     UtangCapability.ManageShifts,
                     UtangCapability.ViewDashboard,
                     UtangCapability.ViewReports,
                     UtangCapability.EnterPos
                 })
        {
            Assert.False(UtangCapabilityPolicy.IsAllowed(capability, status, AllStoreGrants));
        }
    }

    [Theory]
    [InlineData(PosFeatureCodes.StoreCatalogView)]
    [InlineData(PosFeatureCodes.StoreSalesView)]
    [InlineData(PosFeatureCodes.StoreInventoryView)]
    [InlineData(PosFeatureCodes.StoreExpensesView)]
    [InlineData(PosFeatureCodes.StoreSuppliersView)]
    [InlineData(PosFeatureCodes.StorePurchasingView)]
    [InlineData(PosFeatureCodes.StoreShiftsView)]
    [InlineData(PosFeatureCodes.StoreDashboardView)]
    [InlineData(PosFeatureCodes.StoreReportsView)]
    [InlineData(PosFeatureCodes.StoreCatalogManage)]
    public void Continuity_entry_allowed_with_each_approved_store_grant(string grant)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(PosSubscriptionStatuses.PastDue, [grant]));
    }

    [Theory]
    [InlineData(PosFeatureCodes.StoreSalesCreate)]
    [InlineData(PosFeatureCodes.StoreSalesVoid)]
    [InlineData(PosFeatureCodes.StoreInventoryManage)]
    [InlineData(PosFeatureCodes.StoreExpensesManage)]
    public void Continuity_entry_denied_when_only_mutation_grants_present(string grant)
    {
        Assert.False(UtangCapabilityPolicy.CanEnter(PosSubscriptionStatuses.PastDue, [grant]));
    }

    [Fact]
    public void Product_based_utang_requires_both_sale_create_and_credit_create_grants()
    {
        // Checkout dual-gate: API requires CreateSale AND CreateCredit. Policy evaluates each alone.
        Assert.True(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateSale,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreSalesCreate]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateCredit,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreSalesCreate]));
        Assert.True(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateCredit,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.CustomerCreditCreate]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateSale,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.CustomerCreditCreate]));
    }

    [Fact]
    public void Development_grants_include_every_phase8_store_feature_code()
    {
        foreach (var code in AllStoreGrants)
        {
            Assert.Contains(code, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        }
    }

    [Fact]
    public void Store_feature_code_constants_use_stable_kebab_case_values()
    {
        Assert.Equal("store-catalog-view", PosFeatureCodes.StoreCatalogView);
        Assert.Equal("store-catalog-manage", PosFeatureCodes.StoreCatalogManage);
        Assert.Equal("store-sales-view", PosFeatureCodes.StoreSalesView);
        Assert.Equal("store-sales-create", PosFeatureCodes.StoreSalesCreate);
        Assert.Equal("store-sales-void", PosFeatureCodes.StoreSalesVoid);
        Assert.Equal("store-inventory-view", PosFeatureCodes.StoreInventoryView);
        Assert.Equal("store-inventory-manage", PosFeatureCodes.StoreInventoryManage);
        Assert.Equal("store-expenses-view", PosFeatureCodes.StoreExpensesView);
        Assert.Equal("store-expenses-manage", PosFeatureCodes.StoreExpensesManage);
        Assert.Equal("store-suppliers-view", PosFeatureCodes.StoreSuppliersView);
        Assert.Equal("store-suppliers-manage", PosFeatureCodes.StoreSuppliersManage);
        Assert.Equal("store-purchasing-view", PosFeatureCodes.StorePurchasingView);
        Assert.Equal("store-purchasing-manage", PosFeatureCodes.StorePurchasingManage);
        Assert.Equal("store-shifts-view", PosFeatureCodes.StoreShiftsView);
        Assert.Equal("store-shifts-manage", PosFeatureCodes.StoreShiftsManage);
        Assert.Equal("store-dashboard-view", PosFeatureCodes.StoreDashboardView);
        Assert.Equal("store-reports-view", PosFeatureCodes.StoreReportsView);
    }
}
