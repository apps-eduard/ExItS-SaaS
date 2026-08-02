using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class LocalValidationIdentityCatalogTests
{
    private static readonly string[] ApprovedDisplayNames =
    [
        "Olivia Mendoza",
        "Rafael Torres",
        "Maria Santos",
        "Carlo Reyes",
        "Ana Cruz",
        "Daniel Garcia",
        "Luis Navarro",
        "Sofia Ramos"
    ];

    [Fact]
    public void Catalog_includes_eight_approved_identities_with_two_organizations()
    {
        var catalog = LocalValidationIdentityCatalog.All;
        var orgs = LocalValidationOrganizationCatalog.All;

        Assert.Equal(8, catalog.Count);
        Assert.Equal(2, orgs.Count);

        foreach (var displayName in ApprovedDisplayNames)
        {
            Assert.Contains(catalog, i => i.DisplayName == displayName);
        }

        foreach (var identity in catalog)
        {
            Assert.EndsWith("@exits.local", identity.Email, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("validation-", identity.Username, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("validation-", identity.DisplayName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("validation-", identity.Email, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains(catalog, i => i.Key == "olivia-mendoza" && i.AssignPlatformAdministrator && i.PreferredAccountClass == AccountClass.Platform);
        Assert.Contains(catalog, i =>
            i.Key == "rafael-torres"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.SampaguitaSlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner");
        Assert.Contains(catalog, i =>
            i.Key == "maria-santos"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.SampaguitaSlug
            && i.PosLocalRoleCode == "Cashier");
        Assert.Contains(catalog, i =>
            i.Key == "carlo-reyes"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.MabuhaySlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner");
        Assert.Contains(catalog, i =>
            i.Key == "ana-cruz"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.MabuhaySlug
            && !i.GrantPosProductAccess);
        Assert.Contains(catalog, i => i.Key == "daniel-garcia" && !i.AssignPlatformAdministrator && i.PreferredAccountClass == AccountClass.Platform);
        Assert.Contains(catalog, i => i.Key == "luis-navarro" && i.PreferredAccountClass == AccountClass.Personal && !i.HasOrganizationMembership);
        Assert.Contains(catalog, i => i.Key == "sofia-ramos" && i.PreferredAccountClass == AccountClass.Personal && !i.HasOrganizationMembership);

        var sampaguitaUsers = catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.SampaguitaSlug, StringComparison.OrdinalIgnoreCase));
        var mabuhayUsers = catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.MabuhaySlug, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, sampaguitaUsers);
        Assert.Equal(2, mabuhayUsers);

        Assert.Equal("sampaguita-store", LocalValidationOptions.OrgSlug);
        Assert.Equal("Sampaguita Neighborhood Store", LocalValidationOptions.OrgDisplayName);
        Assert.Equal("Sampaguita POS Trial", LocalValidationOptions.TrialDisplayName);
        Assert.DoesNotContain("validation-", LocalValidationOptions.ProductPlanCode, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validation-", LocalValidationOptions.ProductPlanDisplayName, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(LocalValidationIdentityCatalog.FindByKey("RAFAEL-TORRES"));
        Assert.Null(LocalValidationIdentityCatalog.FindByKey("missing"));
        Assert.NotNull(LocalValidationOrganizationCatalog.FindBySlug("MABUHAY-MINI-MART"));
    }
}
