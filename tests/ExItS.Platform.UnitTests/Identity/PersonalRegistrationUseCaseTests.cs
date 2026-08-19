using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PersonalRegistrationUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_creates_pending_verification_personal_account_and_issues_token()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var clock = new FixedClock(T0);
        var unitOfWork = new NoOpUnitOfWork();
        var audit = new NoOpAuditWriter();
        var tokenService = new StubSessionTokenService();
        var ensureProfiles = new EnsureAccountProfilesForUser(profiles, roles, memberships, unitOfWork, clock);

        var register = new RegisterPersonalAccount(
            users,
            credentials,
            tokens,
            tokenService,
            ensureProfiles,
            messages,
            audit,
            unitOfWork,
            clock,
            Options.Create(new PlatformCredentialLifecycleOptions { EmailVerificationTokenLifetimeHours = 24, ExposeDebugTokens = true }),
            new SequentialPublicUserIdGenerator());

        var result = await register.ExecuteAsync("New User", "new.user@example.com");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.DebugToken);

        var user = await users.GetByNormalizedEmailAsync("new.user@example.com");
        Assert.NotNull(user);
        Assert.Equal(AccountStatus.PendingVerification, user!.Status);
        Assert.False(string.IsNullOrWhiteSpace(user.Username));

        var profileList = await profiles.ListByUserAsync(user.Id);
        Assert.Contains(profileList, p => p.AccountClass == AccountClass.Personal && p.IsActive);
        Assert.DoesNotContain(profileList, p => p.AccountClass is AccountClass.Platform or AccountClass.Organization);

        var credential = await credentials.GetByUserIdAsync(user.Id);
        Assert.NotNull(credential);
        Assert.False(credential!.SupportsPasswordLogin);
        Assert.Null(credential.EmailVerifiedAtUtc);

        Assert.Equal(PlatformAuthOutboundMessageKinds.EmailVerification, messages.Last!.Kind);
        Assert.Null(messages.Last.PublicSurface);
    }

    [Fact]
    public async Task Register_with_pinoy_loan_manager_surface_does_not_create_organization_access()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var clock = new FixedClock(T0);
        var unitOfWork = new NoOpUnitOfWork();
        var audit = new NoOpAuditWriter();
        var tokenService = new StubSessionTokenService();
        var ensureProfiles = new EnsureAccountProfilesForUser(profiles, roles, memberships, unitOfWork, clock);

        var register = new RegisterPersonalAccount(
            users,
            credentials,
            tokens,
            tokenService,
            ensureProfiles,
            messages,
            audit,
            unitOfWork,
            clock,
            Options.Create(new PlatformCredentialLifecycleOptions { EmailVerificationTokenLifetimeHours = 24, ExposeDebugTokens = true }),
            new SequentialPublicUserIdGenerator());

        var result = await register.ExecuteAsync(
            "PLM User",
            "plm.user@example.com",
            PlatformAuthPublicSurfaces.PinoyLoanManager);
        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformAuthPublicSurfaces.PinoyLoanManager, messages.Last!.PublicSurface);

        var user = await users.GetByNormalizedEmailAsync("plm.user@example.com");
        var profileList = await profiles.ListByUserAsync(user!.Id);
        Assert.DoesNotContain(profileList, p => p.AccountClass is AccountClass.Platform or AccountClass.Organization);
        Assert.Empty((await memberships.ListByUserAsync(user.Id, null, 0, 10)).Items);
    }

    [Fact]
    public async Task Register_rejects_unknown_public_surface()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var clock = new FixedClock(T0);
        var unitOfWork = new NoOpUnitOfWork();
        var audit = new NoOpAuditWriter();
        var tokenService = new StubSessionTokenService();
        var ensureProfiles = new EnsureAccountProfilesForUser(profiles, roles, memberships, unitOfWork, clock);

        var register = new RegisterPersonalAccount(
            users,
            credentials,
            tokens,
            tokenService,
            ensureProfiles,
            messages,
            audit,
            unitOfWork,
            clock,
            Options.Create(new PlatformCredentialLifecycleOptions { EmailVerificationTokenLifetimeHours = 24 }),
            new SequentialPublicUserIdGenerator());

        var result = await register.ExecuteAsync(
            "New User",
            "new.user@example.com",
            "https://evil.example/callback");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AuthPublicSurfaceInvalid, result.ErrorCode);
        Assert.Null(await users.GetByNormalizedEmailAsync("new.user@example.com"));
        Assert.Null(messages.Last);
    }

    [Fact]
    public async Task Activate_sets_password_verifies_email_and_activates()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var clock = new FixedClock(T0);
        var unitOfWork = new NoOpUnitOfWork();
        var audit = new NoOpAuditWriter();
        var tokenService = new StubSessionTokenService();
        var hasher = new StubPasswordHasher();
        var ensureProfiles = new EnsureAccountProfilesForUser(profiles, roles, memberships, unitOfWork, clock);

        var register = new RegisterPersonalAccount(
            users,
            credentials,
            tokens,
            tokenService,
            ensureProfiles,
            messages,
            audit,
            unitOfWork,
            clock,
            Options.Create(new PlatformCredentialLifecycleOptions { EmailVerificationTokenLifetimeHours = 24, ExposeDebugTokens = true }),
            new SequentialPublicUserIdGenerator());

        var registered = await register.ExecuteAsync("Activate Me", "activate.me@example.com");
        var opaque = registered.Value!.DebugToken!;

        var activate = new ActivatePersonalAccountRegistration(
            users,
            credentials,
            tokens,
            tokenService,
            hasher,
            audit,
            unitOfWork,
            clock,
            Options.Create(new PlatformPasswordOptions
            {
                MinimumLength = 12,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true
            }));

        var activated = await activate.ExecuteAsync(opaque, "SecurePass1!");
        Assert.True(activated.IsSuccess, activated.ErrorMessage);

        var user = await users.GetByNormalizedEmailAsync("activate.me@example.com");
        Assert.Equal(AccountStatus.Active, user!.Status);

        var credential = await credentials.GetByUserIdAsync(user.Id);
        Assert.True(credential!.SupportsPasswordLogin);
        Assert.NotNull(credential.EmailVerifiedAtUtc);
        Assert.True(activated.Value!.HasPassword);
        Assert.True(activated.Value.EmailVerified);
    }

    private sealed class StubPasswordHasher : IPlatformPasswordHasher
    {
        public string Algorithm => PlatformUserCredential.AspNetCoreIdentityV3;

        public string HashPassword(string password) => $"hash:{password}";

        public PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword) =>
            hashedPassword == $"hash:{providedPassword}"
                ? PlatformPasswordVerificationResult.Success
                : PlatformPasswordVerificationResult.Failed;
    }
}
