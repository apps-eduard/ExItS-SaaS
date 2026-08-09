using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

using ExItS.Platform.UnitTests.TestSupport;
namespace ExItS.Platform.UnitTests.Application;

public sealed class UseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePlatformUser_succeeds_and_detects_email_and_username_conflict()
    {
        var users = new InMemoryPlatformUserRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator());

        var first = await create.ExecuteAsync("ada", "Ada Lovelace", "ada@example.com");
        Assert.True(first.IsSuccess);
        Assert.Equal(1, users.AddCount);

        var emailConflict = await create.ExecuteAsync("ada2", "Ada Two", "ADA@example.com");
        Assert.Equal(ApplicationErrorCodes.EmailConflict, emailConflict.ErrorCode);

        var usernameConflict = await create.ExecuteAsync("ADA", "Ada Three", "ada3@example.com");
        Assert.Equal(ApplicationErrorCodes.UsernameConflict, usernameConflict.ErrorCode);
        Assert.Equal(1, users.AddCount);
    }

    [Fact]
    public async Task CreatePlatformUser_rejects_invalid_domain_input_without_persisting()
    {
        var users = new InMemoryPlatformUserRepository();
        var create = new CreatePlatformUser(users, new NoOpUnitOfWork(), new FixedClock(T0), new SequentialPublicUserIdGenerator());
        var result = await create.ExecuteAsync("ada", "A", "ada@example.com");
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, result.ErrorCode);
        Assert.Equal(0, users.AddCount);
    }

    [Fact]
    public async Task SuspendPlatformUser_handles_missing_and_success()
    {
        var users = new InMemoryPlatformUserRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var accessTokens = new InMemoryPlatformAccessTokenRepository();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator());
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var suspend = new SuspendPlatformUser(users, roles, sessions, accessTokens, audit, uow, clock);

        var missing = await suspend.ExecuteAsync(PlatformUserId.New());
        Assert.Equal(ApplicationErrorCodes.UserNotFound, missing.ErrorCode);

        var created = await create.ExecuteAsync("ada", "Ada Lovelace", "ada@example.com");
        clock.UtcNow = T0.AddMinutes(1);
        var suspended = await suspend.ExecuteAsync(created.Value!.Id);
        Assert.True(suspended.IsSuccess);
        Assert.Equal(AccountStatus.Suspended, suspended.Value!.Status);
        Assert.Equal(1, users.UpdateCount);
    }

    [Fact]
    public async Task SuspendPlatformUser_revokes_active_sessions_and_access_tokens()
    {
        var users = new InMemoryPlatformUserRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var accessTokens = new InMemoryPlatformAccessTokenRepository();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var create = new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator());
        var user = (await create.ExecuteAsync("ada", "Ada Lovelace", "ada@example.com")).Value!;

        var session = PlatformAuthSession.Create(
            user.Id,
            AccountProfileId.New(),
            AccountClass.Platform,
            "hash-session",
            "stamp",
            T0,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(12));
        await sessions.AddAsync(session);
        var token = PlatformAccessToken.Create(
            user.Id,
            "hash-token",
            "stamp",
            T0,
            TimeSpan.FromHours(8));
        await accessTokens.AddAsync(token);

        clock.UtcNow = T0.AddMinutes(1);
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var suspend = new SuspendPlatformUser(users, roles, sessions, accessTokens, audit, uow, clock);
        Assert.True((await suspend.ExecuteAsync(user.Id)).IsSuccess);
        Assert.False(session.IsActive(clock.UtcNow));
        Assert.False(token.IsActive(clock.UtcNow));
        Assert.True(audit.WriteCount >= 1);
    }

    [Fact]
    public async Task CreatePlatformOrganization_detects_slug_conflict()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var create = new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), new NoOpUnitOfWork(), new FixedClock(T0));
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
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var user = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator()).ExecuteAsync("ada", "Ada Lovelace", "ada@example.com")).Value!;
        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock).ExecuteAsync("Acme Group", "acme-group")).Value!;
        var add = new AddOrganizationMembership(users, orgs, memberships, new EnsureAccountProfilesForUser(new InMemoryAccountProfileRepository(), new InMemoryPlatformRoleAssignmentRepository(), memberships, uow, clock), uow, clock);

        var first = await add.ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationOwner);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, memberships.AddCount);

        // Personal identities cannot receive Staff membership (org-scoped staff required).
        var personalAsStaff = await add.ExecuteAsync(org.Id, user.Id, OrganizationRole.OrganizationMember);
        Assert.Equal(DomainErrorCodes.HomeOrganizationRequired, personalAsStaff.ErrorCode);
        Assert.Equal(1, memberships.AddCount);

        var staff = PlatformUser.CreateOrganizationStaff(
            "ada_staff",
            $"ada@{org.PublicOrganizationId}",
            "ada@example.com",
            org.Id,
            "Ada Staff",
            T0);
        await users.AddAsync(staff);
        var staffMembership = await add.ExecuteAsync(org.Id, staff.Id, OrganizationRole.OrganizationMember);
        Assert.True(staffMembership.IsSuccess);
        var conflict = await add.ExecuteAsync(org.Id, staff.Id, OrganizationRole.OrganizationMember);
        Assert.Equal(ApplicationErrorCodes.MembershipConflict, conflict.ErrorCode);
        Assert.Equal(2, memberships.AddCount);
    }

    [Fact]
    public async Task AddOrganizationMembership_fails_when_user_missing_without_partial_write()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock).ExecuteAsync("Acme Group", "acme-group")).Value!;

        var result = await new AddOrganizationMembership(users, orgs, memberships, new EnsureAccountProfilesForUser(new InMemoryAccountProfileRepository(), new InMemoryPlatformRoleAssignmentRepository(), memberships, uow, clock), uow, clock)
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
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var owner = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator()).ExecuteAsync("owner", "Org Owner", "owner@example.com")).Value!;
        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock).ExecuteAsync("Acme Group", "acme-group")).Value!;
        var staff = PlatformUser.CreateOrganizationStaff(
            "ada_staff",
            $"ada@{org.PublicOrganizationId}",
            "ada@example.com",
            org.Id,
            "Ada Lovelace",
            T0);
        await users.AddAsync(staff);
        var ownerMembership = await new AddOrganizationMembership(users, orgs, memberships, new EnsureAccountProfilesForUser(new InMemoryAccountProfileRepository(), new InMemoryPlatformRoleAssignmentRepository(), memberships, uow, clock), uow, clock)
            .ExecuteAsync(org.Id, owner.Id, OrganizationRole.OrganizationOwner);
        Assert.True(ownerMembership.IsSuccess);
        var membership = (await new AddOrganizationMembership(users, orgs, memberships, new EnsureAccountProfilesForUser(new InMemoryAccountProfileRepository(), new InMemoryPlatformRoleAssignmentRepository(), memberships, uow, clock), uow, clock)
            .ExecuteAsync(org.Id, staff.Id, OrganizationRole.OrganizationMember)).Value!;

        clock.UtcNow = T0.AddMinutes(1);
        var roleChanged = await new ChangeOrganizationRole(memberships, uow, clock)
            .ExecuteAsync(membership.Id, OrganizationRole.OrganizationAdministrator);
        Assert.True(roleChanged.IsSuccess);
        Assert.Equal(OrganizationRole.OrganizationAdministrator, roleChanged.Value!.Role);

        clock.UtcNow = T0.AddMinutes(2);
        var suspended = await new SuspendOrganizationMembership(
            memberships,
            new InMemoryPlatformAuthSessionRepository(),
            new InMemoryPlatformAccessTokenRepository(),
            uow,
            clock)
            .ExecuteAsync(membership.Id);
        Assert.True(suspended.IsSuccess);
        Assert.Equal(MembershipStatus.Suspended, suspended.Value!.Status);
    }
}
