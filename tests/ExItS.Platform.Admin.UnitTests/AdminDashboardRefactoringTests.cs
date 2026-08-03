using ExItS.Platform.Admin.Components.Shared.Reporting;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminDashboardRefactoringTests
{
    [Fact]
    public void Dashboard_uses_antdesign_landing_composition()
    {
        var root = FindRepositoryRoot();
        var dashboard = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "AdminDashboard.razor"));

        Assert.Contains("Dashboard_Section_Primary", dashboard, StringComparison.Ordinal);
        Assert.Contains("Dashboard_Section_Lifecycle", dashboard, StringComparison.Ordinal);
        Assert.Contains("Dashboard_Section_Operations", dashboard, StringComparison.Ordinal);
        Assert.Contains("<Statistic", dashboard, StringComparison.Ordinal);
        Assert.Contains("<PageHeader", dashboard, StringComparison.Ordinal);
        Assert.Contains("GetPortfolioSummaryAsync", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportPageShell", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportKpiCard", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("forecast", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("profit", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trend\"", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">trend<", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", dashboard, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrated_foundation_pages_use_antdesign_tables_remaining_lists_keep_report_shell()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");

        foreach (var (file, markers) in new (string, string[])[]
                 {
                     ("Users.razor", ["<Table", "RemoteDataSource", "GetUsersAsync", "OnChange"]),
                     ("Organizations.razor", ["<Table", "RemoteDataSource", "GetOrganizationsAsync", "OnChange"]),
                     ("Subscriptions.razor", ["<Table", "RemoteDataSource", "GetSubscriptionsAsync", "OnChange"]),
                     ("Entitlements.razor", ["<Table", "RemoteDataSource", "GetLatestEntitlementsAsync", "OnChange"]),
                     ("Audit.razor", ["ReportPageShell", "ReportFilterBar", "ReportTable", "GetAuditRecordsAsync"]),
                     ("Products.razor", ["<Table", "RemoteDataSource", "GetProductsAsync", "OnChange"]),
                     ("Payments.razor", ["admin-elevated-card", "Drawer", "GetPaymentsAsync", "<Table"]),
                 })
        {
            var text = File.ReadAllText(Path.Combine(pages, file));
            foreach (var marker in markers)
            {
                Assert.Contains(marker, text, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("Sum(", text, StringComparison.Ordinal);
        }

        var users = File.ReadAllText(Path.Combine(pages, "Users.razor"));
        Assert.DoesNotContain("ReportPageShell", users, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportTable", users, StringComparison.Ordinal);
        Assert.DoesNotContain("Fluent", users, StringComparison.OrdinalIgnoreCase);

        var orgs = File.ReadAllText(Path.Combine(pages, "Organizations.razor"));
        Assert.DoesNotContain("ReportPageShell", orgs, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportTable", orgs, StringComparison.Ordinal);

        var entitlements = File.ReadAllText(Path.Combine(pages, "Entitlements.razor"));
        Assert.DoesNotContain("ReportPageShell", entitlements, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportTable", entitlements, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_localization_keys_exist_en_and_fil()
    {
        var root = FindRepositoryRoot();
        var loc = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "AdminResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "AdminResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Dashboard_Section_Primary", "Dashboard_Section_Lifecycle", "Dashboard_Section_Operations",
                     "Dashboard_Link_AllSubscriptions", "Dashboard_Link_PendingPayments", "Dashboard_Support_Catalog"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Kpi_cards_remain_non_clickable_by_default()
    {
        var root = FindRepositoryRoot();
        var kpi = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "Reporting", "ReportKpiCard.razor"));
        Assert.DoesNotContain("is-clickable", kpi, StringComparison.Ordinal);
        Assert.DoesNotContain("href", kpi, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Emphasis", kpi, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
