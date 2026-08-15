using Xunit;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebResponsiveUxGuardTests
{
    [Fact]
    public void Shared_org_web_state_components_exist()
    {
        var root = FindRepoRoot();
        var shared = Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Shared");

        foreach (var name in new[]
                 {
                     "OrgAlert.razor",
                     "OrgLoading.razor",
                     "OrgEmpty.razor",
                     "OrgStatusBadge.razor",
                     "OrgSection.razor",
                     "OrgMetricCard.razor"
                 })
        {
            Assert.True(File.Exists(Path.Combine(shared, name)), name);
        }
    }

    [Fact]
    public void Management_pages_use_org_page_and_shared_states()
    {
        var root = FindRepoRoot();
        var pages = Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages");

        var files = Directory.GetFiles(pages, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("Login.razor", StringComparison.OrdinalIgnoreCase)
                        && !f.EndsWith("AccessDenied.razor", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(files.Count >= 25, $"Expected many management pages, found {files.Count}");

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var name = Path.GetFileName(file);
            Assert.Contains("org-page", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Location-specific operational state", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Official Receipt", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("custom RBAC designer", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("platform.permission.", text, StringComparison.OrdinalIgnoreCase);

            if (name is "Overview.razor" or "Branches.razor" or "Products.razor" or "StaffList.razor"
                or "SalesHistory.razor" or "Stock.razor" or "Devices.razor")
            {
                Assert.Contains("OrgLoading", text, StringComparison.Ordinal);
                Assert.Contains("OrgAlert", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Sales_history_uses_transaction_summary_terminology()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Reports",
            "SalesHistory.razor"));
        Assert.Contains("SalesHistory_ViewSummary", page, StringComparison.Ordinal);
        Assert.Contains("Transaction Summary", File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Localization",
            "OrgWebResources.resx")), StringComparison.Ordinal);
        Assert.DoesNotContain("Official Invoice", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mobile_drawer_covers_primary_management_routes()
    {
        var root = FindRepoRoot();
        var layout = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Layout",
            "MainLayout.razor"));

        Assert.Contains("org-mobile-drawer", layout, StringComparison.Ordinal);
        Assert.Contains("/organization/branches", layout, StringComparison.Ordinal);
        Assert.Contains("/operations/devices", layout, StringComparison.Ordinal);
        Assert.Contains("/inventory/transfers", layout, StringComparison.Ordinal);
        Assert.Contains("/reports/utang", layout, StringComparison.Ordinal);
        Assert.Contains("/organization/ownership-transfer", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Responsive_css_covers_breakpoints_and_theme_tokens()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "wwwroot",
            "org-web.css"));

        Assert.Contains("max-width: 767px", css, StringComparison.Ordinal);
        Assert.Contains("min-width: 1440px", css, StringComparison.Ordinal);
        Assert.Contains("max-width: 479px", css, StringComparison.Ordinal);
        Assert.Contains("var(--exits-", css, StringComparison.Ordinal);
        Assert.Contains("html.exits-theme-dark", css, StringComparison.Ordinal);
        Assert.Contains("org-table-wrap", css, StringComparison.Ordinal);
        Assert.DoesNotContain("background: #fff;", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_session_regression_guards_remain()
    {
        var root = FindRepoRoot();
        var handler = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "DevPlatformUserHeaderHandler.cs"));
        var webHost = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Services",
            "WebHostServices.cs"));

        Assert.Contains("PlatformSession", handler, StringComparison.Ordinal);
        Assert.Contains("IsPlatformApiPath", webHost, StringComparison.Ordinal);
        Assert.Contains("OrgWebSessionAmbient", webHost, StringComparison.Ordinal);
    }

    [Fact]
    public void Cashier_host_denial_and_role_gates_remain()
    {
        var root = FindRepoRoot();
        var shell = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Services",
            "WebHostServices.cs"));
        Assert.Contains("CanAccessOrganizationWeb", shell, StringComparison.Ordinal);
        Assert.Contains("IsCashierDenied", shell, StringComparison.Ordinal);
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
