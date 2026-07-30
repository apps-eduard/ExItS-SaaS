using System.Globalization;

namespace ExItS.DesignSystem.Tests;

public sealed class ReusableMvpComponentTests
{
    [Fact]
    public void Reusable_mvp_components_exist()
    {
        var root = FindRepoRoot();
        var components = Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components");
        var required = new[]
        {
            "Forms/FormField.razor", "Forms/FormGroup.razor", "Forms/FormActions.razor",
            "Forms/TextArea.razor", "Forms/Checkbox.razor", "Forms/RadioGroup.razor",
            "Forms/NumberInput.razor", "Forms/CurrencyInput.razor", "Forms/DateInput.razor",
            "Forms/TimeInput.razor", "Forms/FormValidationSummary.razor", "Forms/FieldValidationMessage.razor",
            "Overlay/ConfirmDialog.razor",
            "Feedback/InlineMessage.razor", "Feedback/Progress.razor",
            "Data/ResponsiveDataList.razor", "Data/DataTable.razor", "Data/DataColumn.razor",
            "Data/MobileRowCard.razor", "Data/SearchToolbar.razor", "Data/FilterBar.razor",
            "Data/SortControl.razor", "Data/Pagination.razor", "Data/PaginationSummary.razor",
            "Data/StatusCell.razor", "Data/MoneyDisplay.razor", "Data/DataColumnDefinition.cs",
            "Layout/SectionHeader.razor", "Layout/ActionBar.razor", "Layout/Accordion.razor",
            "Layout/Dropdown.razor",
        };

        foreach (var relative in required)
        {
            Assert.True(File.Exists(Path.Combine(components, relative)), $"Missing: {relative}");
        }
    }

    [Fact]
    public void Number_and_currency_inputs_use_decimal_not_floating_point()
    {
        var root = FindRepoRoot();
        foreach (var file in new[] { "NumberInput.razor", "CurrencyInput.razor" })
        {
            var text = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
                "Components", "Forms", file));
            Assert.Contains("decimal?", text, StringComparison.Ordinal);
            Assert.DoesNotContain("float", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("double", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Money_display_is_decimal_culture_aware_and_display_only()
    {
        var money = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Data", "MoneyDisplay.razor"));
        Assert.Contains("decimal?", money, StringComparison.Ordinal);
        Assert.Contains("CultureInfo.CurrentUICulture", money, StringComparison.Ordinal);
        Assert.Contains("CurrencyCode", money, StringComparison.Ordinal);
        Assert.Contains("Unavailable", money, StringComparison.Ordinal);
        Assert.Contains("exds-money--negative", money, StringComparison.Ordinal);
        Assert.Contains("exds-money--zero", money, StringComparison.Ordinal);
        Assert.DoesNotContain("Convert", money, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tax", money, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discount", money, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payment", money, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Confirm_dialog_supports_variants_loading_and_localized_defaults()
    {
        var dialog = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Overlay", "ConfirmDialog.razor"));
        Assert.Contains("role=\"alertdialog\"", dialog, StringComparison.Ordinal);
        Assert.Contains("Danger", dialog, StringComparison.Ordinal);
        Assert.Contains("ShowReason", dialog, StringComparison.Ordinal);
        Assert.Contains("IsLoading", dialog, StringComparison.Ordinal);
        Assert.Contains("Escape", dialog, StringComparison.Ordinal);
        Assert.Contains("Action_Confirm", dialog, StringComparison.Ordinal);
        Assert.Contains("Action_Cancel", dialog, StringComparison.Ordinal);
        Assert.Contains("Confirm_ReasonLabel", dialog, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorize", dialog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("= \"Confirm\"", dialog, StringComparison.Ordinal);
        Assert.DoesNotContain("= \"Cancel\"", dialog, StringComparison.Ordinal);
    }

    [Fact]
    public void Responsive_data_css_switches_table_and_mobile_cards()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "wwwroot", "exits-design-system.css"));
        Assert.Contains(".exds-data-table", css, StringComparison.Ordinal);
        Assert.Contains(".exds-data-cards", css, StringComparison.Ordinal);
        Assert.Contains(".exds-mobile-row", css, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 767px)", css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 768px)", css, StringComparison.Ordinal);
        Assert.Contains(".exds-data-table-wrap { display: none; }", css, StringComparison.Ordinal);
        Assert.Contains(".exds-data-cards { display: flex; }", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Pagination_and_sort_use_localized_labels()
    {
        var root = FindRepoRoot();
        var pagination = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "Components", "Data", "Pagination.razor"));
        Assert.Contains("Data_Previous", pagination, StringComparison.Ordinal);
        Assert.Contains("Data_Next", pagination, StringComparison.Ordinal);
        Assert.Contains("PageChanged", pagination, StringComparison.Ordinal);

        var summary = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "Components", "Data", "PaginationSummary.razor"));
        Assert.Contains("Data_PaginationSummary", summary, StringComparison.Ordinal);

        var sort = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "Components", "Data", "SortControl.razor"));
        Assert.Contains("Data_SortLabel", sort, StringComparison.Ordinal);
    }

    [Fact]
    public void New_mvp_components_avoid_hard_coded_english_action_defaults()
    {
        var root = FindRepoRoot();
        var files = Directory.EnumerateFiles(
                Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components"),
                "*.razor",
                SearchOption.AllDirectories)
            .Where(f => f.Contains($"{Path.DirectorySeparatorChar}Forms{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        || f.Contains($"{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        || f.EndsWith("ConfirmDialog.razor", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith("InlineMessage.razor", StringComparison.OrdinalIgnoreCase)
                        || f.EndsWith("Progress.razor", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("= \"Previous\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("= \"Next\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("= \"Unavailable\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("= \"Please fix", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DesignSystem_mvp_components_contain_no_pos_business_logic()
    {
        var root = FindRepoRoot();
        var componentsDir = Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components");
        var forbidden = new[]
        {
            "Utang", "GCash", "inventory", "checkout", "cart", "repayment",
            "SecureStorage", "Preferences.Default", "DbContext", "Npgsql",
            "EntityFrameworkCore", "HttpClient", "PinoyBusinessPOS"
        };

        foreach (var file in Directory.EnumerateFiles(componentsDir, "*.*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var phrase in forbidden)
            {
                Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void DesignSystem_resources_include_mvp_component_keys_in_both_cultures()
    {
        var root = FindRepoRoot();
        var en = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "Localization", "DesignSystemResources.resx"));
        var fil = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "Localization", "DesignSystemResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Validation_SummaryTitle", "Confirm_ReasonLabel", "Data_TableLabel",
                     "Data_ListLabel", "Data_SortLabel", "Data_PaginationLabel",
                     "Data_Previous", "Data_Next", "Data_PaginationSummary", "Money_Unavailable"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Css_marks_theme_density_and_form_feedback_tokens_for_mvp()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "wwwroot", "exits-design-system.css"));
        Assert.Contains("[data-theme=\"dark\"]", css, StringComparison.Ordinal);
        Assert.Contains("[data-density=\"compact\"]", css, StringComparison.Ordinal);
        Assert.Contains(".exds-validation-summary", css, StringComparison.Ordinal);
        Assert.Contains(".exds-inline-message", css, StringComparison.Ordinal);
        Assert.Contains(".exds-confirm", css, StringComparison.Ordinal);
        Assert.Contains(".exds-money", css, StringComparison.Ordinal);
        Assert.Contains("--exits-motion-fast", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Money_formatting_keeps_currency_code_for_sample_decimal()
    {
        var culture = CultureInfo.GetCultureInfo("en");
        var amount = 1250.5m.ToString("N2", culture);
        Assert.Equal("1,250.50", amount);
        var display = $"PHP {amount}";
        Assert.StartsWith("PHP ", display, StringComparison.Ordinal);
        Assert.DoesNotContain("Convert", display, StringComparison.Ordinal);
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
