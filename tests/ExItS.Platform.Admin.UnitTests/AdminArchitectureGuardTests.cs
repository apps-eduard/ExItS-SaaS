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
    public void Admin_pages_do_not_include_deferred_mutation_controls()
    {
        var root = FindRepositoryRoot();
        var pagesDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages");
        var forbidden = new[]
        {
            "Confirm payment", "Reject payment", "Void payment", "Activate subscription",
            "Enter grace", "Mark past due", "Suspend subscription", "Reactivate",
            "Create product", "Publish plan", "Generate snapshot"
        };

        foreach (var file in Directory.GetFiles(pagesDir, "*.razor"))
        {
            var text = File.ReadAllText(file);
            foreach (var phrase in forbidden)
            {
                Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
            }
        }
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
    }

    [Fact]
    public void Admin_nav_exposes_required_routes()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        foreach (var href in new[] { "/admin", "/admin/products", "/admin/organizations", "/admin/subscriptions", "/admin/payments", "/admin/entitlements" })
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
