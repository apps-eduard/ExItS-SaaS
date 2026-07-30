namespace ExItS.ArchitectureTests;

/// <summary>P9-WP06: Commercial MVP closeout reconciliation guards.</summary>
public sealed class CommercialMvpCloseoutArchitectureTests
{
    [Fact]
    public void Phase_marker_is_p9_wp06_on_apis_and_closeout_library()
    {
        var root = FindRepoRoot();
        var pos = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        var platform = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        var closeout = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.Deployment", "CommercialMvpCloseout.cs"));
        Assert.Contains("P10-WP01-suppliers", pos, StringComparison.Ordinal);
        Assert.Contains("P10-WP01-suppliers", platform, StringComparison.Ordinal);
        Assert.Contains("P9-WP06-commercial-mvp-closeout", closeout, StringComparison.Ordinal);
        Assert.Contains("Phase 10 — Full POS", closeout, StringComparison.Ordinal);
    }

    [Fact]
    public void P9_wp01_security_controls_remain()
    {
        var root = FindRepoRoot();
        var platformPipeline = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformSecurityPipeline.cs"));
        var posGuard = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Common", "PosDevelopmentEnvironment.cs"));
        Assert.Contains("ValidateProductionConfigurationOrThrow", platformPipeline, StringComparison.Ordinal);
        Assert.Contains("exits_platform_dev_only", platformPipeline, StringComparison.Ordinal);
        Assert.Contains("AddRateLimiter", platformPipeline, StringComparison.Ordinal);
        Assert.Contains("PosProductionSecurityGuard", posGuard, StringComparison.Ordinal);
        var platformAppsettings = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "appsettings.json"));
        Assert.DoesNotContain("exits_platform_dev_only", platformAppsettings, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P9_wp02_health_ready_and_no_migrate_at_startup()
    {
        var root = FindRepoRoot();
        var platform = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        var pos = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        Assert.Contains("MapPlatformHealthEndpoints", platform, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", platform, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", pos, StringComparison.Ordinal);
    }

    [Fact]
    public void P9_wp03_backup_and_p9_wp05_pilot_assets_remain_non_production()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "ops", "backup", "Backup-ExItsDatabase.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops", "deploy", "Invoke-ExItsDeploy.ps1")));
        var compose = File.ReadAllText(Path.Combine(root, "deploy", "docker", "docker-compose.pilot.yml"));
        Assert.Contains("NON-PRODUCTION", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare", compose, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closeout_does_not_introduce_tax_payroll_or_gateway_surface()
    {
        var closeout = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.Deployment", "CommercialMvpCloseout.cs"));
        Assert.Contains("DeferredEnhancement", closeout, StringComparison.Ordinal);
        Assert.Contains("TAX-REFUND-ACCT", closeout, StringComparison.Ordinal);
        Assert.DoesNotContain("ImplementPayroll", closeout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PaymentGatewayClient", closeout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Database_ownership_constants_match_approved_split()
    {
        var closeout = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.Deployment", "CommercialMvpCloseout.cs"));
        Assert.Contains("ExItS_Platform", closeout, StringComparison.Ordinal);
        Assert.Contains("ExItS_PinoyBusinessPOS", closeout, StringComparison.Ordinal);
        Assert.Contains("ForbidsHealthCareCoupling", closeout, StringComparison.Ordinal);
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
