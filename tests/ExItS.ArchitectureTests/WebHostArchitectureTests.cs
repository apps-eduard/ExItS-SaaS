namespace ExItS.ArchitectureTests;

public sealed class WebHostArchitectureTests
{
    [Fact]
    public void Legacy_blazor_frontend_projects_are_absent()
    {
        var root = FindRepositoryRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Shared", "ExItS.Web.UI")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem")));
        Assert.False(Directory.Exists(Path.Combine(root, "tests", "ExItS.Personal.Web.Tests")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.personal-web")));

        var slnx = File.ReadAllText(Path.Combine(root, "ExItS.slnx"));
        Assert.DoesNotContain("ExItS.Personal.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Web.UI.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Platform.Admin.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.DesignSystem.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void AntDesign_and_blazor_component_packages_have_no_active_consumer()
    {
        var root = FindRepositoryRoot();
        var packages = File.ReadAllText(Path.Combine(root, "Directory.Packages.props"));
        Assert.DoesNotContain("AntDesign", packages, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.AspNetCore.Components", packages, StringComparison.Ordinal);

        var hits = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(Path.Combine(root, "tools"), "*.csproj", SearchOption.AllDirectories))
            .Select(File.ReadAllText)
            .Where(text =>
                text.Contains("AntDesign", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Microsoft.NET.Sdk.Razor", StringComparison.Ordinal)
                || text.Contains("AddRazorComponents", StringComparison.Ordinal)
                || text.Contains("MapRazorComponents", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(hits);

        var razor = Directory.GetFiles(Path.Combine(root, "src"), "*.razor", SearchOption.AllDirectories);
        Assert.Empty(razor);
    }

    [Fact]
    public void Personal_port_8094_is_retired()
    {
        var root = FindRepositoryRoot();
        var start = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        var stack = File.ReadAllText(Path.Combine(root, "tools", "LocalValidation.stack.ps1"));
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "docker", "compose.local-validation.yaml"));
        Assert.DoesNotContain("DefaultPersonalWebPort", stack, StringComparison.Ordinal);
        Assert.DoesNotContain("8094", stack, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.Personal.Web", start, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalPort 8094", start, StringComparison.Ordinal);
        Assert.DoesNotContain("personal-web:", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("8094", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void React_client_owns_personal_organization_and_session_establish()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.React", "src", "app", "router.tsx"));
        Assert.Contains("path: \"personal\"", router, StringComparison.Ordinal);
        Assert.Contains("path: \"org\"", router, StringComparison.Ordinal);
        Assert.Contains("SessionEstablishPage", router, StringComparison.Ordinal);
        Assert.Contains("path: \"start-business\"", router, StringComparison.Ordinal);

        var chooser = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin.Web", "src", "lib", "auth", "cutover-routing.ts"));
        Assert.Contains("http://127.0.0.1:5177", chooser, StringComparison.Ordinal);
        Assert.Contains("\"/personal\"", chooser, StringComparison.Ordinal);
        Assert.DoesNotContain("8094", chooser, StringComparison.Ordinal);
        Assert.DoesNotContain("8093", chooser, StringComparison.Ordinal);
        Assert.DoesNotContain("8090", chooser, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_web_routes_exist_and_have_no_checkout()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.React", "src", "app", "router.tsx"));
        var org = router.IndexOf("path: \"org\"", StringComparison.Ordinal);
        Assert.True(org >= 0);
        var orgTree = router[org..];
        Assert.DoesNotContain("path: \"checkout\"", orgTree, StringComparison.Ordinal);
        Assert.Contains("path: \"sales-documents\"", orgTree, StringComparison.Ordinal);
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
