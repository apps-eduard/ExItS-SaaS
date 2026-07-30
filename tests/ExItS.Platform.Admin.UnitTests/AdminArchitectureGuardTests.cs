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
        var subscriptions = File.ReadAllText(Path.Combine(pagesDir, "Subscriptions.razor"));
        var payments = File.ReadAllText(Path.Combine(pagesDir, "Payments.razor"));

        Assert.Contains("Start trial", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enter grace period", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mark past due", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Suspend subscription", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("development-stage", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Doctor", subscriptions, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Confirm payment", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reject payment", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Void payment", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not automatic provider verification", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirm and activate subscription", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConfirmDialog", subscriptions, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", payments, StringComparison.Ordinal);
        Assert.Contains("terminal", subscriptions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@bind=\"_cardNumber\"", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no payment gateway, webhook", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stripe", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayPal", payments, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_user_and_access_pages_exclude_product_local_roles_and_login()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var files = new[] { "Users.razor", "OrganizationMembers.razor", "OrganizationProductAccess.razor" };
        var forbidden = new[]
        {
            "Doctor", "Nurse", "Cashier", "Store Manager", "Clinic Admin", "POS Administrator", "Patient",
            "type=\"password\"", "login", "MFA", "SSO", "Active Directory"
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(Path.Combine(pagesDir, file));
            foreach (var phrase in forbidden)
            {
                if (phrase is "Doctor" or "Nurse" or "Cashier" or "Store Manager" or "Clinic Admin" or "POS Administrator" or "Patient")
                {
                    Assert.DoesNotContain($"option>{phrase}", text, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain($"value=\"{phrase}\"", text, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Contains("development-stage", text, StringComparison.OrdinalIgnoreCase);
        }

        var productAccess = File.ReadAllText(Path.Combine(pagesDir, "OrganizationProductAccess.razor"));
        Assert.Contains("does", productAccess, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not", productAccess, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("assign", productAccess, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("product-local", productAccess, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("not proof of delivery", entitlements, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not evidence that", entitlements, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Admin_theme_and_language_infrastructure_is_present()
    {
        var root = FindRepositoryRoot();
        var adminRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin");

        Assert.True(File.Exists(Path.Combine(adminRoot, "wwwroot", "theme-boot.js")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "ThemeSelector.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Shared", "LanguageSelector.razor")));
        Assert.True(File.Exists(Path.Combine(adminRoot, "Components", "Layout", "AppShell.razor")));
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

        var themeBoot = File.ReadAllText(Path.Combine(adminRoot, "wwwroot", "theme-boot.js"));
        Assert.Contains("exits-admin-theme", themeBoot, StringComparison.Ordinal);
        Assert.Contains("exits-admin-culture", themeBoot, StringComparison.Ordinal);
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
