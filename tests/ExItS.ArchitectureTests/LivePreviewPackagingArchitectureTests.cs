namespace ExItS.ArchitectureTests;

public sealed class LivePreviewPackagingArchitectureTests
{
    [Fact]
    public void Live_preview_compose_is_separate_project_with_admin_and_distinct_ports()
    {
        var root = FindRepoRoot();
        var livePath = Path.Combine(root, "deploy", "docker", "compose.live-preview.yaml");
        var packagingPath = Path.Combine(root, "deploy", "docker", "compose.yaml");
        Assert.True(File.Exists(livePath));
        Assert.True(File.Exists(packagingPath));

        var live = File.ReadAllText(livePath);
        var packaging = File.ReadAllText(packagingPath);

        Assert.Contains("name: exits-live-preview", live, StringComparison.Ordinal);
        Assert.Contains("name: exits-packaging", packaging, StringComparison.Ordinal);
        Assert.Contains("admin-web:", live, StringComparison.Ordinal);
        Assert.Contains("exits-live-preview-admin-web", live, StringComparison.Ordinal);
        Assert.Contains("exits_live_preview_platform_db_data", live, StringComparison.Ordinal);
        Assert.Contains("exits_live_preview_pos_db_data", live, StringComparison.Ordinal);
        Assert.Contains("NOT Production", live, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Staging", live, StringComparison.Ordinal);
        Assert.Contains("LivePreview__Enabled", live, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare/", live, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".Migrate(", live, StringComparison.Ordinal);

        Assert.Contains("${LIVE_PREVIEW_ADMIN_HOST_PORT:-8090}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LIVE_PREVIEW_PLATFORM_API_HOST_PORT:-8091}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LIVE_PREVIEW_POS_API_HOST_PORT:-8092}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LIVE_PREVIEW_PLATFORM_DB_HOST_PORT:-15533}:5432", live, StringComparison.Ordinal);
        Assert.Contains("${LIVE_PREVIEW_POS_DB_HOST_PORT:-15534}:5432", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT:-8081", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_API_HOST_PORT:-8082", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_DB_HOST_PORT:-15433", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_DB_HOST_PORT:-15434", live, StringComparison.Ordinal);

        Assert.Contains("${PLATFORM_API_HOST_PORT:-8081}:8080", packaging, StringComparison.Ordinal);
        Assert.Contains("${POS_API_HOST_PORT:-8082}:8080", packaging, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-web:", packaging, StringComparison.Ordinal);
    }

    [Fact]
    public void Live_preview_env_example_exists_without_secrets_and_is_not_packaging_env()
    {
        var root = FindRepoRoot();
        var liveEnv = Path.Combine(root, "deploy", "docker", ".env.live-preview.example");
        var packagingEnv = Path.Combine(root, "deploy", "docker", ".env.example");
        Assert.True(File.Exists(liveEnv));
        Assert.True(File.Exists(packagingEnv));

        var text = File.ReadAllText(liveEnv);
        Assert.Contains("REPLACE_LIVE_PREVIEW_PLATFORM_DB_PASSWORD", text, StringComparison.Ordinal);
        Assert.Contains("8090", text, StringComparison.Ordinal);
        Assert.DoesNotContain("exits_platform_dev_only", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT=8081", text, StringComparison.Ordinal);
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
