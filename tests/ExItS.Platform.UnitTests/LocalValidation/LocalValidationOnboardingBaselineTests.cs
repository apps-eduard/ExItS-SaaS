using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Authorization;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class LocalValidationOnboardingBaselineTests
{
    [Fact]
    public void Onboarding_baseline_catalog_has_exactly_two_platform_administrators()
    {
        var identities = LocalValidationIdentityCatalog.PlatformAdministratorsOnly;
        Assert.Equal(2, identities.Count);
        Assert.All(identities, i => Assert.Equal(PlatformSystemRole.PlatformAdministrator, i.AssignPlatformRole));
        Assert.Contains(identities, i => i.Key == "olivia-mendoza" && i.Summary.Contains("Primary", StringComparison.Ordinal));
        Assert.Contains(identities, i => i.Key == "rafael-torres" && i.Summary.Contains("Backup", StringComparison.Ordinal));
    }

    [Fact]
    public void Built_in_platform_roles_include_administrator_and_auditor()
    {
        Assert.Contains(BuiltInPlatformRoleDefinitions.All, r => r.Code == BuiltInPlatformRoleDefinitions.PlatformAdministratorCode);
        Assert.Contains(BuiltInPlatformRoleDefinitions.All, r => r.Code == BuiltInPlatformRoleDefinitions.PlatformAuditorCode);
        Assert.Equal(
            new[] { PlatformPermission.ViewPortfolio, PlatformPermission.ViewAuditRecords }.OrderBy(x => x),
            PlatformRolePermissionCatalog.GetPermissions(PlatformSystemRole.PlatformAuditor).OrderBy(x => x));
    }

    [Fact]
    public void Reset_script_requires_confirm_and_uses_platform_administrators_only()
    {
        var root = FindRepoRoot();
        var reset = File.ReadAllText(Path.Combine(root, "tools", "Reset-LocalValidation.ps1"));
        Assert.Contains("ConfirmReset", reset, StringComparison.Ordinal);
        Assert.Contains("PlatformAdministratorsOnly", reset, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_db_data", reset, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_pos_db_data", reset, StringComparison.Ordinal);
        Assert.Contains("both Platform Administrator", reset, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker compose down -v", reset, StringComparison.Ordinal);
        // seed-identities returns a JSON array; bare $json.items under Stop throws PropertyNotFoundException.
        Assert.Contains("PSObject.Properties.Name -contains 'items'", reset, StringComparison.Ordinal);
        Assert.Contains("$json -is [System.Array]", reset, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_identities_api_uses_seed_scope_not_full_catalog_only()
    {
        var root = FindRepoRoot();
        var auth = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "LocalValidationAuthUseCases.cs"));
        Assert.Contains("IdentitiesForSeedScope(_options.SeedScope)", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var identity in LocalValidationIdentityCatalog.All)", auth, StringComparison.Ordinal);

        var initializer = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "LocalValidation", "InitializeLocalValidationDataset.cs"));
        Assert.Contains("ILocalValidationBaselinePurge", initializer, StringComparison.Ordinal);
        Assert.Contains("PurgeTransactionalDataAsync", initializer, StringComparison.Ordinal);
        Assert.Contains("EnsureBuiltInPlatformRoleDefinitions", initializer, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
