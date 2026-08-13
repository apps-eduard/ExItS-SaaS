namespace ExItS.ArchitectureTests;

public sealed class OrgWebAdminArchitectureTests
{
    [Fact]
    public void Organization_web_does_not_reference_infrastructure_ef_or_antdesign()
    {
        var csproj = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web",
            "ExItS.PinoyBusinessPOS.Web.csproj"));
        Assert.Contains("ExItS.DesignSystem", csproj, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.ApiClient", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Admin", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentUI", csproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Organization_web_is_in_the_solution()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.PinoyBusinessPOS.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.Web.Tests.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare", slnx, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_validation_includes_organization_web_port_8093()
    {
        var root = FindRepositoryRoot();
        var start = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        var stack = File.ReadAllText(Path.Combine(root, "tools", "LocalValidation.stack.ps1"));
        var launch = File.ReadAllText(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "Properties", "launchSettings.json"));
        Assert.Contains("DefaultOrgWebPort", stack, StringComparison.Ordinal);
        Assert.Contains("8093", stack, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8093", start, StringComparison.Ordinal);
        Assert.Contains("http://0.0.0.0:8093", launch, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.Web", start, StringComparison.Ordinal);
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
