namespace ExItS.ArchitectureTests;

public sealed class Phase16AccountSeedArchitectureTests
{
    [Fact]
    public void Phase16_seed_guards_Production_and_is_invoked_only_from_non_production_LivePreview_host()
    {
        var root = FindRepoRoot();
        var seed = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "Identity", "InitializePhase16AccountSeed.cs"));
        Assert.Contains("must never run in Production", seed, StringComparison.Ordinal);
        Assert.Contains("IsProduction()", seed, StringComparison.Ordinal);

        var hosted = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "LivePreview", "LivePreviewHostedService.cs"));
        Assert.Contains("InitializePhase16AccountSeed", hosted, StringComparison.Ordinal);
        Assert.Contains("IsProduction()", hosted, StringComparison.Ordinal);

        var production = File.ReadAllText(Path.Combine(root, "deploy", "docker", "compose.production.yaml"));
        Assert.DoesNotContain("LivePreview__Enabled", production, StringComparison.Ordinal);
        Assert.DoesNotContain("phase16-seed", production, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InitializePhase16AccountSeed", production, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS-SaaS.sln"))
                || File.Exists(Path.Combine(dir.FullName, "ExItS.sln"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "Platform")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
