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
        Assert.True(File.Exists(Path.Combine(pages, "Pages", "DeferredPage.razor")));

        var home = File.ReadAllText(Path.Combine(pages, "Pages", "Home.razor"));
        Assert.DoesNotContain("fake sales", home, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inventory count", home, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Status.", home, StringComparison.Ordinal);
        Assert.Contains("ApiStatus", home, StringComparison.Ordinal);
        Assert.True(
            File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui", "Services", "PosStatusState.cs"))
                .Contains("GetHealthAsync", StringComparison.Ordinal));

        var deferred = File.ReadAllText(Path.Combine(pages, "Pages", "DeferredPage.razor"));
        Assert.Contains("Deferred_", deferred, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(pages, "Pages", "Settings.razor"));
        Assert.Contains("Theme", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Density", settings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Language", settings, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", settings, StringComparison.OrdinalIgnoreCase);
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
        Assert.True(File.Exists(Path.Combine(services, "MauiConnectivityService.cs")));
        Assert.True(File.Exists(Path.Combine(services, "MauiAppInfoService.cs")));
        Assert.True(File.Exists(Path.Combine(services, "DensityController.cs")));
        Assert.True(File.Exists(Path.Combine(services, "ThemeController.cs")));
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
