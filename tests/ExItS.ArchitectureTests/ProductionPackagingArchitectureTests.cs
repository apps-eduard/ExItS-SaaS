namespace ExItS.ArchitectureTests;

public sealed class ProductionPackagingArchitectureTests
{
    [Fact]
    public void Packaging_compose_baseline_separates_platform_and_pos_databases()
    {
        var root = FindRepoRoot();
        var composePath = Path.Combine(root, "deploy", "docker", "compose.yaml");
        Assert.True(File.Exists(composePath));
        var compose = File.ReadAllText(composePath);

        Assert.Contains("P14-WP02", compose, StringComparison.Ordinal);
        Assert.Contains("NOT a Production cutover", compose, StringComparison.Ordinal);
        Assert.Contains("platform-db:", compose, StringComparison.Ordinal);
        Assert.Contains("pos-db:", compose, StringComparison.Ordinal);
        Assert.Contains("platform-api:", compose, StringComparison.Ordinal);
        Assert.Contains("pos-api:", compose, StringComparison.Ordinal);
        Assert.Contains("exits_platform", compose, StringComparison.Ordinal);
        Assert.Contains("exits_pos", compose, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Staging", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare/", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".Migrate(", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("MigrateAsync(", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Packaging_compose_does_not_silently_reuse_pilot_filename_as_production()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "docker-compose.pilot.yml")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "compose.yaml")));
        var pilot = File.ReadAllText(Path.Combine(root, "deploy", "docker", "docker-compose.pilot.yml"));
        Assert.Contains("NON-PRODUCTION", pilot, StringComparison.Ordinal);
    }

    [Fact]
    public void Packaging_env_example_and_readme_exist_without_secrets()
    {
        var root = FindRepoRoot();
        var envExample = File.ReadAllText(Path.Combine(root, "deploy", "docker", ".env.example"));
        Assert.Contains("REPLACE_PLATFORM_DB_PASSWORD", envExample, StringComparison.Ordinal);
        Assert.DoesNotContain("exits_platform_dev_only", envExample, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "README.md")));
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
