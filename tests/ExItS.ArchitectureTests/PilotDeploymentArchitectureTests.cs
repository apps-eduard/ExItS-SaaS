namespace ExItS.ArchitectureTests;

public sealed class PilotDeploymentArchitectureTests
{
    [Fact]
    public void Deployment_library_phase_marker_is_p9_wp05()
    {
        var source = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.Deployment", "DeploymentCore.cs"));
        Assert.Contains("P10-WP08-phase-10-closeout", source, StringComparison.Ordinal);
        Assert.Contains("DEPLOY_PRODUCTION_CONFIRMED", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Pos_and_platform_program_declare_p9_wp05_phase_marker()
    {
        var pos = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        var platform = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        Assert.Contains("P10-WP08-phase-10-closeout", pos, StringComparison.Ordinal);
        Assert.Contains("P10-WP08-phase-10-closeout", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Pilot_compose_and_dockerfiles_exist_without_healthcare()
    {
        var root = FindRepoRoot();
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "docker", "docker-compose.pilot.yml"));
        Assert.Contains("exits-pilot", compose, StringComparison.Ordinal);
        Assert.Contains("NON-PRODUCTION", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare", compose, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.platform-api")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.pos-api")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.platform-admin")));
    }

    [Fact]
    public void Deploy_ops_scripts_and_templates_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "ops", "deploy", "Invoke-ExItsDeploy.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops", "deploy", "Invoke-ExItsSmoke.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops", "deploy", "templates", "pilot.env.example")));
        Assert.True(File.Exists(Path.Combine(root, "ops", "deploy", "templates", "production.env.example")));
    }

    [Fact]
    public void No_migrate_at_api_startup()
    {
        var root = FindRepoRoot();
        var pos = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        var platform = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        Assert.DoesNotContain(".Migrate(", pos, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", pos, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", platform, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", platform, StringComparison.Ordinal);
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
