using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class StaffProvisioningUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePlatformStaffUser_assigns_role_and_exclusive_platform_profile()
    {
        var users = new InMemoryPlatformUserRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var accessTokens = new InMemoryPlatformAccessTokenRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var audit = new NoOpAuditWriter();
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);

        var createStaff = new CreatePlatformStaffUser(
            new CreatePlatformUser(users, uow, clock),
            new InMemoryStaffNumberGenerator(),
            new AssignPlatformRole(roles, users, orgs, ensure, audit, uow, clock),
            ensure,
            credentials,
            new SetPlatformUserPassword(
                users,
                credentials,
                sessions,
                accessTokens,
                new StubPasswordHasher(),
                audit,
                uow,
                clock,
                Options.Create(new PlatformPasswordOptions())),
            new IssueEmailVerificationForUser(
                users,
                credentials,
                tokens,
                new StubSessionTokenService(),
                new CapturingAuthOutboundMessageSink(),
                audit,
                uow,
                clock,
                Options.Create(new PlatformCredentialLifecycleOptions { ExposeDebugTokens = true })),
            uow,
            clock);

        var result = await createStaff.ExecuteAsync(
            "Olivia",
            "Staff",
            "Olivia Staff",
            "olivia.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin");

        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformSystemRole.PlatformSupport, result.Value!.PlatformRole);
        Assert.False(result.Value.EmailVerificationIssued);
        Assert.Equal("STF-000001", result.Value.User.StaffNumber);
        Assert.Equal("Olivia", result.Value.User.FirstName);
        Assert.Equal("Staff", result.Value.User.LastName);

        var activeRoles = await roles.ListActiveByUserAsync(result.Value.User.Id);
        Assert.Contains(activeRoles, r => r.Role == PlatformSystemRole.PlatformSupport && r.OrganizationId is null);

        var activeProfiles = (await profiles.ListByUserAsync(result.Value.User.Id))
            .Where(p => p.IsActive)
            .Select(p => p.AccountClass)
            .ToList();
        Assert.Equal([AccountClass.Platform], activeProfiles);
    }

    [Fact]
    public async Task EnsureOrganizationStaffIdentity_creates_exclusive_organization_profile_only()
    {
        var users = new InMemoryPlatformUserRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
        var provision = new EnsureOrganizationStaffIdentity(
            new CreatePlatformUser(users, uow, clock),
            users,
            ensure);

        var result = await provision.ExecuteAsync("carlo.staff@example.com", "Carlo Staff");

        Assert.True(result.IsSuccess);
        var activeProfiles = (await profiles.ListByUserAsync(result.Value!.Id))
            .Where(p => p.IsActive)
            .Select(p => p.AccountClass)
            .ToList();
        Assert.Equal([AccountClass.Organization], activeProfiles);
        Assert.DoesNotContain(activeProfiles, c => c is AccountClass.Personal or AccountClass.Platform);
    }

    [Fact]
    public async Task CreatePlatformStaffUser_issues_email_verification_without_initial_password()
    {
        var users = new InMemoryPlatformUserRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var accessTokens = new InMemoryPlatformAccessTokenRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var audit = new NoOpAuditWriter();
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
        var messages = new CapturingAuthOutboundMessageSink();

        var createStaff = new CreatePlatformStaffUser(
            new CreatePlatformUser(users, uow, clock),
            new InMemoryStaffNumberGenerator(),
            new AssignPlatformRole(roles, users, orgs, ensure, audit, uow, clock),
            ensure,
            credentials,
            new SetPlatformUserPassword(
                users,
                credentials,
                sessions,
                accessTokens,
                new StubPasswordHasher(),
                audit,
                uow,
                clock,
                Options.Create(new PlatformPasswordOptions())),
            new IssueEmailVerificationForUser(
                users,
                credentials,
                tokens,
                new StubSessionTokenService(),
                messages,
                audit,
                uow,
                clock,
                Options.Create(new PlatformCredentialLifecycleOptions { ExposeDebugTokens = true })),
            uow,
            clock);

        var result = await createStaff.ExecuteAsync(
            "Pending",
            "Staff",
            "Pending Staff",
            "pending.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin",
            requireEmailVerification: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.EmailVerificationIssued);
        Assert.Equal(AccountStatus.PendingVerification, result.Value.User.Status);
        var storedCredential = await credentials.GetByUserIdAsync(result.Value.User.Id);
        Assert.NotNull(storedCredential);
        Assert.False(storedCredential!.SupportsPasswordLogin);
    }

    private sealed class StubPasswordHasher : IPlatformPasswordHasher
    {
        public string Algorithm => "stub";

        public string HashPassword(string password) => $"hash:{password}";

        public PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword) =>
            hashedPassword == $"hash:{providedPassword}"
                ? PlatformPasswordVerificationResult.Success
                : PlatformPasswordVerificationResult.Failed;
    }
}
