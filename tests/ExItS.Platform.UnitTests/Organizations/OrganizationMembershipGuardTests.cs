using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationMembershipGuardTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Protects_final_governing_admin_from_revoke()
    {
        var repo = new InMemoryOrganizationMembershipRepository();
        var orgId = PlatformOrganizationId.New();
        var sole = OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationOwner, T0);
        await repo.AddAsync(sole);

        var blocked = await OrganizationMembershipGuard.EnsureCanRemoveGoverningSeatAsync(repo, sole);
        Assert.NotNull(blocked);
        Assert.Equal(DomainErrorCodes.LastGoverningAdminProtected, blocked!.ErrorCode);
    }

    [Fact]
    public async Task OrganizationAdministrator_cannot_assign_owner()
    {
        var repo = new InMemoryOrganizationMembershipRepository();
        var orgId = PlatformOrganizationId.New();
        var member = OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationMember, T0);
        await repo.AddAsync(member);
        await repo.AddAsync(OrganizationMembership.Create(orgId, PlatformUserId.New(), OrganizationRole.OrganizationOwner, T0));

        var blocked = await OrganizationMembershipGuard.EnsureCanChangeRoleAsync(
            repo,
            member,
            OrganizationRole.OrganizationOwner,
            OrganizationRole.OrganizationAdministrator,
            actorHasPlatformManageMemberships: false);
        Assert.NotNull(blocked);
        Assert.Equal(DomainErrorCodes.OrganizationOwnerAssignmentDenied, blocked!.ErrorCode);
    }
}
