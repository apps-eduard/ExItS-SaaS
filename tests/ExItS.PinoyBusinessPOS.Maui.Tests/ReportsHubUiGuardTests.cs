namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ReportsHubUiGuardTests
{
    [Fact]
    public void Reports_hub_is_compact_grouped_and_searchable()
    {
        var hub = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Reporting", "ReportsHub.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("@page \"/reports\"", hub, StringComparison.Ordinal);
        Assert.Contains("pos-reports", hub, StringComparison.Ordinal);
        Assert.Contains("PageHeader", hub, StringComparison.Ordinal);
        Assert.Contains("pos-reports__actions", hub, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"sales\")", hub, StringComparison.Ordinal);
        Assert.Contains("pos-reports__row-icon", hub, StringComparison.Ordinal);
        Assert.Contains("Reports_SearchPlaceholder", hub, StringComparison.Ordinal);
        Assert.Contains("Reports_OperationalSection", hub, StringComparison.Ordinal);
        Assert.Contains("Reports_LegacyMenuTitle", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/operational/overview", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/operational/cash-variance", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/sales", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/utang", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/inventory", hub, StringComparison.Ordinal);
        Assert.Contains("/reports/expenses", hub, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewReports", hub, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewShifts", hub, StringComparison.Ordinal);
        Assert.Contains("pos-reports__footnote", hub, StringComparison.Ordinal);
        Assert.Contains("GoDashboard", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-reports__dashboard", hub, StringComparison.Ordinal);

        Assert.DoesNotContain("ButtonVariant.Primary", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("Alert Tone", hub, StringComparison.Ordinal);

        Assert.Contains(".pos-reports__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-reports__search", css, StringComparison.Ordinal);
        Assert.Contains(".pos-reports__row-icon", css, StringComparison.Ordinal);
        Assert.Contains(".pos-report__actions", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dashboard__stats", css, StringComparison.Ordinal);
        Assert.Contains(".pos-dashboard__actions", css, StringComparison.Ordinal);

        var dashboard = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Reporting", "DashboardPage.razor"));
        Assert.Contains("@page \"/dashboard\"", dashboard, StringComparison.Ordinal);
        Assert.Contains("pos-dashboard", dashboard, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--two", dashboard, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", dashboard, StringComparison.Ordinal);
        Assert.Contains("pos-dashboard__stats", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("<Actions>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", dashboard, StringComparison.Ordinal);

        var operational = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Reporting", "OperationalReportPage.razor"));
        Assert.Contains("pos-report__actions", operational, StringComparison.Ordinal);
        Assert.Contains("<StoreHeaderBack Href=\"/reports\" />", operational, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", operational, StringComparison.Ordinal);
        Assert.DoesNotContain("IconGlyphs.Get(\"back\")", operational, StringComparison.Ordinal);
        Assert.DoesNotContain("<Actions>", operational, StringComparison.Ordinal);

        foreach (var page in new[] { "SalesReportPage.razor", "UtangReportPage.razor", "InventoryReportPage.razor", "ExpensesReportPage.razor" })
        {
            var text = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Reporting", page));
            Assert.Contains("pos-report__actions", text, StringComparison.Ordinal);
            Assert.Contains("<StoreHeaderBack Href=\"/reports\" />", text, StringComparison.Ordinal);
            Assert.Contains("IconGlyphs.Get(\"refresh\")", text, StringComparison.Ordinal);
            Assert.Contains("pos-dashboard__stats", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IconGlyphs.Get(\"back\")", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<Actions>", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Auth_Logout", text, StringComparison.Ordinal);
        }

        foreach (var key in new[]
                 {
                     "Reports_SearchPlaceholder", "Reports_OperationalSection", "Reports_ClassicHint",
                     "Reports_GroupSales", "Reports_NoMatches"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate PinoyBusinessPOS.Maui project.");
    }
}
