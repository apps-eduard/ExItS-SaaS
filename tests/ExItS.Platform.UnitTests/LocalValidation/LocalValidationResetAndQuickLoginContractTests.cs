using ExItS.Platform.Application.LocalValidation;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class LocalValidationResetAndQuickLoginContractTests
{
    [Fact]
    public void Quick_login_is_database_backed_and_labels_canonical_baseline()
    {
        var auth = Read("src", "Platform", "ExItS.Platform.Application", "LocalValidation", "LocalValidationAuthUseCases.cs");
        Assert.Contains("class ListLocalValidationQuickLoginIdentities", auth, StringComparison.Ordinal);
        Assert.Contains("_users", auth, StringComparison.Ordinal);
        Assert.Contains("ListAsync", auth, StringComparison.Ordinal);
        Assert.Contains("profile.AccountClass is AccountClass.Platform", auth, StringComparison.Ordinal);
        Assert.Contains("profile.AccountClass is AccountClass.Personal", auth, StringComparison.Ordinal);
        Assert.Contains("profile.AccountClass is not AccountClass.Organization", auth, StringComparison.Ordinal);
        Assert.Contains("IsCanonicalBaselineEmail", auth, StringComparison.Ordinal);
        Assert.Contains("Baseline · ", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalValidationIdentityCatalog.All", auth, StringComparison.Ordinal);
        Assert.Contains("isProductionEnvironment || !_options.Enabled", auth, StringComparison.Ordinal);
    }

    [Fact]
    public void Seed_lookup_uses_username_not_display_name()
    {
        var initializer = Read("src", "Platform", "ExItS.Platform.Application", "LocalValidation", "InitializeLocalValidationDataset.cs");
        Assert.Contains("GetByNormalizedUsernameAsync", initializer, StringComparison.Ordinal);
        Assert.Contains("identity.Username", initializer, StringComparison.Ordinal);
        Assert.Contains("FindActiveStaffByHomeOrgAndContactEmailAsync", initializer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetByDisplayName", initializer, StringComparison.Ordinal);
        Assert.Contains("ReconcileNonBaselineFixtureIdentitiesAsync", initializer, StringComparison.Ordinal);
        Assert.Contains("DecommissionObsoleteUserAsync", initializer, StringComparison.Ordinal);
        Assert.Contains("FullCatalogExceptBaseline", initializer, StringComparison.Ordinal);
    }

    [Fact]
    public void Administrators_only_seed_decommissions_full_catalog_fixtures()
    {
        var initializer = Read("src", "Platform", "ExItS.Platform.Application", "LocalValidation", "InitializeLocalValidationDataset.cs");
        Assert.Contains("isPlatformAdministratorsOnly", initializer, StringComparison.Ordinal);
        Assert.Contains("closeCatalogDemoOrgs: !isFullSeed", initializer, StringComparison.Ordinal);
        Assert.Contains("maria.santos", Read("src", "Platform", "ExItS.Platform.Application", "LocalValidation", "LocalValidationOptions.cs"), StringComparison.OrdinalIgnoreCase);
        Assert.False(LocalValidationIdentityCatalog.IsCanonicalBaselineEmail("maria.santos@exits.local"));
        Assert.False(LocalValidationIdentityCatalog.IsCanonicalBaselineEmail("carlo.reyes@exits.local"));
        Assert.Contains("olivia.mendoza@exits.local", LocalValidationIdentityCatalog.PlatformAdministratorsOnly.Select(i => i.Email), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("rafael.torres@exits.local", LocalValidationIdentityCatalog.PlatformAdministratorsOnly.Select(i => i.Email), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Quick_login_and_reset_are_unavailable_in_production()
    {
        var endpoints = Read("src", "Platform", "ExItS.Platform.Api", "LocalValidation", "LocalValidationEndpoints.cs");
        var program = Read("src", "Platform", "ExItS.Platform.Admin", "Program.cs");
        var reset = Read("tools", "Reset-LocalValidation.ps1");
        Assert.Contains("env.IsProduction()", endpoints, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", endpoints, StringComparison.Ordinal);
        Assert.Contains("quick-login-identities", endpoints, StringComparison.Ordinal);
        Assert.Contains("env.IsProduction() || !localValidation.IsAvailable", program, StringComparison.Ordinal);
        Assert.Contains("Results.NotFound()", program, StringComparison.Ordinal);
        Assert.Contains("Unknown Local Validation identity", Read("src", "Platform", "ExItS.Platform.Admin", "Services", "LocalValidationSignInService.cs"), StringComparison.Ordinal);
        Assert.Contains("ConfirmReset", reset, StringComparison.Ordinal);
        Assert.Contains("PlatformAdministratorsOnly", reset, StringComparison.Ordinal);
        Assert.Contains("Production", reset, StringComparison.Ordinal);
    }

    [Fact]
    public void Quick_login_routes_by_authoritative_account_class()
    {
        var program = Read("src", "Platform", "ExItS.Platform.Admin", "Program.cs");
        Assert.Contains("\"Organization\" => WebApps.Organization", program, StringComparison.Ordinal);
        Assert.Contains("\"Personal\" => WebApps.Personal", program, StringComparison.Ordinal);
        Assert.Contains("\"Platform\" => WebApps.Platform", program, StringComparison.Ordinal);
        Assert.Contains("selected?.OrganizationId", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Start_default_seed_scope_is_platform_administrators_only()
    {
        var start = Read("tools", "Start-LocalValidation.ps1");
        Assert.Contains("SeedScope = 'PlatformAdministratorsOnly'", start, StringComparison.Ordinal);
        Assert.Contains("-SeedScope Full", start, StringComparison.Ordinal);
    }

    private static string Read(params string[] relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return File.ReadAllText(Path.Combine(new[] { dir.FullName }.Concat(relative).ToArray()));
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
