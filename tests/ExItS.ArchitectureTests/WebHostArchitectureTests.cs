namespace ExItS.ArchitectureTests;

public sealed class WebHostArchitectureTests
{
    [Fact]
    public void AntDesign_version_is_centralized_at_1_6_2()
    {
        var root = FindRepositoryRoot();
        var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
        Assert.Contains("Include=\"AntDesign\" Version=\"1.6.2\"", packages, StringComparison.Ordinal);
        Assert.Equal(1, Count(packages, "Include=\"AntDesign\""));
    }

    [Fact]
    public void Browser_hosts_use_antdesign_and_shared_web_ui()
    {
        var root = FindRepositoryRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj"),
                     Path.Combine("src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "ExItS.PinoyBusinessPOS.Web.csproj"),
                     Path.Combine("src", "Platform", "ExItS.Personal.Web", "ExItS.Personal.Web.csproj"),
                     Path.Combine("src", "Shared", "ExItS.Web.UI", "ExItS.Web.UI.csproj")
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("AntDesign", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Tailwind", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("FluentUI", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MudBlazor", text, StringComparison.OrdinalIgnoreCase);
        }

        var org = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "ExItS.PinoyBusinessPOS.Web.csproj"));
        var personal = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "ExItS.Personal.Web.csproj"));
        Assert.Contains("ExItS.Web.UI", org, StringComparison.Ordinal);
        Assert.Contains("ExItS.Web.UI", personal, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_web_does_not_reference_infrastructure_or_ef()
    {
        var csproj = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Platform", "ExItS.Personal.Web", "ExItS.Personal.Web.csproj"));
        Assert.DoesNotContain("Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExItS.Platform.Admin", csproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solution_contains_three_web_hosts()
    {
        var slnx = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ExItS.slnx"));
        Assert.Contains("ExItS.Platform.Admin.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.PinoyBusinessPOS.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.Personal.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.Web.UI.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_includes_personal_web_port_8094()
    {
        var root = FindRepositoryRoot();
        var start = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        var stack = File.ReadAllText(Path.Combine(root, "tools", "LocalValidation.stack.ps1"));
        var launch = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Properties", "launchSettings.json"));
        Assert.Contains("DefaultPersonalWebPort", stack, StringComparison.Ordinal);
        Assert.Contains("8094", stack, StringComparison.Ordinal);
        Assert.Contains("http://0.0.0.0:8094", launch, StringComparison.Ordinal);
        Assert.Contains("ExItS.Personal.Web", start, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8094", start, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_web_routes_exist()
    {
        var pages = Path.Combine(FindRepositoryRoot(), "src", "Platform", "ExItS.Personal.Web", "Components", "Pages");
        var combined = string.Join('\n', Directory.GetFiles(pages, "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("@page \"/home\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/utang/people\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/start-business\"", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Canonical_login_and_quick_login_are_admin_hosted()
    {
        var root = FindRepositoryRoot();
        var adminProgram = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs"));
        Assert.Contains("/admin/login/credentials", adminProgram, StringComparison.Ordinal);
        Assert.Contains("/admin/login/as/{key}", adminProgram, StringComparison.Ordinal);
        Assert.Contains("env.IsProduction()", adminProgram, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", adminProgram, StringComparison.Ordinal);
        Assert.Contains("appFromIdentity", adminProgram, StringComparison.Ordinal);
        Assert.Contains("WebApps.Organization", adminProgram, StringComparison.Ordinal);
        Assert.Contains("WebApps.Personal", adminProgram, StringComparison.Ordinal);
        Assert.Contains("selected?.OrganizationId", adminProgram, StringComparison.Ordinal);

        var orgProgram = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "Program.cs"));
        var personalProgram = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Program.cs"));
        Assert.Contains("CanonicalLoginUrl", orgProgram, StringComparison.Ordinal);
        Assert.Contains("CanonicalLoginUrl", personalProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/login/as/", orgProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/login/as/", personalProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_web_ui_owns_theme_and_safe_return_path()
    {
        var root = FindRepositoryRoot();
        var theme = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.Web.UI", "ExitsWebThemeService.cs"));
        var safe = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.Web.UI", "ExItSWebHostOptions.cs"));
        var boot = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.Web.UI", "wwwroot", "theme-boot.js"));
        Assert.Contains("exits-web-theme", theme, StringComparison.Ordinal);
        Assert.Contains("Light", theme, StringComparison.Ordinal);
        Assert.Contains("Dark", theme, StringComparison.Ordinal);
        Assert.Contains("System", theme, StringComparison.Ordinal);
        Assert.Contains("SafeReturnPath", safe, StringComparison.Ordinal);
        Assert.Contains("PlatformAdmin", safe, StringComparison.Ordinal);
        Assert.Contains("OrganizationWeb", safe, StringComparison.Ordinal);
        Assert.Contains("PersonalWeb", safe, StringComparison.Ordinal);
        Assert.Contains("exits-web-theme", boot, StringComparison.Ordinal);

        var handoff = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.Web.UI", "WebHandoffHttp.cs"));
        Assert.Contains("/session/establish?ticket=", handoff, StringComparison.Ordinal);
        Assert.DoesNotContain("password", handoff, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SharedPassword", handoff, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Org_web_routes_exist_and_have_no_checkout()
    {
        var pages = Path.Combine(FindRepositoryRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web");
        var combined = string.Join('\n', Directory.GetFiles(pages, "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("@page \"/overview\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/products\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/inventory\"", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutAsync", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("/checkout", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string text, string token)
    {
        var n = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            n++;
            index += token.Length;
        }

        return n;
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
