namespace ExItS.ArchitectureTests;

public sealed class OrgWebAdminArchitectureTests
{
    [Fact]
    public void Organization_web_uses_antdesign_and_does_not_reference_infrastructure()
    {
        var csproj = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web",
            "ExItS.PinoyBusinessPOS.Web.csproj"));
        Assert.Contains("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ExItS.Web.UI", csproj, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.ApiClient", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.DesignSystem", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Admin", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentUI", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MudBlazor", csproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Organization_web_is_in_the_solution()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.PinoyBusinessPOS.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.Web.Tests.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain(
            PortfolioIndependenceTokens.ForbiddenToken,
            slnx,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Organization_web_shell_uses_antdesign_and_shared_theme()
    {
        var root = FindRepositoryRoot();
        var web = Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web");
        var program = File.ReadAllText(Path.Combine(web, "Program.cs"));
        var app = File.ReadAllText(Path.Combine(web, "Components", "App.razor"));
        var layout = File.ReadAllText(Path.Combine(web, "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("AddAntDesign", program, StringComparison.Ordinal);
        Assert.Contains("MapExitsCultureSet", program, StringComparison.Ordinal);
        Assert.Contains("/session/establish", program, StringComparison.Ordinal);
        Assert.Contains(".ExItS.OrgWeb.Auth", program, StringComparison.Ordinal);
        Assert.Contains("/_content/AntDesign/css/ant-design-blazor.css", app, StringComparison.Ordinal);
        Assert.Contains("/_content/ExItS.Web.UI/theme-boot.js", app, StringComparison.Ordinal);
        Assert.Contains("<Sider", layout, StringComparison.Ordinal);
        Assert.Contains("ExitsThemeSelector", layout, StringComparison.Ordinal);
        Assert.Contains("ExitsLanguageSelector", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.DesignSystem", layout, StringComparison.Ordinal);

        var pages = string.Join('\n', Directory.GetFiles(Path.Combine(web, "Components", "Pages"), "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("@page \"/overview\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/products\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/inventory\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/customers\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/organization/branches\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/staff\"", pages, StringComparison.Ordinal);
        Assert.Contains("@page \"/settings\"", pages, StringComparison.Ordinal);
        Assert.DoesNotContain("@using ExItS.DesignSystem", pages, StringComparison.Ordinal);
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
