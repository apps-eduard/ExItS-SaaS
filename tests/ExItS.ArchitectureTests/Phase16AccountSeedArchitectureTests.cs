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
    public void Local_validation_seed_removes_obsolete_phase16_identities_and_uses_exclusive_profiles()
    {
        var root = FindRepoRoot();
        var init = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "InitializeLocalValidationDataset.cs"));
        Assert.Contains("CleanupObsoleteSeedAsync", init, StringComparison.Ordinal);
        Assert.Contains("CloseObsoleteOrganizationsAsync", init, StringComparison.Ordinal);
        Assert.Contains("ObsoleteLocalValidationOrganizations", init, StringComparison.Ordinal);
        Assert.Contains("ObsoletePhase16SeedIdentities", init, StringComparison.Ordinal);
        Assert.Contains("exclusivePreferredClass: true", init, StringComparison.Ordinal);
        Assert.Contains("InitializeLocalValidationPersonalUtangSeed", init, StringComparison.Ordinal);

        var options = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "LocalValidationOptions.cs"));
        Assert.Contains("platform.admin1@exits.test", options, StringComparison.Ordinal);
        Assert.Contains("personal.user2@exits.test", options, StringComparison.Ordinal);
        Assert.Contains("phase16-seed-org", options, StringComparison.Ordinal);
        Assert.Contains("abc-sari-sari", options, StringComparison.Ordinal);
        Assert.Contains("xyz-mini-grocery", options, StringComparison.Ordinal);
        Assert.Contains("sampaguita-store", options, StringComparison.Ordinal);
        Assert.Contains("AssignPlatformRole", options, StringComparison.Ordinal);
        Assert.Contains("rafael.torres@exits.local", options, StringComparison.Ordinal);
        Assert.Contains("PlatformSupport", options, StringComparison.Ordinal);
        Assert.Contains("local-validation:luis-lends-sofia", options, StringComparison.Ordinal);

        var ensure = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "Identity", "AccountProfileUseCases.cs"));
        Assert.Contains("exclusivePreferredClass", ensure, StringComparison.Ordinal);
        Assert.Contains("automatic Personal companion", ensure, StringComparison.OrdinalIgnoreCase);

        var repo = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "Persistence", "Repositories", "PlatformUserRepository.cs"));
        Assert.Contains("UserDirectoryFilter.Personal", repo, StringComparison.Ordinal);
        Assert.Contains("accounttype", repo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("updatedutc", repo, StringComparison.OrdinalIgnoreCase);

        var usersPage = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Users.razor"));
        Assert.Contains("@page \"/admin/users/personal\"", usersPage, StringComparison.Ordinal);
        Assert.Contains("OnUsersTableChangeAsync", usersPage, StringComparison.Ordinal);
        Assert.Contains("sortBy", usersPage, StringComparison.Ordinal);
        Assert.Contains("ActionColumn", usersPage, StringComparison.Ordinal);
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
