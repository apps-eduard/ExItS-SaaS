using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformStaffIdentityFieldsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePlatformStaffUser_generates_unique_staff_numbers_for_two_creates()
    {
        var harness = CreateStaffHarness();

        var first = await harness.CreateStaff.ExecuteAsync(
            "Olivia",
            "Staff",
            "Olivia Staff",
            "olivia.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin");

        var second = await harness.CreateStaff.ExecuteAsync(
            "Carlo",
            "Staff",
            "Carlo Staff",
            "carlo.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin");

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal("STF-000001", first.Value!.User.StaffNumber);
        Assert.Equal("STF-000002", second.Value!.User.StaffNumber);
        Assert.NotEqual(first.Value.User.StaffNumber, second.Value.User.StaffNumber);
    }

    [Fact]
    public async Task UpdateStaffProfile_rejects_staff_number_changes()
    {
        var harness = CreateStaffHarness();
        var created = await harness.CreateStaff.ExecuteAsync(
            "Olivia",
            "Staff",
            "Olivia Staff",
            "olivia.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin");
        Assert.True(created.IsSuccess);

        var update = new UpdatePlatformUserProfile(harness.Users, harness.UnitOfWork, harness.Clock);
        var result = await update.ExecuteAsync(
            created.Value!.User.Id,
            created.Value.User.DisplayName,
            created.Value.User.NormalizedEmail,
            firstName: "Olivia",
            lastName: "Staff",
            staffNumber: "STF-999999");

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.StaffNumberImmutable, result.ErrorCode);
    }

    [Fact]
    public async Task CreatePlatformStaffUser_assigns_required_platform_role()
    {
        var harness = CreateStaffHarness();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var createStaff = new CreatePlatformStaffUser(
            new CreatePlatformUser(harness.Users, harness.UnitOfWork, harness.Clock, new SequentialPublicUserIdGenerator()),
            new InMemoryStaffNumberGenerator(),
            new AssignPlatformRole(
                roles,
                harness.Users,
                new InMemoryPlatformOrganizationRepository(),
                harness.EnsureProfiles,
                new NoOpAuditWriter(),
                harness.UnitOfWork,
                harness.Clock),
            harness.EnsureProfiles,
            harness.Credentials,
            harness.SetPassword,
            harness.IssueEmailVerification,
            harness.UnitOfWork,
            harness.Clock);

        var result = await createStaff.ExecuteAsync(
            "Olivia",
            "Staff",
            "Olivia Staff",
            "olivia.staff@example.com",
            PlatformSystemRole.PlatformAdministrator,
            actorIdentifier: "test-admin");

        Assert.True(result.IsSuccess);
        var activeRoles = await roles.ListActiveByUserAsync(result.Value!.User.Id);
        Assert.Contains(
            activeRoles,
            r => r.Role == PlatformSystemRole.PlatformAdministrator && r.OrganizationId is null);
    }

    [Fact]
    public async Task Pending_verification_staff_cannot_sign_in()
    {
        var harness = CreateStaffHarness();
        var created = await harness.CreateStaff.ExecuteAsync(
            "Pending",
            "Staff",
            "Pending Staff",
            "pending.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin",
            requireEmailVerification: true,
            initialPassword: "SecurePass1!");
        Assert.True(created.IsSuccess);
        Assert.Equal(AccountStatus.PendingVerification, created.Value!.User.Status);

        var login = new LoginPlatformUser(
            harness.Users,
            harness.Credentials,
            harness.Sessions,
            new InMemoryOrganizationMembershipRepository(),
            new InMemoryPlatformOrganizationRepository(),
            new InMemoryOrganizationContextPreferenceRepository(),
            harness.EnsureProfiles,
            new StubPasswordHasher(),
            new StubSessionTokenService(),
            new NoOpAuditWriter(),
            harness.UnitOfWork,
            harness.Clock,
            Options.Create(new PlatformLockoutOptions()),
            Options.Create(new PlatformSessionOptions()),
            new NullPlatformMfaReadinessService(),
            Options.Create(new LocalValidationOptions()));

        var loginResult = await login.ExecuteAsync(
            "pending.staff@example.com",
            "SecurePass1!",
            ipAddress: null,
            userAgent: null);

        Assert.False(loginResult.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountNotEligibleForLogin, loginResult.ErrorCode);
    }

    [Fact]
    public async Task Staff_pending_verification_can_activate_to_active()
    {
        var harness = CreateStaffHarness();
        var created = await harness.CreateStaff.ExecuteAsync(
            "Activate",
            "Staff",
            "Activate Staff",
            "activate.staff@example.com",
            PlatformSystemRole.PlatformSupport,
            actorIdentifier: "test-admin",
            requireEmailVerification: true,
            initialPassword: "SecurePass1!");
        Assert.True(created.IsSuccess);
        Assert.Equal(AccountStatus.PendingVerification, created.Value!.User.Status);

        var user = await harness.Users.GetByIdAsync(created.Value.User.Id);
        user!.ActivateFromPendingVerification(T0.AddMinutes(1));
        await harness.Users.UpdateAsync(user);
        await harness.UnitOfWork.SaveChangesAsync();

        var refreshed = await harness.Users.GetByIdAsync(created.Value.User.Id);
        Assert.Equal(AccountStatus.Active, refreshed!.Status);
    }

    private static StaffHarness CreateStaffHarness()
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
        var setPassword = new SetPlatformUserPassword(
            users,
            credentials,
            sessions,
            accessTokens,
            new InMemoryPlatformDeviceRecoveryCredentialRepository(),
            new StubPasswordHasher(),
            audit,
            uow,
            clock,
            Options.Create(new PlatformPasswordOptions()));
        var issueEmailVerification = new IssueEmailVerificationForUser(
            users,
            credentials,
            tokens,
            new StubSessionTokenService(),
            new CapturingAuthOutboundMessageSink(),
            audit,
            uow,
            clock,
            Options.Create(new PlatformCredentialLifecycleOptions { ExposeDebugTokens = true }));

        var createStaff = new CreatePlatformStaffUser(
            new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator()),
            new InMemoryStaffNumberGenerator(),
            new AssignPlatformRole(roles, users, orgs, ensure, audit, uow, clock),
            ensure,
            credentials,
            setPassword,
            issueEmailVerification,
            uow,
            clock);

        return new StaffHarness(
            users,
            credentials,
            sessions,
            uow,
            clock,
            ensure,
            setPassword,
            issueEmailVerification,
            createStaff);
    }

    private sealed record StaffHarness(
        InMemoryPlatformUserRepository Users,
        InMemoryPlatformUserCredentialRepository Credentials,
        InMemoryPlatformAuthSessionRepository Sessions,
        NoOpUnitOfWork UnitOfWork,
        FixedClock Clock,
        EnsureAccountProfilesForUser EnsureProfiles,
        SetPlatformUserPassword SetPassword,
        IssueEmailVerificationForUser IssueEmailVerification,
        CreatePlatformStaffUser CreateStaff);

    private sealed class StubPasswordHasher : IPlatformPasswordHasher
    {
        public string Algorithm => "stub";

        public string HashPassword(string password) => $"hash:{password}";

        public PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword) =>
            hashedPassword == $"hash:{providedPassword}"
                ? PlatformPasswordVerificationResult.Success
                : PlatformPasswordVerificationResult.Failed;
    }

    private sealed class NullPlatformMfaReadinessService : IPlatformMfaReadinessService
    {
        public Task<PlatformMfaReadinessDto> GetForUserAsync(
            PlatformUserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlatformMfaReadinessDto(
                MfaEnabled: false,
                EnrollmentAvailable: false,
                EnforcementRequired: false,
                ChallengeRequired: false,
                RegisteredFactorCount: 0,
                ReadinessState: PlatformMfaReadinessService.StateNotEnrolled));
    }
}
