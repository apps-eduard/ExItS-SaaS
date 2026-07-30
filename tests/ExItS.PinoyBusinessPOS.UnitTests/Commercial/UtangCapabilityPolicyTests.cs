using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.UnitTests.Commercial;

public sealed class UtangCapabilityPolicyTests
{
    private static readonly string[] FullGrants =
    [
        PosFeatureCodes.CustomerCreditView,
        PosFeatureCodes.CustomerCreditRepay,
        PosFeatureCodes.CustomerCreditCreate
    ];

    private static readonly string[] ContinuityGrants =
    [
        PosFeatureCodes.CustomerCreditView,
        PosFeatureCodes.CustomerCreditRepay
    ];

    private static readonly string[] ViewOnly = [PosFeatureCodes.CustomerCreditView];

    private static readonly string[] CatalogGrants =
    [
        PosFeatureCodes.StoreCatalogView,
        PosFeatureCodes.StoreCatalogManage
    ];

    private static readonly string[] SalesGrants =
    [
        PosFeatureCodes.StoreSalesView,
        PosFeatureCodes.StoreSalesCreate,
        PosFeatureCodes.StoreSalesVoid
    ];

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_mutations_when_grants_present(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCustomer, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.EditCustomer, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCredit, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.MutateDueDate, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseRepayment, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.RecordRepayment, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseCredit, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateStatement, status, FullGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateReceipt, status, FullGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_view_repay_credit_reverse_deny_mutations(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.RecordRepayment, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseCredit, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateStatement, status, ContinuityGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewGenerateReceipt, status, ContinuityGrants));

        // OD-07 / OD-08 / OD-09 repayment reverse / due-date / create credit
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCustomer, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.EditCustomer, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateCredit, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ReverseRepayment, status, ContinuityGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.MutateDueDate, status, ContinuityGrants));
    }

    [Fact]
    public void Suspended_denies_all_capabilities()
    {
        foreach (UtangCapability capability in Enum.GetValues<UtangCapability>())
        {
            Assert.False(UtangCapabilityPolicy.IsAllowed(capability, PosSubscriptionStatuses.Suspended, FullGrants));
        }
    }

    [Fact]
    public void Missing_or_unknown_status_denies()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, null, FullGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCustomersAndHistory, "Unknown", FullGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.EnterPos, "", ViewOnly));
    }

    [Fact]
    public void Continuity_entry_requires_view_or_repay_not_create_alone()
    {
        Assert.False(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.CustomerCreditCreate]));
        Assert.True(UtangCapabilityPolicy.CanEnter(PosSubscriptionStatuses.Expired, ViewOnly));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_catalog_view_and_manage_with_catalog_grants(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCatalog, status, CatalogGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageCatalog, status, CatalogGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_catalog_view_but_deny_catalog_manage(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewCatalog, status, CatalogGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageCatalog, status, CatalogGrants));
    }

    [Fact]
    public void Continuity_entry_is_granted_by_either_catalog_code()
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.StoreCatalogView]));
        Assert.True(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.StoreCatalogManage]));
    }

    [Fact]
    public void Catalog_capabilities_require_their_own_grants()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewCatalog,
            PosSubscriptionStatuses.Active,
            FullGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ManageCatalog,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreCatalogView]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateCredit,
            PosSubscriptionStatuses.Active,
            CatalogGrants));
    }

    [Fact]
    public void Development_grants_include_catalog_codes()
    {
        Assert.Contains(PosFeatureCodes.StoreCatalogView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreCatalogManage, UtangCapabilityPolicy.DefaultDevelopmentGrants);
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_sales_view_create_and_void_with_sales_grants(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSales, status, SalesGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateSale, status, SalesGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.VoidSale, status, SalesGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_sales_view_but_deny_create_and_void(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewSales, status, SalesGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.CreateSale, status, SalesGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.VoidSale, status, SalesGrants));
    }

    [Fact]
    public void Continuity_entry_is_granted_by_sales_view_code()
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.StoreSalesView]));
        Assert.False(UtangCapabilityPolicy.CanEnter(
            PosSubscriptionStatuses.Expired,
            [PosFeatureCodes.StoreSalesCreate]));
    }

    [Fact]
    public void Sales_capabilities_require_their_own_grants()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewSales,
            PosSubscriptionStatuses.Active,
            CatalogGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateSale,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreSalesView]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.VoidSale,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreSalesCreate]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ManageCatalog,
            PosSubscriptionStatuses.Active,
            SalesGrants));
    }

    [Fact]
    public void Development_grants_include_sales_codes()
    {
        Assert.Contains(PosFeatureCodes.StoreSalesView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreSalesCreate, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreSalesVoid, UtangCapabilityPolicy.DefaultDevelopmentGrants);
    }

    private static readonly string[] InventoryGrants =
    [
        PosFeatureCodes.StoreInventoryView,
        PosFeatureCodes.StoreInventoryManage
    ];

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_inventory_view_and_manage(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewInventory, status, InventoryGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageInventory, status, InventoryGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_inventory_view_deny_manage(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, InventoryGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewInventory, status, InventoryGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageInventory, status, InventoryGrants));
    }

    [Fact]
    public void Development_grants_include_inventory_codes()
    {
        Assert.Contains(PosFeatureCodes.StoreInventoryView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreInventoryManage, UtangCapabilityPolicy.DefaultDevelopmentGrants);
    }

    private static readonly string[] ExpenseGrants =
    [
        PosFeatureCodes.StoreExpensesView,
        PosFeatureCodes.StoreExpensesManage
    ];

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    public void Full_states_allow_expense_view_and_manage(string status)
    {
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewExpenses, status, ExpenseGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageExpenses, status, ExpenseGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_states_allow_expense_view_deny_manage(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, ExpenseGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewExpenses, status, ExpenseGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ManageExpenses, status, ExpenseGrants));
    }

    [Fact]
    public void Development_grants_include_expense_codes()
    {
        Assert.Contains(PosFeatureCodes.StoreExpensesView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreExpensesManage, UtangCapabilityPolicy.DefaultDevelopmentGrants);
    }

    private static readonly string[] ReportingGrants =
    [
        PosFeatureCodes.StoreDashboardView,
        PosFeatureCodes.StoreReportsView
    ];

    [Theory]
    [InlineData(PosSubscriptionStatuses.Trialing)]
    [InlineData(PosSubscriptionStatuses.Active)]
    [InlineData(PosSubscriptionStatuses.GracePeriod)]
    [InlineData(PosSubscriptionStatuses.PastDue)]
    [InlineData(PosSubscriptionStatuses.Cancelled)]
    [InlineData(PosSubscriptionStatuses.Expired)]
    public void Continuity_and_full_states_allow_dashboard_and_reports_when_granted(string status)
    {
        Assert.True(UtangCapabilityPolicy.CanEnter(status, ReportingGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewDashboard, status, ReportingGrants));
        Assert.True(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewReports, status, ReportingGrants));
    }

    [Theory]
    [InlineData(PosSubscriptionStatuses.Suspended)]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void Suspended_missing_or_unknown_deny_dashboard_and_reports(string? status)
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewDashboard, status, ReportingGrants));
        Assert.False(UtangCapabilityPolicy.IsAllowed(UtangCapability.ViewReports, status, ReportingGrants));
    }

    [Fact]
    public void Development_grants_include_dashboard_and_report_codes()
    {
        Assert.Contains(PosFeatureCodes.StoreDashboardView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
        Assert.Contains(PosFeatureCodes.StoreReportsView, UtangCapabilityPolicy.DefaultDevelopmentGrants);
    }

    [Fact]
    public void Feature_grants_required_even_in_active()
    {
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.CreateCredit,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.RecordRepayment,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewInventory,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ManageInventory,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreInventoryView]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewExpenses,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ManageExpenses,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreExpensesView]));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewDashboard,
            PosSubscriptionStatuses.Active,
            ViewOnly));
        Assert.False(UtangCapabilityPolicy.IsAllowed(
            UtangCapability.ViewReports,
            PosSubscriptionStatuses.Active,
            [PosFeatureCodes.StoreDashboardView]));
    }
}
