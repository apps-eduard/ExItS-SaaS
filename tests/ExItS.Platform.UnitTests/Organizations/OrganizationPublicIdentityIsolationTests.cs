using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

/// <summary>Leak-prevention for public org identity and Personal identity boundaries.</summary>
public sealed class OrganizationPublicIdentityIsolationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Non_member_cannot_read_organization_public_identity()
    {
        var clock = new FixedClock(T0);
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();

        var ownerA = PlatformUser.Create("ownera", "Owner A", "ownera@example.com", T0);
        var outsider = PlatformUser.Create("outsider", "Outsider", "out@example.com", T0);
        await users.AddAsync(ownerA);
        await users.AddAsync(outsider);

        var orgA = PlatformOrganization.Create("Org A", "org-a", T0);
        orgA.AssignPublicOrganizationId("ORG000001", T0);
        await orgs.AddAsync(orgA);
        await memberships.AddAsync(
            OrganizationMembership.Create(orgA.Id, ownerA.Id, OrganizationRole.OrganizationOwner, T0));

        var useCase = new GetOrganizationPublicIdentity(orgs, memberships);
        var denied = await useCase.ExecuteAsync(outsider.Id, orgA.Id);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.MembershipNotFound, denied.ErrorCode);

        var allowed = await useCase.ExecuteAsync(ownerA.Id, orgA.Id);
        Assert.True(allowed.IsSuccess);
        Assert.Equal("ORG000001", allowed.Value!.PublicOrganizationId);
        Assert.Equal("exits://qr/v1/organization/ORG000001", allowed.Value.QrPayload);
        Assert.Equal("Org A", allowed.Value.DisplayName);

        // Public identity must not leak contact / address fields.
        var dtoType = typeof(OrganizationPublicIdentityDto);
        Assert.Null(dtoType.GetProperty("ContactEmail"));
        Assert.Null(dtoType.GetProperty("ContactPhone"));
        Assert.Null(dtoType.GetProperty("AddressLine1"));
        Assert.Null(dtoType.GetProperty("LegalName"));
        Assert.Equal(3, dtoType.GetProperties().Length);
    }

    [Fact]
    public async Task Public_identity_excludes_contact_even_when_profile_has_contact()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();

        var owner = PlatformUser.Create("ownera", "Owner A", "ownera@example.com", T0);
        var org = PlatformOrganization.Create("Org A", "org-a-contact", T0);
        org.AssignPublicOrganizationId("ORG000099", T0);
        org.UpdateProfile(
            OrganizationProfile.Create(
                "Secret Legal",
                "secret@org.example",
                "+639179999999",
                "Hidden St",
                null,
                "Manila",
                "NCR",
                "1000",
                "PH",
                null,
                null,
                null),
            T0);
        await orgs.AddAsync(org);
        await memberships.AddAsync(
            OrganizationMembership.Create(org.Id, owner.Id, OrganizationRole.OrganizationOwner, T0));

        var useCase = new GetOrganizationPublicIdentity(orgs, memberships);
        var allowed = await useCase.ExecuteAsync(owner.Id, org.Id);
        Assert.True(allowed.IsSuccess);
        Assert.Equal("ORG000099", allowed.Value!.PublicOrganizationId);
        Assert.Equal("Org A", allowed.Value.DisplayName);
        Assert.DoesNotContain("secret", allowed.Value.QrPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hidden", allowed.Value.DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Personal_get_public_identity_is_self_scoped_only()
    {
        var clock = new FixedClock(T0);
        var users = new InMemoryPlatformUserRepository();
        var uow = new NoOpUnitOfWork();
        var audit = new NoOpAuditWriter();
        var generator = new SequentialPublicUserIdGenerator();

        var userA = PlatformUser.Create("usera", "User A", "a@example.com", T0);
        var userB = PlatformUser.Create("userb", "User B", "b@example.com", T0);
        await users.AddAsync(userA);
        await users.AddAsync(userB);

        var get = new GetOrAssignPublicIdentity(users, generator, uow, clock, audit);
        var a = await get.ExecuteAsync(userA.Id);
        var b = await get.ExecuteAsync(userB.Id);
        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);
        Assert.NotEqual(a.Value!.PublicUserId, b.Value!.PublicUserId);

        // Re-fetch A does not mutate B.
        var aAgain = await get.ExecuteAsync(userA.Id);
        Assert.Equal(a.Value.PublicUserId, aAgain.Value!.PublicUserId);
        Assert.Equal(b.Value.PublicUserId, (await get.ExecuteAsync(userB.Id)).Value!.PublicUserId);
    }
}
