using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Application;

public sealed class UseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePlatformUser_succeeds_and_detects_email_conflict()
    {
        var users = new InMemoryPlatformUserRepository();
        var clock = new FixedClock(T0);
        var create = new CreatePlatformUser(users, clock);

        var first = await create.ExecuteAsync("Ada Lovelace", "ada@example.com");
        Assert.True(first.IsSuccess);
        Assert.Equal(1, users.AddCount);

        var conflict = await create.ExecuteAsync("Ada Two", "ADA@example.com");
        Assert.False(conflict.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.EmailConflict, conflict.ErrorCode);
        Assert.Equal(1, users.AddCount);
    }

    [Fact]
    public async Task CreatePlatformUser_rejects_invalid_domain_input_without_persisting()
    {
        var users = new InMemoryPlatformUserRepository();
        var create = new CreatePlatformUser(users, new FixedClock(T0));
        var result = await create.ExecuteAsync("A", "ada@example.com");
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, result.ErrorCode);
        Assert.Equal(0, users.AddCount);
    }

    [Fact]
    public async Task SuspendPlatformUser_handles_missing_and_success()
    {
        var users = new InMemoryPlatformUserRepository();
        var clock = new FixedClock(T0);
        var create = new CreatePlatformUser(users, clock);
        var suspend = new SuspendPlatformUser(users, clock);

        var missing = await suspend.ExecuteAsync(PlatformUserId.New());
        Assert.Equal(ApplicationErrorCodes.UserNotFound, missing.ErrorCode);

        var created = await create.ExecuteAsync("Ada Lovelace", "ada@example.com");
        clock.UtcNow = T0.AddMinutes(1);
        var suspended = await suspend.ExecuteAsync(created.Value!.Id);
        Assert.True(suspended.IsSuccess);
        Assert.Equal(AccountStatus.Suspended, suspended.Value!.Status);
        Assert.Equal(1, users.UpdateCount);
    }

    [Fact]
    public async Task CreatePlatformOrganization_detects_slug_conflict()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var create = new CreatePlatformOrganization(orgs, new NoOpUnitOfWork(), new FixedClock(T0));
        Assert.True((await create.ExecuteAsync("Acme Group", "acme-group")).IsSuccess);
        var conflict = await create.ExecuteAsync("Acme Two", "ACME-GROUP");
        Assert.Equal(ApplicationErrorCodes.SlugConflict, conflict.ErrorCode);
        Assert.Equal(1, orgs.AddCount);
    }

    [Fact]
    public async Task AddOrganizationMembership_enforces_active_pair_and_conflict()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var clock = new FixedClock(T0);

        var user = (await new CreatePlatformUser(users, clock).ExecuteAsync("Ada Lovelace", "ada@example.com")).Value!;
        var org = (await new CreatePlatformOrganization(orgs, new NoOpUnitOfWork(), clock).ExecuteAsync("Acme Group", "acme-group")).Value!;
        var add = new AddOrganizationMembership(users, orgs, memberships, clock);

        var first = await add.ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationOwner);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, memberships.AddCount);

        var conflict = await add.ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationMember);
        Assert.Equal(ApplicationErrorCodes.MembershipConflict, conflict.ErrorCode);
        Assert.Equal(1, memberships.AddCount);
    }

    [Fact]
    public async Task AddOrganizationMembership_fails_when_user_missing_without_partial_write()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var clock = new FixedClock(T0);
        var org = (await new CreatePlatformOrganization(orgs, new NoOpUnitOfWork(), clock).ExecuteAsync("Acme Group", "acme-group")).Value!;

        var result = await new AddOrganizationMembership(users, orgs, memberships, clock)
            .ExecuteAsync(org.Id, PlatformUserId.New(), OrganizationRole.OrganizationMember);

        Assert.Equal(ApplicationErrorCodes.UserNotFound, result.ErrorCode);
        Assert.Equal(0, memberships.AddCount);
    }

    [Fact]
    public async Task SuspendMembership_and_ChangeRole_work()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var clock = new FixedClock(T0);

        var user = (await new CreatePlatformUser(users, clock).ExecuteAsync("Ada Lovelace", "ada@example.com")).Value!;
        var org = (await new CreatePlatformOrganization(orgs, new NoOpUnitOfWork(), clock).ExecuteAsync("Acme Group", "acme-group")).Value!;
        var membership = (await new AddOrganizationMembership(users, orgs, memberships, clock)
            .ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationMember)).Value!;

        clock.UtcNow = T0.AddMinutes(1);
        var roleChanged = await new ChangeOrganizationRole(memberships, clock)
            .ExecuteAsync(membership.Id, OrganizationRole.OrganizationAdministrator);
        Assert.True(roleChanged.IsSuccess);
        Assert.Equal(OrganizationRole.OrganizationAdministrator, roleChanged.Value!.Role);

        clock.UtcNow = T0.AddMinutes(2);
        var suspended = await new SuspendOrganizationMembership(memberships, clock)
            .ExecuteAsync(membership.Id);
        Assert.True(suspended.IsSuccess);
        Assert.Equal(MembershipStatus.Suspended, suspended.Value!.Status);
    }
}
