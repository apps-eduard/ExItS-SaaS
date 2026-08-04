using System.Reflection;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class MauiFoundationGuardTests
{
    [Fact]
    public void Maui_project_targets_android_first_and_excludes_bootstrap()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "ExItS.PinoyBusinessPOS.Maui.csproj"));
        Assert.Contains("net10.0-android", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bootstrap", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HealthCare", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);

        var bootstrapDir = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "lib", "bootstrap");
        Assert.False(Directory.Exists(bootstrapDir));
    }

    [Fact]
    public void Shell_home_settings_and_deferred_routes_exist()
    {
        var root = FindRepoRoot();
        var pages = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components");
        Assert.True(File.Exists(Path.Combine(pages, "Layout", "PosShell.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "Pages", "Home.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "Pages", "Settings.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "Pages", "MoreHub.razor")));

        var home = File.ReadAllText(Path.Combine(pages, "Pages", "Home.razor"));
        Assert.DoesNotContain("fake sales", home, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventory count", home, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ResolveStartRouteAsync", home, StringComparison.Ordinal);
        Assert.True(
            File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui", "Services", "PosStatusState.cs"))
                .Contains("GetHealthAsync", StringComparison.Ordinal));

        var more = File.ReadAllText(Path.Combine(pages, "Pages", "MoreHub.razor"));
        Assert.Contains("@page \"/more\"", more, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/customers\"", more, StringComparison.Ordinal);
        Assert.DoesNotContain("Deferred_", more, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(pages, "Pages", "Settings.razor"));
        Assert.Contains("Theme", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Density", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Language", settings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", settings, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Customer_routes_cover_list_create_detail_edit_credit_payment_ledger_overdue_statement_and_receipt()
    {
        var root = FindRepoRoot();
        var customers = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages", "Customers");
        Assert.True(File.Exists(Path.Combine(customers, "CustomersList.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerCreate.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerDetail.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerEdit.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerForm.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CreditCreate.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CreditDetail.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "RepaymentCreate.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "RepaymentDetail.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerLedger.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "OverdueList.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerOverdue.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "CustomerStatement.razor")));
        Assert.True(File.Exists(Path.Combine(customers, "RepaymentReceipt.razor")));

        foreach (var file in Directory.EnumerateFiles(customers, "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("UtangBalanceField", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RecordSale", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SyncQueue", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PrintReceipt", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PaymentGateway", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Installment", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Interest", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Penalty", text, StringComparison.OrdinalIgnoreCase);
        }

        var list = File.ReadAllText(Path.Combine(customers, "CustomersList.razor"));
        Assert.Contains("@page \"/customers\"", list, StringComparison.Ordinal);
        Assert.Contains("Customers_CreditDeferred", list, StringComparison.Ordinal);
        Assert.Contains("ResponsiveDataList", list, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(customers, "CustomerCreate.razor"));
        Assert.Contains("@page \"/customers/new\"", create, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(customers, "CustomerDetail.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("Deactivate", detail, StringComparison.Ordinal);
        Assert.Contains("Reactivate", detail, StringComparison.Ordinal);
        Assert.Contains("GetUtangSummaryAsync", detail, StringComparison.Ordinal);
        Assert.Contains("GoAddPayment", detail, StringComparison.Ordinal);
        Assert.Contains("GoLedger", detail, StringComparison.Ordinal);
        Assert.Contains("GoOverdue", detail, StringComparison.Ordinal);
        Assert.Contains("GoStatement", detail, StringComparison.Ordinal);
        Assert.Contains("CurrentDueDate", detail, StringComparison.Ordinal);

        var edit = File.ReadAllText(Path.Combine(customers, "CustomerEdit.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/edit\"", edit, StringComparison.Ordinal);

        var ledger = File.ReadAllText(Path.Combine(customers, "CustomerLedger.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/ledger\"", ledger, StringComparison.Ordinal);

        var creditDetail = File.ReadAllText(Path.Combine(customers, "CreditDetail.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/credit/{CreditEntryId:guid}\"", creditDetail, StringComparison.Ordinal);
        Assert.Contains("SetCreditDueDateAsync", creditDetail, StringComparison.Ordinal);
        Assert.Contains("DueDate_HistoryTitle", creditDetail, StringComparison.Ordinal);

        var statement = File.ReadAllText(Path.Combine(customers, "CustomerStatement.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/statement\"", statement, StringComparison.Ordinal);
        Assert.Contains("GetStatementAsync", statement, StringComparison.Ordinal);
        Assert.Contains("IDocumentHandoffService", statement, StringComparison.Ordinal);

        var receipt = File.ReadAllText(Path.Combine(customers, "RepaymentReceipt.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/repayments/{RepaymentId:guid}/receipt\"", receipt, StringComparison.Ordinal);
        Assert.Contains("GetRepaymentReceiptAsync", receipt, StringComparison.Ordinal);
        Assert.Contains("Receipt_Reversed", receipt, StringComparison.Ordinal);

        var repaymentDetail = File.ReadAllText(Path.Combine(customers, "RepaymentDetail.razor"));
        Assert.Contains("GoReceipt", repaymentDetail, StringComparison.Ordinal);

        var overdue = File.ReadAllText(Path.Combine(customers, "OverdueList.razor"));
        Assert.Contains("@page \"/overdue\"", overdue, StringComparison.Ordinal);

        var customerOverdue = File.ReadAllText(Path.Combine(customers, "CustomerOverdue.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/overdue\"", customerOverdue, StringComparison.Ordinal);

        var repaymentCreate = File.ReadAllText(Path.Combine(customers, "RepaymentCreate.razor"));
        Assert.Contains("@page \"/customers/{CustomerId:guid}/repayments/new\"", repaymentCreate, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.fil-PH.resx"));
        Assert.Contains("Customers_Title", en, StringComparison.Ordinal);
        Assert.Contains("Customers_Title", fil, StringComparison.Ordinal);
        Assert.Contains("Payment_Record", en, StringComparison.Ordinal);
        Assert.Contains("Payment_Record", fil, StringComparison.Ordinal);
        Assert.Contains("Ledger_Title", en, StringComparison.Ordinal);
        Assert.Contains("Ledger_Title", fil, StringComparison.Ordinal);
        Assert.Contains("DueDate_Badge_Overdue", en, StringComparison.Ordinal);
        Assert.Contains("DueDate_Badge_Overdue", fil, StringComparison.Ordinal);
        Assert.Contains("Overdue_Title", en, StringComparison.Ordinal);
        Assert.Contains("Overdue_Title", fil, StringComparison.Ordinal);
        Assert.Contains("Utang_DeferredMessage", en, StringComparison.Ordinal);
        Assert.Contains("Utang_DeferredMessage", fil, StringComparison.Ordinal);
        Assert.Contains("Statement_Title", en, StringComparison.Ordinal);
        Assert.Contains("Statement_Title", fil, StringComparison.Ordinal);
        Assert.Contains("Receipt_Title", en, StringComparison.Ordinal);
        Assert.Contains("Receipt_Title", fil, StringComparison.Ordinal);
        Assert.Contains("Access_RestrictedMessage", en, StringComparison.Ordinal);
        Assert.Contains("Access_RestrictedMessage", fil, StringComparison.Ordinal);
        Assert.Contains("Handoff_Initiated", en, StringComparison.Ordinal);
        Assert.Contains("Handoff_Initiated", fil, StringComparison.Ordinal);
        Assert.Contains("Interest and credit limits are not available", en, StringComparison.Ordinal);
        Assert.DoesNotContain("Statements, printable receipts, interest", en, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("statements and printable receipts are not available", en, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preference_and_connectivity_services_exist_behind_abstractions()
    {
        var root = FindRepoRoot();
        var services = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Services");
        Assert.True(File.Exists(Path.Combine(services, "MauiThemePreferenceStore.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiDensityPreferenceStore.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiCulturePreferenceStore.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiSecureTokenStore.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiOnboardingPreferenceStore.cs")));
        Assert.False(File.Exists(Path.Combine(services, "NullSecureTokenStore.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiConnectivityService.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiAppInfoService.cs")));
        Assert.True(File.Exists(Path.Combine(services, "DensityController.cs")));
        Assert.True(File.Exists(Path.Combine(services, "ThemeController.cs")));
        Assert.True(File.Exists(Path.Combine(services, "NavigationGate.cs")));
    }

    [Fact]
    public void Auth_routes_and_secure_storage_foundation_exist()
    {
        var root = FindRepoRoot();
        var pages = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages");
        Assert.True(File.Exists(Path.Combine(pages, "Boot.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "Welcome.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "SignIn.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "OrganizationSelect.razor")));
        Assert.True(File.Exists(Path.Combine(pages, "AccessDenied.razor")));

        var secure = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Services", "MauiSecureTokenStore.cs"));
        Assert.Contains("SecureStorage", secure, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences", secure, StringComparison.Ordinal);

        var authService = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application", "Auth", "AuthenticationService.cs"));
        Assert.Contains("IsDevelopmentAuthenticationEnabled", authService, StringComparison.Ordinal);
        Assert.DoesNotContain("Cashier", authService, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Store Manager", authService, StringComparison.OrdinalIgnoreCase);
        // Password grant is authorized (P13-WP06); passwords must never be persisted to SecureStorage.
        Assert.Contains("IssueTokenAsync", authService, StringComparison.Ordinal);
        Assert.Contains("GrantType: \"password\"", authService, StringComparison.Ordinal);
        Assert.DoesNotContain("SecureTokenKeys.Password", authService, StringComparison.Ordinal);
        Assert.DoesNotContain("tokens.SetAsync(SecureTokenKeys.AccessToken, password", authService, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Theme_boot_applies_theme_and_density_before_paint()
    {
        var boot = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "theme-boot.js"));
        Assert.Contains("applyTheme", boot, StringComparison.Ordinal);
        Assert.Contains("applyDensity", boot, StringComparison.Ordinal);
        Assert.Contains("data-density", boot, StringComparison.Ordinal);
        Assert.Contains("compact", boot, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_and_app_css_define_phone_and_tablet_layout_markers()
    {
        var root = FindRepoRoot();
        var shell = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Layout", "PosShell.razor"));
        Assert.Contains("pos-bottom-nav", shell, StringComparison.Ordinal);
        Assert.Contains("pos-nav-item--active", shell, StringComparison.Ordinal);
        Assert.Contains("data-layout=\"phone\"", shell, StringComparison.Ordinal);
        Assert.Contains("DensityCtl", shell, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "app.css"));
        Assert.Contains("min-width: 768px", css, StringComparison.Ordinal);
        Assert.Contains("orientation: landscape", css, StringComparison.Ordinal);
        Assert.Contains("safe-area-inset", css, StringComparison.Ordinal);
        Assert.Contains("pos-status-grid", css, StringComparison.Ordinal);
        Assert.DoesNotContain("status-bar-safe-area", css, StringComparison.Ordinal);
        Assert.DoesNotContain("+ env(safe-area-inset-top)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("safe-area-inset-top))", css, StringComparison.Ordinal);
    }

    [Fact]
    public void MainPage_applies_safe_area_edges_container_once_at_host()
    {
        var root = FindRepoRoot();
        var maui = Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

        var mainPage = File.ReadAllText(Path.Combine(maui, "MainPage.xaml"));
        Assert.Contains("SafeAreaEdges=\"Container\"", mainPage, StringComparison.Ordinal);
        Assert.Contains("BlazorWebView", mainPage, StringComparison.Ordinal);

        var appXaml = File.ReadAllText(Path.Combine(maui, "App.xaml"));
        Assert.Contains("SafeAreaEdges", appXaml, StringComparison.Ordinal);
        Assert.Contains("Container", appXaml, StringComparison.Ordinal);
        Assert.Contains("ContentPage", appXaml, StringComparison.Ordinal);

        var index = File.ReadAllText(Path.Combine(maui, "wwwroot", "index.html"));
        Assert.DoesNotContain("status-bar-safe-area", index, StringComparison.Ordinal);

        var activity = File.ReadAllText(Path.Combine(maui, "Platforms", "Android", "MainActivity.cs"));
        Assert.Contains("SoftInput.AdjustResize", activity, StringComparison.Ordinal);
        Assert.Contains("SafeAreaEdges", activity, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_resources_cover_nav_density_and_deferred_copy()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        Assert.True(File.Exists(Path.Combine(loc, "PosResources.resx")));
        Assert.True(File.Exists(Path.Combine(loc, "PosResources.fil-PH.resx")));
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        Assert.Contains("Nav_Home", en, StringComparison.Ordinal);
        Assert.Contains("Settings_", en, StringComparison.Ordinal);
        Assert.Contains("Settings_DensityLabel", en, StringComparison.Ordinal);
        Assert.Contains("Settings_Density_Compact", en, StringComparison.Ordinal);
        Assert.Contains("Nav_Primary", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_and_apiclient_have_no_ef_or_healthcare_refs()
    {
        foreach (var project in new[]
                 {
                     Path.Combine("src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
                         "ExItS.PinoyBusinessPOS.Application.csproj"),
                     Path.Combine("src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient",
                         "ExItS.PinoyBusinessPOS.ApiClient.csproj")
                 })
        {
            var text = File.ReadAllText(Path.Combine(FindRepoRoot(), project));
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HealthCare", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void No_sales_inventory_utang_sync_implementation_in_maui_pages()
    {
        var root = FindRepoRoot();
        var pagesDir = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages");
        foreach (var file in Directory.EnumerateFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("SQLite", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SyncQueue", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UtangBalance", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RecordSale", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Stripe", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Dev_component_showcase_is_gated_and_not_in_production_nav()
    {
        var root = FindRepoRoot();
        var showcase = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages", "Dev", "ComponentShowcase.razor");
        Assert.True(File.Exists(showcase));
        var text = File.ReadAllText(showcase);
        Assert.Contains("@page \"/dev/components\"", text, StringComparison.Ordinal);
        Assert.Contains("Development", text, StringComparison.Ordinal);
        Assert.Contains("Testing", text, StringComparison.Ordinal);
        Assert.Contains("DevShowcase_", text, StringComparison.Ordinal);
        Assert.Contains("Sample Alpha", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordSale", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Utang", text, StringComparison.OrdinalIgnoreCase);

        var shell = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Layout", "PosShell.razor"));
        Assert.DoesNotContain("/dev/components", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("DevShowcase", shell, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.resx"));
        Assert.Contains("DevShowcase_Title", en, StringComparison.Ordinal);
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
