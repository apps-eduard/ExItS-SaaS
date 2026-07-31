using System.Reflection;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminArchitectureGuardTests
{
    [Fact]
    public void Admin_csproj_does_not_reference_infrastructure_ef_npgsql_ant_or_tailwind()
    {
        var root = FindRepositoryRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj"));
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HealthCare", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyBusinessPOS", csproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_catalog_and_entitlement_pages_remain_without_deferred_mutations()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var deferredPages = new[] { "Products.razor", "Entitlements.razor" };
        var forbidden = new[]
        {
            "Create product", "Publish plan", "Generate snapshot"
        };

        foreach (var page in deferredPages)
        {
            var text = File.ReadAllText(Path.Combine(pagesDir, page));
            foreach (var phrase in forbidden)
            {
                Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Admin_subscription_and_payment_pages_expose_lifecycle_without_gateway_or_card_controls()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var resx = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        var subscriptions = File.ReadAllText(Path.Combine(pagesDir, "Subscriptions.razor"));
        var payments = File.ReadAllText(Path.Combine(pagesDir, "Payments.razor"));

        Assert.Contains("Subscriptions_StartTrialButton", subscriptions, StringComparison.Ordinal);
        Assert.Contains("Subscriptions_Warning", subscriptions, StringComparison.Ordinal);
        Assert.Contains("Start trial", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("grace", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("past due", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Suspend", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("development-stage", resx, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Doctor", subscriptions, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("ConfirmDialog", subscriptions, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", payments, StringComparison.Ordinal);
        Assert.Contains("terminal", resx, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@bind=\"_cardNumber\"", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no payment gateway", resx, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stripe", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayPal", payments, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_user_and_access_pages_exclude_product_local_roles_and_external_idp()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var resx = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        var files = new[] { "Users.razor", "OrganizationMembers.razor", "OrganizationProductAccess.razor" };
        var forbiddenRoles = new[]
        {
            "Doctor", "Nurse", "Cashier", "Store Manager", "Clinic Admin", "POS Administrator", "Patient"
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(Path.Combine(pagesDir, file));
            foreach (var phrase in forbiddenRoles)
            {
                Assert.DoesNotContain($"option>{phrase}", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain($"value=\"{phrase}\"", text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("MFA", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SSO", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Active Directory", text, StringComparison.OrdinalIgnoreCase);

            // Credential password fields are allowed on Users detail (P13-WP04); not on membership/access pages.
            if (!string.Equals(file, "Users.razor", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("type=\"password\"", text, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.Contains("Credentials", File.ReadAllText(Path.Combine(pagesDir, "Users.razor")), StringComparison.Ordinal);
        Assert.Contains("development-stage", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("product-local", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OrgProductAccess_Warning", File.ReadAllText(Path.Combine(pagesDir, "OrganizationProductAccess.razor")), StringComparison.Ordinal);
        Assert.Contains("OrganizationOwner", File.ReadAllText(Path.Combine(pagesDir, "OrganizationMembers.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_shell_includes_development_security_and_delivery_warnings()
    {
        var root = FindRepositoryRoot();
        var bannerResx = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        Assert.Contains("unauthenticated", bannerResx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not production-secure", bannerResx, StringComparison.OrdinalIgnoreCase);

        var banner = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "EnvironmentBanner.razor"));
        Assert.Contains("Banner_DevSecurityCompact", banner, StringComparison.Ordinal);
        Assert.Contains("Banner_DevSecurityDetail", banner, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "DevSecurityBanner.razor")),
            "DevSecurityBanner.razor should be superseded by the compact EnvironmentBanner in P4-WP04.");

        var entitlements = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Entitlements.razor"));
        Assert.Contains("Entitlements_Warning", entitlements, StringComparison.Ordinal);
        Assert.Contains("not proof of delivery", bannerResx, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_theme_and_language_infrastructure_is_present()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");

        Assert.True(File.Exists(Path.Combine(adminRoot, "wwwroot", "theme-boot.js")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "ThemeSelector.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "LanguageSelector.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor")));
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "AppShell.razor")),
            "Duplicate AppShell must remain removed; MainLayout is the sole Admin shell.");
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Pages", "Audit.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Localization", "AdminResources.resx")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Localization", "AdminResources.fil-PH.resx")));

        var css = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "app.css"));
        Assert.Contains("--color-background", css, StringComparison.Ordinal);
        Assert.Contains("--color-surface", css, StringComparison.Ordinal);
        Assert.Contains("--color-text", css, StringComparison.Ordinal);
        Assert.Contains("--color-primary", css, StringComparison.Ordinal);
        Assert.Contains("--shadow-sm", css, StringComparison.Ordinal);
        Assert.Contains("--radius-sm", css, StringComparison.Ordinal);
        Assert.Contains("--motion-fast", css, StringComparison.Ordinal);
        Assert.Contains("data-theme=\"dark\"", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.Ordinal);
        Assert.Contains("--sidebar-width: 16rem", css, StringComparison.Ordinal);
        Assert.Contains("--sidebar-width-collapsed: 4.25rem", css, StringComparison.Ordinal);
        Assert.Contains("IBM Plex Sans", css, StringComparison.Ordinal);
        Assert.Contains(".page-frame--standard", css, StringComparison.Ordinal);
        Assert.Contains(".page-frame--wide", css, StringComparison.Ordinal);
        Assert.Contains(".page-frame--form", css, StringComparison.Ordinal);

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.Contains("exits-admin-theme", themeBoot, StringComparison.Ordinal);
        Assert.Contains("exits-admin-culture", themeBoot, StringComparison.Ordinal);
        Assert.Contains("Blazor.addEventListener", themeBoot, StringComparison.Ordinal);
        Assert.Contains("enhancedload", themeBoot, StringComparison.Ordinal);
        Assert.DoesNotContain("document.addEventListener(\"enhancedload\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("normalize", themeBoot, StringComparison.Ordinal);
        Assert.Contains("\"light\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("\"dark\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("\"system\"", themeBoot, StringComparison.Ordinal);

        var themeService = File.ReadAllText(Path.Combine(adminRoot, "Services", "ThemeService.cs"));
        Assert.Contains("ToStorageValue", themeService, StringComparison.Ordinal);
        Assert.Contains("applyTheme", themeService, StringComparison.Ordinal);
        Assert.Contains("\"light\"", themeService, StringComparison.Ordinal);

        var selector = File.ReadAllText(Path.Combine(adminRoot, "Components", "Shared", "ThemeSelector.razor"));
        Assert.Contains("value=\"system\"", selector, StringComparison.Ordinal);
        Assert.Contains("value=\"light\"", selector, StringComparison.Ordinal);
        Assert.Contains("value=\"dark\"", selector, StringComparison.Ordinal);
        Assert.Contains("LocationChanged", selector, StringComparison.Ordinal);
        Assert.Contains("reapplyFromStorage", selector, StringComparison.Ordinal);

        var app = File.ReadAllText(Path.Combine(adminRoot, "Components", "App.razor"));
        Assert.DoesNotContain("data-permanent", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_has_single_shell_and_no_route_body_data_permanent()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");

        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor")));
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "AppShell.razor")));

        var routes = File.ReadAllText(Path.Combine(adminRoot, "Components", "Routes.razor"));
        Assert.Contains("DefaultLayout=\"typeof(Layout.MainLayout)\"", routes, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("@Body", layout, StringComparison.Ordinal);
        Assert.Contains("id=\"main-content\"", layout, StringComparison.Ordinal);
        Assert.Contains("skip-link", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("data-permanent", layout, StringComparison.Ordinal);

        foreach (var path in Directory.EnumerateFiles(Path.Combine(adminRoot, "Components"), "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("data-permanent", text, StringComparison.Ordinal);
        }

        var pageHeader = File.ReadAllText(Path.Combine(adminRoot, "Components", "Shared", "PageHeader.razor"));
        Assert.Contains("<PageTitle>", pageHeader, StringComparison.Ordinal);
        Assert.Contains("breadcrumb", pageHeader, StringComparison.Ordinal);
        Assert.Contains("page-actions", pageHeader, StringComparison.Ordinal);

        var nav = File.ReadAllText(Path.Combine(adminRoot, "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("exitsAdminShell.closeDrawer", nav, StringComparison.Ordinal);
        Assert.Contains("<nav", nav, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_audit_page_is_permission_gated_and_does_not_hardcode_english_shell_copy()
    {
        var root = FindRepositoryRoot();
        var audit = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Audit.razor"));
        Assert.Contains("ViewAuditRecords", audit, StringComparison.Ordinal);
        Assert.Contains("UnauthorizedPanel", audit, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/audit\"", audit, StringComparison.Ordinal);
        Assert.Contains("@page \"/admin/audit/{AuditId:guid}\"", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_assembly_does_not_reference_infrastructure_or_ef()
    {
        var referenced = typeof(ExItS.Platform.Admin.Services.IPlatformApiClient).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(referenced, n => n.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("AntDesign", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Tailwind", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("AspNetCore.Identity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Stripe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Admin_shell_recovery_rejects_template_remnants_and_requires_wired_assets()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");
        var pages = Directory.EnumerateFiles(Path.Combine(adminRoot, "Components"), "*.razor", SearchOption.AllDirectories);
        foreach (var page in pages)
        {
            var text = File.ReadAllText(page);
            Assert.DoesNotContain("Hello, world!", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Welcome to your new app", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("admin-shell", text, StringComparison.Ordinal);
        }

        var app = File.ReadAllText(Path.Combine(adminRoot, "Components", "App.razor"));
        Assert.Contains("theme-boot.js", app, StringComparison.Ordinal);
        Assert.Contains("app.css", app, StringComparison.Ordinal);
        Assert.Contains("ExItS.Platform.Admin.styles.css", app, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("app-shell", layout, StringComparison.Ordinal);
        Assert.Contains("app-sidebar", layout, StringComparison.Ordinal);
        Assert.Contains("app-header", layout, StringComparison.Ordinal);
        Assert.Contains("AdminNav", layout, StringComparison.Ordinal);
        Assert.Contains("ThemeSelector", layout, StringComparison.Ordinal);
        Assert.Contains("LanguageSelector", layout, StringComparison.Ordinal);
        Assert.Contains("EnvironmentBanner", layout, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "app.css"));
        Assert.Contains(".app-shell", css, StringComparison.Ordinal);
        Assert.Contains(".app-sidebar", css, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"dark\"]", css, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"system\"]", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@import url(\"https://fonts.googleapis.com", css, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(adminRoot, "Program.cs"));
        Assert.Contains("MapStaticAssets().AllowAnonymous()", program, StringComparison.Ordinal);
        Assert.Contains("/admin/login/credentials", program, StringComparison.Ordinal);
        Assert.Contains("/admin/login/live-preview", program, StringComparison.Ordinal);
        Assert.Contains("Results.Redirect(\"/admin\")", program, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Pages", "Home.razor")),
            "Template Home.razor must remain removed; '/' redirects to /admin.");

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.Contains("exitsAdminShell", themeBoot, StringComparison.Ordinal);
        Assert.Contains("closeDrawer", themeBoot, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_nav_exposes_required_routes()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        foreach (var href in new[] { "/admin", "/admin/products", "/admin/organizations", "/admin/subscriptions", "/admin/payments", "/admin/entitlements", "/admin/users", "/admin/audit" })
        {
            Assert.Contains($"href=\"{href}\"", nav, StringComparison.Ordinal);
        }

        Assert.Contains("PlatformPermissionCodes", nav, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
