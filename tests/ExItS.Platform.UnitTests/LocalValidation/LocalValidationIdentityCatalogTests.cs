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
    public void Catalog_includes_eight_approved_identities_with_single_scope_each()
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
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.SampaguitaSlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner"
            && i.AssignPlatformRole is null);

        Assert.Contains(catalog, i =>
            i.Key == "carlo-reyes"
            && i.PreferredAccountClass == AccountClass.Organization
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.SampaguitaSlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationMember
            && i.PosLocalRoleCode == "Cashier");

        Assert.Contains(catalog, i =>
            i.Key == "ana-cruz"
            && i.PreferredAccountClass == AccountClass.Organization
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.MabuhaySlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationOwner
            && i.PosLocalRoleCode == "Owner");

        Assert.Contains(catalog, i =>
            i.Key == "daniel-garcia"
            && i.PreferredAccountClass == AccountClass.Organization
            && i.OrganizationSlug == LocalValidationOrganizationCatalog.MabuhaySlug
            && i.OrganizationRole == OrganizationMembershipValidationRole.OrganizationMember
            && i.PosLocalRoleCode == "Cashier"
            && i.AssignPlatformRole is null);

        Assert.Contains(catalog, i =>
            i.Key == "luis-navarro"
            && i.PreferredAccountClass == AccountClass.Personal
            && !i.HasOrganizationMembership
            && i.AssignPlatformRole is null);

        Assert.Contains(catalog, i =>
            i.Key == "sofia-ramos"
            && i.PreferredAccountClass == AccountClass.Personal
            && !i.HasOrganizationMembership
            && i.AssignPlatformRole is null);

        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Platform));
        Assert.Equal(4, catalog.Count(i => i.PreferredAccountClass == AccountClass.Organization));
        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Personal));

        var sampaguitaUsers = catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.SampaguitaSlug, StringComparison.OrdinalIgnoreCase));
        var mabuhayUsers = catalog.Count(i =>
            string.Equals(i.OrganizationSlug, LocalValidationOrganizationCatalog.MabuhaySlug, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, sampaguitaUsers);
        Assert.Equal(2, mabuhayUsers);

        Assert.Equal(
            ["platform.admin1@exits.test", "platform.admin2@exits.test", "org.seed.owner@exits.test", "personal.user1@exits.test", "personal.user2@exits.test"],
            ObsoletePhase16SeedIdentities.NormalizedEmails.ToArray());
        Assert.Equal("phase16-seed-org", ObsoletePhase16SeedIdentities.SeedOrgSlug);
    }

    [Theory]
    [InlineData("olivia-mendoza", AccountClass.Platform)]
    [InlineData("rafael-torres", AccountClass.Platform)]
    [InlineData("maria-santos", AccountClass.Organization)]
    [InlineData("carlo-reyes", AccountClass.Organization)]
    [InlineData("ana-cruz", AccountClass.Organization)]
    [InlineData("daniel-garcia", AccountClass.Organization)]
    [InlineData("luis-navarro", AccountClass.Personal)]
    [InlineData("sofia-ramos", AccountClass.Personal)]
    public void Approved_identity_has_exactly_one_preferred_account_class(string key, AccountClass expected)
    {
        var identity = LocalValidationIdentityCatalog.FindByKey(key);
        Assert.NotNull(identity);
        Assert.Equal(expected, identity!.PreferredAccountClass);
        Assert.False(
            identity.PreferredAccountClass == AccountClass.Personal && identity.HasOrganizationMembership);
        Assert.False(
            identity.PreferredAccountClass == AccountClass.Personal && identity.AssignPlatformRole is not null);
        Assert.False(
            identity.PreferredAccountClass == AccountClass.Platform && identity.HasOrganizationMembership);
        Assert.False(
            identity.PreferredAccountClass == AccountClass.Organization && identity.AssignPlatformRole is not null);
    }
}
