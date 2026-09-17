using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class B2bBusinessContactListTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_includes_active_staff_even_when_IsBusinessContact_false()
    {
        var orgId = PlatformOrganizationId.New();
        var ownerUser = PlatformUser.Create("paul.owner", "Paul Owner", "paul.owner@example.com", T0);
        var staffUser = PlatformUser.CreateOrganizationStaff(
            username: "ana.staff",
            staffLogin: "ana@ORG000001",
            contactEmail: "ana@example.com",
            homeOrganizationId: orgId,
            displayName: "Ana Staff",
            utcNow: T0);

        var ownerMembership = OrganizationMembership.Create(
            orgId,
            ownerUser.Id,
            OrganizationRole.OrganizationOwner,
            T0);
        var staffMembership = OrganizationMembership.Create(
            orgId,
            staffUser.Id,
            OrganizationRole.OrganizationMember,
            T0);
        Assert.False(staffMembership.IsBusinessContact);

        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(ownerMembership);
        await memberships.AddAsync(staffMembership);

        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(ownerUser);
        await users.AddAsync(staffUser);

        var useCase = new ListOrganizationB2bBusinessContacts(memberships, users);
        var result = await useCase.ExecuteAsync(orgId.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, c => c.IsOwner && c.DisplayName == "Paul Owner");
        Assert.Contains(result.Value, c => !c.IsOwner && c.DisplayName == "Ana Staff");
    }

    [Fact]
    public async Task List_excludes_suspended_memberships()
    {
        var orgId = PlatformOrganizationId.New();
        var ownerUser = PlatformUser.Create("owner2", "Owner Two", "owner2@example.com", T0);
        var staffUser = PlatformUser.CreateOrganizationStaff(
            username: "bob.staff",
            staffLogin: "bob@ORG000002",
            contactEmail: "bob@example.com",
            homeOrganizationId: orgId,
            displayName: "Bob Suspended",
            utcNow: T0);

        var ownerMembership = OrganizationMembership.Create(
            orgId,
            ownerUser.Id,
            OrganizationRole.OrganizationOwner,
            T0);
        var staffMembership = OrganizationMembership.Create(
            orgId,
            staffUser.Id,
            OrganizationRole.OrganizationMember,
            T0);
        staffMembership.Suspend(T0, "leave");

        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(ownerMembership);
        await memberships.AddAsync(staffMembership);

        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(ownerUser);
        await users.AddAsync(staffUser);

        var useCase = new ListOrganizationB2bBusinessContacts(memberships, users);
        var result = await useCase.ExecuteAsync(orgId.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Single(result.Value!);
        Assert.Equal("Owner Two", result.Value[0].DisplayName);
    }
}
