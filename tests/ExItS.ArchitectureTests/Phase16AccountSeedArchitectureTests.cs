namespace ExItS.ArchitectureTests;

public sealed class Phase16AccountSeedArchitectureTests
{
    [Fact]
    public void Phase16_seed_guards_Production_and_is_not_invoked_from_LocalValidation_host()
    {
        var root = FindRepoRoot();
        var seed = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "Identity", "InitializePhase16AccountSeed.cs"));
        Assert.Contains("must never run in Production", seed, StringComparison.Ordinal);
        Assert.Contains("IsProduction()", seed, StringComparison.Ordinal);
        Assert.Contains("LocalValidation:Enabled=true", seed, StringComparison.Ordinal);

        var hosted = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "LocalValidation", "LocalValidationHostedService.cs"));
        Assert.DoesNotContain("InitializePhase16AccountSeed", hosted, StringComparison.Ordinal);
        Assert.Contains("InitializeLocalValidationDataset", hosted, StringComparison.Ordinal);
        Assert.Contains("IsProduction()", hosted, StringComparison.Ordinal);

        var production = File.ReadAllText(Path.Combine(root, "deploy", "docker", "compose.production.yaml"));
        Assert.DoesNotContain("LocalValidation__Enabled", production, StringComparison.Ordinal);
        Assert.DoesNotContain("phase16-seed", production, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InitializePhase16AccountSeed", production, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_session_is_not_implemented_in_platform_source()
    {
        var root = FindRepoRoot();
        var platformRoot = Path.Combine(root, "src", "Platform");
        foreach (var path in Directory.EnumerateFiles(platformRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            Assert.DoesNotContain("SupportSession", text, StringComparison.Ordinal);
            Assert.DoesNotContain("support_session", text, StringComparison.OrdinalIgnoreCase);
        }
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
