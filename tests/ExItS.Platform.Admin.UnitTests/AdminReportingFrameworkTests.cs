using ExItS.Platform.Admin.Components.Shared.Reporting;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminReportingFrameworkTests
{
    [Fact]
    public void Reporting_framework_files_exist()
    {
        var root = FindRepositoryRoot();
        var reporting = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "Reporting");
        foreach (var name in new[]
                 {
                     "ReportPageShell.razor", "ReportHeader.razor", "ReportFilterBar.razor",
                     "ReportDateRangeFilter.razor", "ReportQuickRangeSelector.razor", "ReportQuickRangeHelper.cs",
                     "ReportKpiGrid.razor", "ReportKpiCard.razor", "ReportSummaryCard.razor",
                     "ReportSection.razor", "ReportTable.razor", "ReportTotalsRow.razor", "ReportGroupHeader.razor",
                     "ReportStatusBadge.razor", "ReportLoadingState.razor", "ReportEmptyState.razor",
                     "ReportErrorState.razor", "ReportAccessDeniedState.razor", "ReportConflictState.razor",
                     "ReportDataPanel.razor", "ResponsiveReportLayout.razor"
                 })
        {
            Assert.True(File.Exists(Path.Combine(reporting, name)), name);
        }

        var table = File.ReadAllText(Path.Combine(reporting, "ReportTable.razor"));
        Assert.Contains("AdminDataTable", table, StringComparison.Ordinal);

        var panel = File.ReadAllText(Path.Combine(reporting, "ReportDataPanel.razor"));
        Assert.Contains("AdminDataPanel", panel, StringComparison.Ordinal);

        var badge = File.ReadAllText(Path.Combine(reporting, "ReportStatusBadge.razor"));
        Assert.Contains("StatusBadge", badge, StringComparison.Ordinal);

        var kpi = File.ReadAllText(Path.Combine(reporting, "ReportKpiCard.razor"));
        Assert.DoesNotContain("onclick", kpi, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", kpi, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_pages_use_reporting_framework()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");

        var dashboard = File.ReadAllText(Path.Combine(pages, "AdminDashboard.razor"));
        Assert.Contains("<Statistic", dashboard, StringComparison.Ordinal);
        Assert.Contains("<PageHeader", dashboard, StringComparison.Ordinal);
        Assert.Contains("<Spin", dashboard, StringComparison.Ordinal);
        Assert.Contains("ResultStatus.Error", dashboard, StringComparison.Ordinal);
        Assert.Contains("GetPortfolioSummaryAsync", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportPageShell", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportKpiGrid", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("profit", dashboard, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COGS", dashboard, StringComparison.OrdinalIgnoreCase);

        var payments = File.ReadAllText(Path.Combine(pages, "Payments.razor"));
        Assert.Contains("ReportPageShell", payments, StringComparison.Ordinal);
        Assert.Contains("ReportFilterBar", payments, StringComparison.Ordinal);
        Assert.Contains("ReportDateRangeFilter", payments, StringComparison.Ordinal);
        Assert.Contains("ReportQuickRangeSelector", payments, StringComparison.Ordinal);
        Assert.Contains("ReportTable", payments, StringComparison.Ordinal);
        Assert.Contains("ReportTotalsRow", payments, StringComparison.Ordinal);
        Assert.Contains("ReportGroupHeader", payments, StringComparison.Ordinal);
        Assert.Contains("ReportDataPanel", payments, StringComparison.Ordinal);
        Assert.Contains("AdminPagination", payments, StringComparison.Ordinal);
        Assert.Contains("ReportQuickRangeHelper.IsWithinMaxSpan", payments, StringComparison.Ordinal);
        Assert.Contains("GetPaymentsAsync", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("Sum(", payments, StringComparison.Ordinal);
        Assert.DoesNotContain("PaidAtUtc.UtcDateTime", payments, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ReportQuickRange.Today, 0, 0)]
    [InlineData(ReportQuickRange.Last7Days, -6, 0)]
    [InlineData(ReportQuickRange.Last30Days, -29, 0)]
    public void Quick_range_helper_resolves_inclusive_calendar_dates(ReportQuickRange range, int fromOffset, int toOffset)
    {
        var today = new DateOnly(2026, 7, 29);
        var (from, to) = ReportQuickRangeHelper.Resolve(range, today);
        Assert.Equal(today.AddDays(fromOffset), from);
        Assert.Equal(today.AddDays(toOffset), to);
    }

    [Fact]
    public void Quick_range_this_month_starts_on_first_calendar_day()
    {
        var today = new DateOnly(2026, 7, 29);
        var (from, to) = ReportQuickRangeHelper.Resolve(ReportQuickRange.ThisMonth, today);
        Assert.Equal(new DateOnly(2026, 7, 1), from);
        Assert.Equal(today, to);
    }

    [Fact]
    public void Date_span_bounds_are_inclusive_and_do_not_invent_totals()
    {
        var from = new DateOnly(2026, 1, 1);
        Assert.True(ReportQuickRangeHelper.IsWithinMaxSpan(from, from, 1));
        Assert.True(ReportQuickRangeHelper.IsWithinMaxSpan(from, from.AddDays(365), 366));
        Assert.False(ReportQuickRangeHelper.IsWithinMaxSpan(from, from.AddDays(366), 366));
        Assert.False(ReportQuickRangeHelper.IsWithinMaxSpan(from.AddDays(1), from, 366));
    }

    [Fact]
    public void Report_localization_keys_exist_in_english_and_filipino()
    {
        var root = FindRepositoryRoot();
        var loc = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "AdminResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "AdminResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Report_FiltersAria", "Report_FromDate", "Report_ToDate", "Report_QuickRangesAria",
                     "Report_Quick_Today", "Report_Quick_Last7", "Report_Quick_Last30", "Report_Quick_ThisMonth",
                     "Report_DateRangeInvalid", "Report_DateRangeTooLarge", "Report_PaidRangeHelp",
                     "Report_PaymentsSection", "Report_PaymentsTotals", "Report_PageResults"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void App_css_includes_antdesign_branding_and_residual_report_hooks_without_tailwind()
    {
        var root = FindRepositoryRoot();
        var css = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "wwwroot", "app.css"));
        Assert.Contains("exits-admin-layout", css, StringComparison.Ordinal);
        Assert.Contains("exits-login", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@tailwind", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tailwindcss", css, StringComparison.OrdinalIgnoreCase);

        var csproj = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj"));
        Assert.Contains("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentUI", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shadcn", csproj, StringComparison.OrdinalIgnoreCase);
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
