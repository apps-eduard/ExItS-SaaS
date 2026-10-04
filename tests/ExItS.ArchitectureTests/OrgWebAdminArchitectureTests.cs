namespace ExItS.ArchitectureTests;

public sealed class OrgWebAdminArchitectureTests
{
    [Fact]
    public void Organization_web_blazor_project_is_removed()
    {
        var root = FindRepositoryRoot();
        Assert.False(Directory.Exists(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web")));
        Assert.False(File.Exists(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web",
            "ExItS.PinoyBusinessPOS.Web.csproj")));
        Assert.False(Directory.Exists(Path.Combine(root, "tests", "ExItS.PinoyBusinessPOS.Web.Tests")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.organization-web")));

        var slnx = File.ReadAllText(Path.Combine(root, "ExItS.slnx"));
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Web.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Web.Tests.csproj", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_does_not_launch_organization_web_or_port_8093()
    {
        var root = FindRepositoryRoot();
        var stack = File.ReadAllText(Path.Combine(root, "tools", "LocalValidation.stack.ps1"));
        var start = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "docker", "compose.local-validation.yaml"));

        Assert.DoesNotContain("org-web:", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("exits-local-validation-org-web", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("${LOCAL_VALIDATION_ORG_WEB_HOST_PORT:-8093}:8080", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("http://localhost:8093", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("http://127.0.0.1:8093", compose, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_APP_ORIGIN:-http://127.0.0.1:5177}", compose, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1:5177", compose, StringComparison.Ordinal);

        Assert.DoesNotContain("DefaultOrgWebPort", stack, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.PinoyBusinessPOS.Web", start, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalPort 8093", start, StringComparison.Ordinal);
        Assert.DoesNotContain("ServiceKey 'org-web'", start, StringComparison.Ordinal);
    }

    [Fact]
    public void React_client_is_the_canonical_organization_experience()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.React", "src", "app", "router.tsx"));
        foreach (var route in new[]
                 {
                     "path: \"profile\"",
                     "path: \"branches\"",
                     "path: \"staff\"",
                     "path: \"subscription\"",
                     "path: \"tax-compliance\"",
                     "path: \"audit\"",
                     "path: \"sales-documents\""
                 })
        {
            Assert.Contains(route, router, StringComparison.Ordinal);
        }

        Assert.Contains("Navigate to=\"/org/tax-compliance\"", router, StringComparison.Ordinal);
        Assert.Contains("Navigate to=\"/org/audit\"", router, StringComparison.Ordinal);
        Assert.Contains("Navigate to=\"/org/sales-documents\"", router, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireSalesDocumentAcknowledgment", router, StringComparison.Ordinal);

        var sell = router.IndexOf("path: \"sell\"", StringComparison.Ordinal);
        var checkout = router.IndexOf("path: \"checkout\"", StringComparison.Ordinal);
        var org = router.IndexOf("path: \"org\"", StringComparison.Ordinal);
        Assert.True(sell >= 0 && checkout > sell && checkout < org);
    }

    [Fact]
    public void Platform_admin_and_personal_web_hand_organization_to_react()
    {
        var root = FindRepositoryRoot();
        var chooser = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin.Web", "src", "lib", "auth", "cutover-routing.ts"));
        Assert.Contains("http://127.0.0.1:5177", chooser, StringComparison.Ordinal);
        Assert.Contains("buildSessionEstablishUrl", chooser, StringComparison.Ordinal);
        Assert.Contains("/session/establish", chooser, StringComparison.Ordinal);
        Assert.DoesNotContain("8093", chooser, StringComparison.Ordinal);
        Assert.DoesNotContain("8094", chooser, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Shared", "ExItS.Web.UI")));
    }

    [Fact]
    public void Design_system_is_removed_with_organization_web()
    {
        var root = FindRepositoryRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem")));
        Assert.False(Directory.Exists(Path.Combine(root, "tests", "ExItS.DesignSystem.Tests")));
        var slnx = File.ReadAllText(Path.Combine(root, "ExItS.slnx"));
        Assert.DoesNotContain("ExItS.DesignSystem.csproj", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("ExItS.DesignSystem.Tests.csproj", slnx, StringComparison.Ordinal);
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
