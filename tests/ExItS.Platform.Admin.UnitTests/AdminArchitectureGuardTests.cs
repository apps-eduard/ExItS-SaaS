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

        Assert.Contains("Subscriptions_StartTrial", subscriptions, StringComparison.Ordinal);
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

            Assert.DoesNotContain("SSO", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Active Directory", text, StringComparison.OrdinalIgnoreCase);

            // MFA challenge is allowed on Users lifecycle reactivation (P16-WP11); not on membership/access pages.
            if (!string.Equals(file, "Users.razor", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("MFA", text, StringComparison.OrdinalIgnoreCase);
            }

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
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("enabled-products", nav, StringComparison.Ordinal);
        Assert.Contains("my-products", nav, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(pagesDir, "OrganizationEnabledProducts.razor")));
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
        Assert.DoesNotContain("/admin/login/local-validation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("StaticWebAssetMaterializer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("LivePreview", program, StringComparison.Ordinal);
        Assert.DoesNotContain("UseStaticWebAssets()", program, StringComparison.Ordinal);
        Assert.Contains("Results.Redirect(\"/admin\")", program, StringComparison.Ordinal);
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
        var accountNav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminAccountUserNav.cs"));
        foreach (var href in new[] { "/admin/products", "/admin/organizations", "/admin/subscriptions", "/admin/platform-roles", "/admin/audit", "/admin/payments", "/admin/entitlements" })
        {
            Assert.Contains($"RouterLink=\"{href}\"", nav, StringComparison.Ordinal);
        }

        Assert.Contains("RouterLink=\"/admin\"", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/users", accountNav, StringComparison.Ordinal);
        Assert.Contains("/admin/users/unassigned", accountNav, StringComparison.Ordinal);
        Assert.Contains("/admin/users/organization", accountNav, StringComparison.Ordinal);
        Assert.Contains("/admin/users/platform-staff", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_Accounts", nav, StringComparison.Ordinal);
        Assert.Contains("AdminAccountUserNav", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_AllAccounts", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_NeedsReview", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_RolesPermissions", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationMemberships", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationStaff", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_People", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_Contacts", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_SelectOrganization", nav, StringComparison.Ordinal);
        Assert.Contains("/invitations", accountNav, StringComparison.Ordinal);
        Assert.DoesNotContain("tab=invitations", accountNav, StringComparison.Ordinal);
        Assert.Contains("IsPlatformShell", nav, StringComparison.Ordinal);
        Assert.Contains("IsOrganizationShell", nav, StringComparison.Ordinal);
        Assert.Contains("IsPersonalShell", nav, StringComparison.Ordinal);
        Assert.Contains("/admin/personal/utang/people", accountNav, StringComparison.Ordinal);
        Assert.Contains("Nav_UtangTracker", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("CanView(PlatformPermissionCodes.ViewPortfolio) || IsOrgAdminMembership", nav, StringComparison.Ordinal);
        Assert.Contains("RouterMatch=\"NavLinkMatch.All\"", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("Sign out", nav, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/admin/logout", nav, StringComparison.Ordinal);
        Assert.Contains("PlatformPermissionCodes", nav, StringComparison.Ordinal);
        Assert.Contains("<Menu", nav, StringComparison.Ordinal);
        Assert.Contains("<SubMenu", nav, StringComparison.Ordinal);
        Assert.Contains("Permissions.Loaded", nav, StringComparison.Ordinal);

        var tableSort = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminTableSort.cs"));
        Assert.DoesNotContain(".ConfigureAwait(false)", tableSort, StringComparison.Ordinal);
        Assert.Contains("sync context", tableSort, StringComparison.OrdinalIgnoreCase);

        var entitlementsPage = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Entitlements.razor"));
        Assert.Contains("AdminShellContext Shell", entitlementsPage, StringComparison.Ordinal);
        Assert.Contains("_suppressInitialTableChange", entitlementsPage, StringComparison.Ordinal);
        Assert.Contains("CanViewEntitlements", entitlementsPage, StringComparison.Ordinal);

        foreach (var pageName in new[] { "Organizations.razor", "Plans.razor", "Subscriptions.razor" })
        {
            var page = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", pageName));
            Assert.Contains("_suppressInitialTableChange", page, StringComparison.Ordinal);
            Assert.Contains("Shell.EnsureLoadedAsync()", page, StringComparison.Ordinal);
            Assert.Contains("_pageReady", page, StringComparison.Ordinal);
        }

        var usersPage = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        Assert.Contains("CanCreatePlatformStaff", usersPage, StringComparison.Ordinal);
        Assert.Contains("DirectoryFilter == \"PlatformStaff\"", usersPage, StringComparison.Ordinal);
        Assert.Contains("_loadedDirectory", usersPage, StringComparison.Ordinal);
        Assert.Contains("LocationChanged", usersPage, StringComparison.Ordinal);
        Assert.Contains("EnsureDirectoryListAsync", usersPage, StringComparison.Ordinal);
        Assert.Contains("IsPlatformShell", usersPage, StringComparison.Ordinal);
        Assert.Contains("Scope_PlatformAccountsDenied", usersPage, StringComparison.Ordinal);
        Assert.Contains("Users_AccountType", usersPage, StringComparison.Ordinal);
        Assert.Contains("Users_OrganizationName", usersPage, StringComparison.Ordinal);
        Assert.Contains("AccountClasses", usersPage, StringComparison.Ordinal);
        Assert.Contains("OrganizationNames", usersPage, StringComparison.Ordinal);

        var apiClient = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        Assert.DoesNotContain(".ConfigureAwait(false)", apiClient, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs"));
        Assert.Contains("DetailedErrors", program, StringComparison.Ordinal);

        Assert.Contains("AddScoped<AdminShellContext>",
            File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs")),
            StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminShellContext.cs"));
        Assert.Contains("AdminShellMode.Personal", shell, StringComparison.Ordinal);
        Assert.Contains("AccountClass", shell, StringComparison.Ordinal);
        Assert.Contains("isPlatformAccount", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("HasAnyPermission(", shell, StringComparison.Ordinal);

        var permissionState = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformPermissionState.cs"));
        Assert.Contains("EnsureLoadedForNonPlatformAsync", permissionState, StringComparison.Ordinal);
        Assert.Contains("allowDevFallback", permissionState, StringComparison.Ordinal);

        var membersPage = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationMembers.razor"));
        Assert.Contains("Shell.IsPlatformShell", membersPage, StringComparison.Ordinal);

        var guard = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Authentication", "AccountScopeGuardMiddleware.cs"));
        Assert.Contains("/api/v1/platform/authorization/me", guard, StringComparison.Ordinal);
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
                     ("OrganizationInvitations.razor", "ManageMemberships"),
                     ("OrganizationProductAccess.razor", "ManageProductAccess"),
                 })
        {
            var text = File.ReadAllText(Path.Combine(pages, file));
            Assert.Contains("UnauthorizedPanel", text, StringComparison.Ordinal);
            Assert.Contains(permission, text, StringComparison.Ordinal);
            Assert.Contains("[Authorize]", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Organization_switcher_excludes_platform_administration_and_support_session()
    {
        var root = FindRepositoryRoot();
        var switcher = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "OrganizationContextSwitcher.razor"));
        Assert.Contains("IsOrganizationShell", switcher, StringComparison.Ordinal);
        Assert.Contains("Platform Administration must never appear", switcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Support Session", switcher, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlatformAdministration", switcher, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Organization", switcher, StringComparison.Ordinal);
        Assert.Contains("@using AntDesign", switcher, StringComparison.Ordinal);
        Assert.Contains("<Select", switcher, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", switcher, StringComparison.Ordinal);
        Assert.DoesNotContain("org-context-select", switcher, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("Nav_ComingSoon", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Account_MyProfile\"] <span class=\"exits-phase15-tag\">@L[\"Nav_Phase15\"]", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_uses_antdesign_with_local_validation_picker_but_no_password_leak_or_fluent()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");
        var login = File.ReadAllText(Path.Combine(adminRoot, "Components", "Pages", "Login.razor"));
        var picker = File.ReadAllText(Path.Combine(adminRoot, "Components", "Pages", "LocalValidationIdentityPicker.razor"));
        var signIn = File.ReadAllText(Path.Combine(adminRoot, "Services", "LocalValidationSignInService.cs"));

        Assert.Contains("@using AntDesign", login, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", login, StringComparison.Ordinal);
        Assert.Contains("/admin/login/credentials", login, StringComparison.Ordinal);
        Assert.Contains("type=\"password\"", login, StringComparison.Ordinal);
        Assert.Contains("<Alert", login, StringComparison.Ordinal);
        Assert.Contains("LocalValidationIdentityPicker", login, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", login, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", login, StringComparison.Ordinal);
        Assert.DoesNotContain("FluentUI", login, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exits-native-select", login, StringComparison.Ordinal);

        Assert.Contains("<Select", picker, StringComparison.Ordinal);
        Assert.Contains("DisplayName", picker, StringComparison.Ordinal);
        Assert.Contains("/admin/login/as/", picker, StringComparison.Ordinal);
        Assert.Contains("location.assign", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("Summary", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("/local-validation/sessions", picker, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("live-preview", picker, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("SignInAsKeyAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("quick-login-identities", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("/local-validation/sessions", signIn, StringComparison.OrdinalIgnoreCase);
        var sessionService = File.ReadAllText(Path.Combine(adminRoot, "Services", "PlatformBrowserSessionService.cs"));
        Assert.Contains("/api/v1/platform/auth/login", sessionService, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(adminRoot, "Program.cs"));
        Assert.Contains("/admin/login/as/{key}", program, StringComparison.Ordinal);
        Assert.Contains("LocalValidationSignInService", program, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(adminRoot, "Services", "LivePreviewStaticWebAssetMaterializer.cs")));
        var servicesDir = Path.Combine(adminRoot, "Services");
        Assert.True(Directory.Exists(servicesDir));
        Assert.Empty(Directory.EnumerateFiles(servicesDir, "*Materializer*.cs"));

        var css = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "app.css"));
        Assert.DoesNotContain("exits-native-input", css, StringComparison.Ordinal);
        Assert.DoesNotContain("exits-native-select", css, StringComparison.Ordinal);
        Assert.DoesNotContain("org-context-select", css, StringComparison.Ordinal);

        var a11y = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "admin-a11y.js"));
        Assert.DoesNotContain("closeDrawer", a11y, StringComparison.Ordinal);
        Assert.DoesNotContain("exitsAdminShell", a11y, StringComparison.Ordinal);
        Assert.Contains("dialogOpen", a11y, StringComparison.Ordinal);

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.DoesNotContain("closeDrawer", themeBoot, StringComparison.Ordinal);
        Assert.DoesNotContain("exitsAdminShell", themeBoot, StringComparison.Ordinal);

        var launch = File.ReadAllText(Path.Combine(adminRoot, "Properties", "launchSettings.json"));
        Assert.DoesNotContain("LocalValidation", launch, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enabled_products_page_uses_antdesign_select_and_alert()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationEnabledProducts.razor"));
        Assert.Contains("@using AntDesign", page, StringComparison.Ordinal);
        Assert.Contains("<Select", page, StringComparison.Ordinal);
        Assert.Contains("<Alert", page, StringComparison.Ordinal);
        Assert.Contains("ButtonType.Primary", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", page, StringComparison.Ordinal);
        Assert.DoesNotContain("FluentUI", page, StringComparison.OrdinalIgnoreCase);
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
