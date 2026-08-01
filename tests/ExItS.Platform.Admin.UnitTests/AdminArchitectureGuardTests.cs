using System.Reflection;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminArchitectureGuardTests
{
    [Fact]
    public void Admin_csproj_pins_antdesign_and_forbids_infrastructure_ef_fluent_tailwind()
    {
        var root = FindRepositoryRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj"));
        var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
        Assert.Contains("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Include=\"AntDesign\" Version=\"1.6.2\"", packages, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentUI", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HealthCare", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PinoyBusinessPOS", csproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_catalog_and_entitlement_pages_remain_without_deferred_mutations()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var deferredPages = new[] { "Products.razor" };
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

        var entitlements = File.ReadAllText(Path.Combine(pagesDir, "Entitlements.razor"));
        Assert.Contains("@using AntDesign", entitlements, StringComparison.Ordinal);
        Assert.Contains("Entitlements_Warning", entitlements, StringComparison.Ordinal);
        Assert.Contains("GenerateEntitlementSnapshotAsync", entitlements, StringComparison.Ordinal);
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
        Assert.DoesNotContain("development-stage", resx, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Doctor", subscriptions, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Popconfirm", subscriptions, StringComparison.Ordinal);
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
        Assert.DoesNotContain("development-stage", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("product-local", resx, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OrgProductAccess_Warning", File.ReadAllText(Path.Combine(pagesDir, "OrganizationProductAccess.razor")), StringComparison.Ordinal);
        Assert.Contains("OrganizationOwner", File.ReadAllText(Path.Combine(pagesDir, "OrganizationMembers.razor")), StringComparison.Ordinal);
        Assert.Contains("AntDesign", File.ReadAllText(Path.Combine(pagesDir, "OrganizationMembers.razor")), StringComparison.Ordinal);
        Assert.Contains("/admin/organization-users", File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationUsers.razor")), StringComparison.Ordinal);
        Assert.Contains("organization-users", File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor")), StringComparison.Ordinal);
        Assert.Contains("Nav_SectionSettings", File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_shell_omits_development_stage_banner_and_keeps_delivery_warnings()
    {
        var root = FindRepositoryRoot();
        var bannerResx = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", "AdminResources.resx"));
        Assert.DoesNotContain("development-stage", bannerResx, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not production-secure", bannerResx, StringComparison.OrdinalIgnoreCase);

        var layout = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "MainLayout.razor"));
        Assert.DoesNotContain("EnvironmentBanner", layout, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "EnvironmentBanner.razor")));
        Assert.False(File.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "DevSecurityBanner.razor")));

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
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "ThemeHost.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "LanguageSelector.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor")));
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "AppShell.razor")),
            "Duplicate AppShell must remain removed; MainLayout is the sole Admin shell.");
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Pages", "Audit.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Localization", "AdminResources.resx")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Localization", "AdminResources.fil-PH.resx")));

        var css = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "app.css"));
        Assert.Contains("--exits-bg", css, StringComparison.Ordinal);
        Assert.Contains("--exits-text", css, StringComparison.Ordinal);
        Assert.Contains("--exits-primary", css, StringComparison.Ordinal);
        Assert.Contains("data-theme=\"dark\"", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.Ordinal);
        Assert.Contains("exits-admin-layout", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@tailwind", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IBM Plex Sans", css, StringComparison.Ordinal);

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.Contains("exits-admin-theme", themeBoot, StringComparison.Ordinal);
        Assert.Contains("exits-admin-culture", themeBoot, StringComparison.Ordinal);
        Assert.Contains("Blazor.addEventListener", themeBoot, StringComparison.Ordinal);
        Assert.Contains("enhancedload", themeBoot, StringComparison.Ordinal);
        Assert.DoesNotContain("document.addEventListener(\"enhancedload\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("normalize", themeBoot, StringComparison.Ordinal);
        Assert.Contains("\"light\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("\"dark\"", themeBoot, StringComparison.Ordinal);
        Assert.Contains("ant-design-blazor.dark.css", themeBoot, StringComparison.Ordinal);
        Assert.Contains("exits-antd-theme", themeBoot, StringComparison.Ordinal);

        var themeService = File.ReadAllText(Path.Combine(adminRoot, "Services", "ThemeService.cs"));
        Assert.Contains("ToStorageValue", themeService, StringComparison.Ordinal);
        Assert.Contains("applyTheme", themeService, StringComparison.Ordinal);
        Assert.Contains("\"light\"", themeService, StringComparison.Ordinal);
        Assert.Contains("\"system\"", themeService, StringComparison.Ordinal);
        Assert.Contains("AdminTheme.System", themeService, StringComparison.Ordinal);

        var selector = File.ReadAllText(Path.Combine(adminRoot, "Components", "Shared", "ThemeSelector.razor"));
        Assert.Contains("<Select", selector, StringComparison.Ordinal);
        Assert.Contains("Theme_System", selector, StringComparison.Ordinal);
        Assert.Contains("Theme_Light", selector, StringComparison.Ordinal);
        Assert.Contains("Theme_Dark", selector, StringComparison.Ordinal);
        Assert.Contains("LocationChanged", selector, StringComparison.Ordinal);
        Assert.Contains("reapplyFromStorage", selector, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleLightDarkAsync", selector, StringComparison.Ordinal);

        var app = File.ReadAllText(Path.Combine(adminRoot, "Components", "App.razor"));
        Assert.DoesNotContain("data-permanent", app, StringComparison.Ordinal);
        Assert.Contains("AddAntDesign", File.ReadAllText(Path.Combine(adminRoot, "Program.cs")), StringComparison.Ordinal);
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
        Assert.Contains("<Sider", layout, StringComparison.Ordinal);
        Assert.Contains("<Header", layout, StringComparison.Ordinal);
        Assert.Contains("<Drawer", layout, StringComparison.Ordinal);
        Assert.Contains("Account_SignOut", layout, StringComparison.Ordinal);
        Assert.Contains("OrganizationContextSwitcher", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("data-permanent", layout, StringComparison.Ordinal);

        foreach (var path in Directory.EnumerateFiles(Path.Combine(adminRoot, "Components"), "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("data-permanent", text, StringComparison.Ordinal);
        }

        var pageHeader = File.ReadAllText(Path.Combine(adminRoot, "Components", "Shared", "AdminPageHeader.razor"));
        Assert.Contains("<PageTitle>", pageHeader, StringComparison.Ordinal);
        Assert.Contains("breadcrumb", pageHeader, StringComparison.Ordinal);

        var nav = File.ReadAllText(Path.Combine(adminRoot, "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("<Menu", nav, StringComparison.Ordinal);
        Assert.Contains("RouterLink=", nav, StringComparison.Ordinal);
        Assert.Contains("PlatformPermissionCodes", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("exitsAdminShell.closeDrawer", nav, StringComparison.Ordinal);
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
    public void Admin_assembly_references_antdesign_but_not_infrastructure_or_ef()
    {
        var referenced = typeof(ExItS.Platform.Admin.Services.IPlatformApiClient).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.Contains(referenced, n => n.Equals("AntDesign", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("FluentUI", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Tailwind", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("AspNetCore.Identity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, n => n.Contains("Stripe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Admin_shell_recovery_rejects_template_remnants_and_requires_antdesign_assets()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");
        var pages = Directory.EnumerateFiles(Path.Combine(adminRoot, "Components"), "*.razor", SearchOption.AllDirectories);
        foreach (var page in pages)
        {
            var text = File.ReadAllText(page);
            Assert.DoesNotContain("Hello, world!", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Welcome to your new app", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FluentUI", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FluentDesignTheme", text, StringComparison.OrdinalIgnoreCase);
        }

        var app = File.ReadAllText(Path.Combine(adminRoot, "Components", "App.razor"));
        Assert.Contains("theme-boot.js", app, StringComparison.Ordinal);
        Assert.Contains("app.css", app, StringComparison.Ordinal);
        Assert.Contains("ExItS.Platform.Admin.styles.css", app, StringComparison.Ordinal);
        Assert.Contains("RootAsset(", app, StringComparison.Ordinal);
        Assert.Contains("/_content/AntDesign/css/ant-design-blazor.css", app, StringComparison.Ordinal);
        Assert.Contains("exits-antd-theme", app, StringComparison.Ordinal);
        Assert.Contains("/_content/AntDesign/js/ant-design-blazor.js", app, StringComparison.Ordinal);
        Assert.Contains("<AntContainer", app, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(adminRoot, "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("exits-admin-layout", layout, StringComparison.Ordinal);
        Assert.Contains("<Sider", layout, StringComparison.Ordinal);
        Assert.Contains("<Header", layout, StringComparison.Ordinal);
        Assert.Contains("AdminNav", layout, StringComparison.Ordinal);
        Assert.Contains("ThemeSelector", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("ThemeHost", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("ActorDisplayName", layout, StringComparison.Ordinal);
        Assert.Contains("LanguageSelector", layout, StringComparison.Ordinal);
        Assert.Contains("AdminShellContext", layout, StringComparison.Ordinal);
        Assert.Contains("Account_SignOut", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("EnvironmentBanner", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("app-shell", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Fluent", layout, StringComparison.OrdinalIgnoreCase);

        var css = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "app.css"));
        Assert.Contains("exits-admin-layout", css, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"dark\"]", css, StringComparison.Ordinal);
        Assert.Contains("--exits-surface", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@import url(\"https://fonts.googleapis.com", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@tailwind", css, StringComparison.OrdinalIgnoreCase);

        var program = File.ReadAllText(Path.Combine(adminRoot, "Program.cs"));
        Assert.Contains("AddAntDesign()", program, StringComparison.Ordinal);
        Assert.Contains("MapStaticAssets().AllowAnonymous()", program, StringComparison.Ordinal);
        Assert.Contains("/admin/login/credentials", program, StringComparison.Ordinal);
        Assert.Contains("/admin/login/live-preview", program, StringComparison.Ordinal);
        Assert.Contains("Results.Redirect(\"/admin\")", program, StringComparison.Ordinal);
        Assert.Contains("UseStaticWebAssets()", program, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(adminRoot, "Components", "Pages", "Home.razor")),
            "Template Home.razor must remain removed; '/' redirects to /admin.");

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.Contains("exitsAdminTheme", themeBoot, StringComparison.Ordinal);
        Assert.Contains("data-theme", themeBoot, StringComparison.Ordinal);
        Assert.Contains("ant-design-blazor.dark.css", themeBoot, StringComparison.Ordinal);
        Assert.Contains("exits-antd-theme", themeBoot, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_nav_exposes_required_routes()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        foreach (var href in new[] { "/admin", "/admin/products", "/admin/organizations", "/admin/subscriptions", "/admin/users", "/admin/users/unassigned", "/admin/platform-roles", "/admin/audit", "/admin/payments", "/admin/entitlements" })
        {
            Assert.Contains($"RouterLink=\"{href}\"", nav, StringComparison.Ordinal);
        }

        Assert.Contains("Nav_AllUsers", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_RolesPermissions", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationUsers", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationMemberships", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_PlatformUsers", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_People", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_SelectOrganization", nav, StringComparison.Ordinal);
        Assert.Contains("tab=invitations", nav, StringComparison.Ordinal);
        Assert.Contains("IsPlatformShell", nav, StringComparison.Ordinal);
        Assert.Contains("IsOrganizationShell", nav, StringComparison.Ordinal);
        Assert.Contains("RouterMatch=\"NavLinkMatch.All\"", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign out", nav, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/logout", nav, StringComparison.Ordinal);
        Assert.Contains("PlatformPermissionCodes", nav, StringComparison.Ordinal);
        Assert.Contains("<Menu", nav, StringComparison.Ordinal);
        Assert.Contains("<SubMenu", nav, StringComparison.Ordinal);

        var usersPage = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        Assert.Contains("_loadedDirectory", usersPage, StringComparison.Ordinal);

        var apiClient = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        Assert.DoesNotContain(".ConfigureAwait(false)", apiClient, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs"));
        Assert.Contains("DetailedErrors", program, StringComparison.Ordinal);

        Assert.Contains("AddScoped<AdminShellContext>",
            File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_sensitive_pages_are_permission_gated_independently_of_nav()
    {
        var root = FindRepositoryRoot();
        var pages = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");

        foreach (var (file, permission) in new[]
                 {
                     ("Users.razor", "ManagePlatformUsers"),
                     ("PlatformRoles.razor", "ManagePlatformUsers"),
                     ("Payments.razor", "ManageManualPayments"),
                     ("Audit.razor", "ViewAuditRecords"),
                     ("OrganizationMembers.razor", "ManageMemberships"),
                     ("OrganizationProductAccess.razor", "ManageProductAccess"),
                 })
        {
            var text = File.ReadAllText(Path.Combine(pages, file));
            Assert.Contains("UnauthorizedPanel", text, StringComparison.Ordinal);
            Assert.Contains(permission, text, StringComparison.Ordinal);
            Assert.Contains("[Authorize]", text, StringComparison.Ordinal);
        }
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
