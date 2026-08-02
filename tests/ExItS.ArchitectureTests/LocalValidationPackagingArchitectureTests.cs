namespace ExItS.ArchitectureTests;

public sealed class LocalValidationPackagingArchitectureTests
{
    [Fact]
    public void Local_validation_compose_is_separate_project_with_admin_and_distinct_ports()
    {
        var root = FindRepoRoot();
        var livePath = Path.Combine(root, "deploy", "docker", "compose.local-validation.yaml");
        var packagingPath = Path.Combine(root, "deploy", "docker", "compose.yaml");
        Assert.True(File.Exists(livePath));
        Assert.True(File.Exists(packagingPath));

        var live = File.ReadAllText(livePath);
        var packaging = File.ReadAllText(packagingPath);

        Assert.Contains("name: exits-local-validation", live, StringComparison.Ordinal);
        Assert.Contains("name: exits-packaging", packaging, StringComparison.Ordinal);
        Assert.Contains("admin-web:", live, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-admin-web", live, StringComparison.Ordinal);
        Assert.Contains("profiles: [\"apps\"]", live, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_db_data", live, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_pos_db_data", live, StringComparison.Ordinal);
        Assert.Contains("production-equivalent", live, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Staging", live, StringComparison.Ordinal);
        Assert.Contains("LocalValidation__Enabled", live, StringComparison.Ordinal);
        Assert.DoesNotContain("live-preview", live, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LivePreview", live, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare/", live, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".Migrate(", live, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "README.local-validation.md")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "README.local-validation-workflow.md")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Start-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Stop-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Start-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Stop-LocalValidation.ps1")));

        var startScript = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        Assert.Contains("dotnet watch", startScript, StringComparison.Ordinal);
        Assert.Contains("DataProtectionKeys", startScript, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-platform-db", startScript, StringComparison.Ordinal);
        Assert.Contains("volumes preserved", startScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker compose down -v", startScript, StringComparison.Ordinal);

        Assert.Contains("${LOCAL_VALIDATION_ADMIN_HOST_PORT:-8090}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_API_HOST_PORT:-8091}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_API_HOST_PORT:-8092}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT:-15533}:5432", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_DB_HOST_PORT:-15534}:5432", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT:-8081", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_API_HOST_PORT:-8082", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_DB_HOST_PORT:-15433", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_DB_HOST_PORT:-15434", live, StringComparison.Ordinal);

        Assert.Contains("${PLATFORM_API_HOST_PORT:-8081}:8080", packaging, StringComparison.Ordinal);
        Assert.Contains("${POS_API_HOST_PORT:-8082}:8080", packaging, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-web:", packaging, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_env_example_exists_without_secrets_and_is_not_packaging_env()
    {
        var root = FindRepoRoot();
        var liveEnv = Path.Combine(root, "deploy", "docker", ".env.local-validation.example");
        var packagingEnv = Path.Combine(root, "deploy", "docker", ".env.example");
        Assert.True(File.Exists(liveEnv));
        Assert.True(File.Exists(packagingEnv));

        var text = File.ReadAllText(liveEnv);
        Assert.Contains("REPLACE_LOCAL_VALIDATION_PLATFORM_DB_PASSWORD", text, StringComparison.Ordinal);
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
