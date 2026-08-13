using System.Reflection;

namespace ExItS.ArchitectureTests;

public sealed class PosFoundationArchitectureTests
{
    [Fact]
    public void DesignSystem_and_pos_projects_do_not_reference_platform_infrastructure()
    {
        var root = FindRepositoryRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "ExItS.DesignSystem.csproj"),
            Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Domain",
                "ExItS.PinoyBusinessPOS.Domain.csproj"),
            Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
                "ExItS.PinoyBusinessPOS.Application.csproj"),
            Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient",
                "ExItS.PinoyBusinessPOS.ApiClient.csproj"),
            Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
                "ExItS.PinoyBusinessPOS.Maui.csproj")
        };

        foreach (var project in projects)
        {
            Assert.True(File.Exists(project), project);
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EntityFrameworkCore", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HealthCare", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AntDesign", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tailwind", text, StringComparison.OrdinalIgnoreCase);
        }

        var infra = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Infrastructure", "ExItS.PinoyBusinessPOS.Infrastructure.csproj"));
        Assert.Contains("EntityFrameworkCore", infra, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Npgsql", infra, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HealthCare", infra, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DesignSystem_has_no_product_business_or_apiclient_dependency()
    {
        var csproj = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "Shared", "ExItS.DesignSystem",
            "ExItS.DesignSystem.csproj"));
        Assert.DoesNotContain("PinoyBusinessPOS", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Platform.Api", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.Maui", csproj, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
