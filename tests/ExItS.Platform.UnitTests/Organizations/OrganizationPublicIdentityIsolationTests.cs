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
