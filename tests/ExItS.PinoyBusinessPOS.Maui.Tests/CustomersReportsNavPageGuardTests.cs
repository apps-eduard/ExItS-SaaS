namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CustomersReportsNavPageGuardTests
{
    [Fact]
    public void Customers_list_requires_view_history_and_does_not_restrict_view_only()
    {
        var list = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Customers",
            "CustomersList.razor"));
        Assert.Contains("UtangCapability.ViewCustomersAndHistory", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateCustomer", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Access_RestrictedTitle", list, StringComparison.Ordinal);

        var credit = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Customers",
            "CreditCreate.razor"));
        Assert.Contains("UtangCapability.CreateCredit", credit, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack", credit, StringComparison.Ordinal);
        Assert.Contains("Customers_Cancel", credit, StringComparison.Ordinal);
        Assert.DoesNotContain("Customers_BackToDetail", credit, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Customers",
            "CustomerDetail.razor"));
        Assert.Contains("StoreHeaderBack", detail, StringComparison.Ordinal);
        Assert.Contains("Href=\"/customers\"", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Customers_BackToList", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Customers_Back\"", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_hub_and_operational_pages_gate_by_capability()
    {
        var hub = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Reporting",
            "ReportsHub.razor"));
        Assert.Contains("UtangCapability.ViewReports", hub, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewShifts", hub, StringComparison.Ordinal);
        Assert.Contains("cash-variance", hub, StringComparison.Ordinal);
        Assert.Contains("pos-reports", hub, StringComparison.Ordinal);
        Assert.Contains("Reports_SearchPlaceholder", hub, StringComparison.Ordinal);

        var operational = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Reporting",
            "OperationalReportPage.razor"));
        Assert.Contains("CanAccessKind", operational, StringComparison.Ordinal);
        Assert.Contains("ViewInventory", operational, StringComparison.Ordinal);

        var more = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "MoreHub.razor"));
        Assert.Contains("UtangCapability.ViewReports", more, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewDashboard", more, StringComparison.Ordinal);
        Assert.Contains("OrganizationId", more, StringComparison.Ordinal);
        Assert.Contains("GoReports", more, StringComparison.Ordinal);
    }

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
