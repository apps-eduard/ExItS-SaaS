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
    public void Admin_pages_do_not_include_deferred_commercial_mutation_controls()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var deferredPages = new[] { "Payments.razor", "Subscriptions.razor", "Products.razor", "Entitlements.razor" };
        var forbidden = new[]
        {
            "Confirm payment", "Reject payment", "Void payment", "Activate subscription",
            "Enter grace", "Mark past due", "Suspend subscription",
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
                // Allow explanatory warning text that mentions product-local roles as exclusions.
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
        var banner = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "DevSecurityBanner.razor"));
        Assert.Contains("unauthenticated", banner, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not production-secure", banner, StringComparison.OrdinalIgnoreCase);

        var payments = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Payments.razor"));
        Assert.Contains("not automatic provider verification", payments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never displays card numbers, CVV", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"password\"", payments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@bind=\"_cardNumber\"", payments, StringComparison.OrdinalIgnoreCase);

        var entitlements = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Entitlements.razor"));
        Assert.Contains("not proof of delivery", entitlements, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not evidence that", entitlements, StringComparison.OrdinalIgnoreCase);
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
    }

    [Fact]
    public void Admin_nav_exposes_required_routes()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        foreach (var href in new[] { "/admin", "/admin/products", "/admin/organizations", "/admin/subscriptions", "/admin/payments", "/admin/entitlements", "/admin/users" })
        {
            Assert.Contains($"href=\"{href}\"", nav, StringComparison.Ordinal);
        }

        Assert.Contains("Platform Admin", nav, StringComparison.Ordinal);
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
