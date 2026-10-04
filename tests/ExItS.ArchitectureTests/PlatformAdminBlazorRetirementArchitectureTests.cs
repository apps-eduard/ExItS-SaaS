namespace ExItS.ArchitectureTests;

/// <summary>
/// React Platform Admin is the only Platform Administration frontend.
/// The legacy Blazor host is removed.
/// </summary>
public sealed class PlatformAdminBlazorRetirementArchitectureTests
{
    [Fact]
    public void Legacy_blazor_platform_admin_project_is_removed()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin")));
        Assert.False(File.Exists(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj")));
        Assert.False(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.platform-admin")));
        Assert.False(Directory.Exists(Path.Combine(root, "tests", "ExItS.Platform.Admin.UnitTests")));
        var slnx = File.ReadAllText(Path.Combine(root, "ExItS.slnx"));
        Assert.DoesNotContain("src/Platform/ExItS.Platform.Admin/", slnx, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_has_one_react_admin_service_on_8095()
    {
        var compose = Read("deploy", "docker", "compose.local-validation.yaml");
        var normalized = compose.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(1, Count(normalized, "\n  admin-web:\n"));
        Assert.DoesNotContain("admin-web-react", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("Dockerfile.platform-admin\n", normalized, StringComparison.Ordinal);
        Assert.Contains("dockerfile: deploy/docker/Dockerfile.platform-admin-web", normalized, StringComparison.Ordinal);
        Assert.Contains("image: exits/platform-admin-web:", normalized, StringComparison.Ordinal);
        Assert.Contains("container_name: exits-local-validation-admin-web", normalized, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_ADMIN_HOST_PORT:-8095}:8080", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain(":8090", normalized, StringComparison.Ordinal);
        Assert.Contains(
            "PlatformEmail__AdminPublicBaseUrl: ${LOCAL_VALIDATION_ADMIN_ORIGIN:-http://127.0.0.1:8095}",
            normalized,
            StringComparison.Ordinal);
        Assert.DoesNotContain("ExItSWebHosts__PlatformAdmin", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void Blazor_hosts_are_removed_and_platform_api_targets_react_admin()
    {
        var platformLaunch = Read("src", "Platform", "ExItS.Platform.Api", "Properties", "launchSettings.json");
        Assert.Contains("8095", platformLaunch, StringComparison.Ordinal);
        Assert.DoesNotContain("8090", platformLaunch, StringComparison.Ordinal);
        Assert.DoesNotContain("8094", platformLaunch, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(FindRepoRoot(), "src", "Platform", "ExItS.Personal.Web")));
        Assert.False(Directory.Exists(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web")));
        Assert.False(File.Exists(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.Web.UI", "ExItSWebForwardedHeaders.cs")));
    }

    [Fact]
    public void Cloudflare_admin_hostname_targets_the_react_admin_service()
    {
        var overlay = Read("deploy", "docker", "compose.cloudflare-preview.yaml");
        Assert.Contains("https://admin.exitsapps.com    -> http://admin-web:8080", overlay, StringComparison.Ordinal);
        Assert.Contains("canonical React Platform Admin", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("\n  admin-web:\n", overlay.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("ForwardedHeaders", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("personal-web:", overlay, StringComparison.Ordinal);
    }

    [Fact]
    public void React_admin_project_keeps_build_and_test_scripts()
    {
        var package = Read("src", "Platform", "ExItS.Platform.Admin.Web", "package.json");
        Assert.Contains("\"typecheck\":", package, StringComparison.Ordinal);
        Assert.Contains("\"test\":", package, StringComparison.Ordinal);
        Assert.Contains("\"build\":", package, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "Platform", "ExItS.Platform.Admin.Web", "src", "app", "App.tsx")));
    }

    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { FindRepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static int Count(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string FindRepoRoot()
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
