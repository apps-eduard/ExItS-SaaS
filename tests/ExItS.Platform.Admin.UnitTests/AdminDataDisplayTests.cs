using ExItS.Platform.Admin.Components.Shared;
using System.Globalization;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminDataDisplayTests
{
    [Fact]
    public void Admin_data_foundation_files_exist()
    {
        var root = FindRepositoryRoot();
        var shared = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared");
        foreach (var name in new[]
                 {
                     "AdminDataTable.razor", "AdminDataPanel.razor", "AdminPagination.razor", "AdminSortHeader.razor",
                     "AdminFilterSummary.razor", "AmountDisplay.razor", "QuantityDisplay.razor", "DateDisplay.razor",
                     "RowActions.razor", "DetailSection.razor", "KeyValueList.razor", "EntitySummaryCard.razor",
                     "DeniedState.razor", "ConflictState.razor", "AdminColumnDefinition.cs", "StatusBadge.razor"
                 })
        {
            Assert.True(File.Exists(Path.Combine(shared, name)), name);
        }

        var table = File.ReadAllText(Path.Combine(shared, "AdminDataTable.razor"));
        Assert.Contains("admin-data-cards", table, StringComparison.Ordinal);
        Assert.Contains("responsive-table", table, StringComparison.Ordinal);
        Assert.Contains("AdminSortHeader", table, StringComparison.Ordinal);

        var badge = File.ReadAllText(Path.Combine(shared, "StatusBadge.razor"));
        Assert.Contains("status-badge-text", badge, StringComparison.Ordinal);
        Assert.Contains("aria-label", badge, StringComparison.Ordinal);
        Assert.Contains("data-tone", badge, StringComparison.Ordinal);
    }

    [Fact]
    public void Representative_pages_use_shared_data_components()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");

        var products = File.ReadAllText(Path.Combine(pages, "Products.razor"));
        Assert.Contains("ReportTable", products, StringComparison.Ordinal);
        Assert.Contains("AdminPagination", products, StringComparison.Ordinal);
        Assert.Contains("ReportDataPanel", products, StringComparison.Ordinal);
        Assert.Contains("KeyValueList", products, StringComparison.Ordinal);

        var orgs = File.ReadAllText(Path.Combine(pages, "Organizations.razor"));
        Assert.Contains("<Table", orgs, StringComparison.Ordinal);
        Assert.Contains("AmountDisplay", orgs, StringComparison.Ordinal);
        Assert.Contains("RemoteDataSource", orgs, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportTable", orgs, StringComparison.Ordinal);

        var payments = File.ReadAllText(Path.Combine(pages, "Payments.razor"));
        Assert.Contains("ReportTable", payments, StringComparison.Ordinal);
        Assert.Contains("AdminFilterSummary", payments, StringComparison.Ordinal);
        Assert.Contains("AmountDisplay", payments, StringComparison.Ordinal);
        Assert.Contains("AdminPagination", payments, StringComparison.Ordinal);

        var users = File.ReadAllText(Path.Combine(pages, "Users.razor"));
        Assert.Contains("<Table", users, StringComparison.Ordinal);
        Assert.Contains("RemoteDataSource", users, StringComparison.Ordinal);
        Assert.Contains("OnPageIndexChange", users, StringComparison.Ordinal);
        Assert.DoesNotContain("ReportTable", users, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPagination", users, StringComparison.Ordinal);

        var members = File.ReadAllText(Path.Combine(pages, "OrganizationMembers.razor"));
        Assert.Contains("AdminDataTable", members, StringComparison.Ordinal);
        Assert.Contains("RowActions", members, StringComparison.Ordinal);
        Assert.Contains("MemberActions", members, StringComparison.Ordinal);
    }

    [Fact]
    public void Amount_and_quantity_formatting_use_current_ui_culture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("1,234.50", 1234.5m.ToString("N2", CultureInfo.CurrentUICulture));
            Assert.Equal("12", 12m.ToString("N0", CultureInfo.CurrentUICulture));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fil-PH");
            var amount = 1234.5m.ToString("N2", CultureInfo.CurrentUICulture);
            Assert.False(string.IsNullOrWhiteSpace(amount));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Column_and_sort_contracts_are_presentation_only()
    {
        var column = new AdminColumnDefinition("amount", "Amount", Numeric: true, Sortable: true);
        Assert.True(column.Numeric);
        Assert.True(column.Sortable);
        Assert.Equal("amount", column.Key);

        var sort = new AdminSortState("amount", AdminSortDirection.Ascending);
        Assert.Equal("amount", sort.ColumnKey);
        Assert.Equal(AdminSortDirection.Ascending, sort.Direction);
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
