using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Authorization;
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
    public void Catalog_includes_eight_approved_identities_with_abc_and_xyz_organizations()
    {
        var catalog = LocalValidationIdentityCatalog.All;
        var orgs = LocalValidationOrganizationCatalog.All;

        Assert.Equal(8, catalog.Count);
        Assert.Equal(2, orgs.Count);
        Assert.Equal("abc-sari-sari", LocalValidationOrganizationCatalog.AbcSariSariSlug);
        Assert.Equal("xyz-mini-grocery", LocalValidationOrganizationCatalog.XyzMiniGrocerySlug);
        Assert.Equal("ABC Sari-Sari Store", LocalValidationOrganizationCatalog.AbcSariSariDisplayName);
        Assert.Equal("XYZ Mini Grocery", LocalValidationOrganizationCatalog.XyzMiniGroceryDisplayName);

        foreach (var displayName in ApprovedDisplayNames)
        {
            Assert.Contains(catalog, i => i.DisplayName == displayName);
        }

        foreach (var identity in catalog)
        {
            Assert.EndsWith("@exits.local", identity.Email, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@exits.test", identity.Email, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains(catalog, i =>
            i.Key == "olivia-mendoza"
            && i.PreferredAccountClass == AccountClass.Platform
            && i.AssignPlatformRole == PlatformSystemRole.PlatformAdministrator
            && !i.HasOrganizationMembership);

        Assert.Contains(catalog, i =>
            i.Key == "rafael-torres"
            && i.PreferredAccountClass == AccountClass.Platform
            && i.AssignPlatformRole == PlatformSystemRole.PlatformSupport
            && !i.HasOrganizationMembership);

        Assert.Contains(catalog, i =>
            i.Key == "maria-santos"
            && i.PreferredAccountClass == AccountClass.Organization
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.AbcSariSariSlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner");

        Assert.Contains(catalog, i =>
            i.Key == "carlo-reyes"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.AbcSariSariSlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationMember
            && i.PosLocalRoleCode == "Cashier");

        Assert.Contains(catalog, i =>
            i.Key == "ana-cruz"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.XyzMiniGrocerySlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner");

        Assert.Contains(catalog, i =>
            i.Key == "daniel-garcia"
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.XyzMiniGrocerySlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationMember
            && i.PosLocalRoleCode == "Cashier");

        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Platform));
        Assert.Equal(4, catalog.Count(i => i.PreferredAccountClass == AccountClass.Organization));
        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Personal));

        Assert.Equal(2, catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.AbcSariSariSlug, StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(2, catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.XyzMiniGrocerySlug, StringComparison.OrdinalIgnoreCase)));

        Assert.Contains("sampaguita-store", ObsoleteLocalValidationOrganizations.Slugs);
        Assert.Contains("mabuhay-mini-mart", ObsoleteLocalValidationOrganizations.Slugs);
        Assert.Contains("phase16-seed-org", ObsoleteLocalValidationOrganizations.Slugs);
        Assert.Equal(5000m, LocalValidationPersonalUtangSeedMarkers.LuisToSofiaLoan);
        Assert.Equal(1500m, LocalValidationPersonalUtangSeedMarkers.LuisToSofiaPayment);
        Assert.Equal(1000m, LocalValidationPersonalUtangSeedMarkers.SofiaToLuisLoan);
        Assert.Equal(3500m, LocalValidationPersonalUtangSeedMarkers.LuisToSofiaLoan - LocalValidationPersonalUtangSeedMarkers.LuisToSofiaPayment);
    }
}
