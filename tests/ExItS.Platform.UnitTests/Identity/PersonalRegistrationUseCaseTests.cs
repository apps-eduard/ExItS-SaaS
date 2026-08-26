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
        var stack = CreateStack(exposeDebugTokens: true);
        var result = await stack.Register.ExecuteAsync("New User", "new.user@example.com");
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.DebugToken);
        Assert.Equal(RegisterPersonalAccount.GenericAcknowledgement, result.Value.Message);

        var user = await stack.Users.GetByNormalizedEmailAsync("new.user@example.com");
        Assert.NotNull(user);
        Assert.Equal(AccountStatus.PendingVerification, user!.Status);
        Assert.False(string.IsNullOrWhiteSpace(user.Username));

        var profileList = await stack.Profiles.ListByUserAsync(user.Id);
        Assert.Contains(profileList, p => p.AccountClass == AccountClass.Personal && p.IsActive);
        Assert.DoesNotContain(profileList, p => p.AccountClass is AccountClass.Platform or AccountClass.Organization);

        var credential = await stack.Credentials.GetByUserIdAsync(user.Id);
        Assert.NotNull(credential);
        Assert.False(credential!.SupportsPasswordLogin);
        Assert.Null(credential.EmailVerifiedAtUtc);

        Assert.Equal(PlatformAuthOutboundMessageKinds.EmailVerification, stack.Messages.Last!.Kind);
        Assert.Null(stack.Messages.Last.PublicSurface);
    }

    [Fact]
    public async Task Register_duplicate_active_returns_same_ack_without_creating_user_or_email()
    {
        var stack = CreateStack(exposeDebugTokens: true);
        var existing = PlatformUser.Create("active.user", "Active User", "active.user@example.com", T0);
        await stack.Users.AddAsync(existing);
        await stack.Credentials.AddAsync(
            PlatformUserCredential.CreateForExternalLogin(existing.Id, T0, emailVerified: true));
        await stack.Profiles.AddAsync(
            AccountProfile.Create(existing.Id, AccountClass.Personal, T0));

        var addCountBefore = stack.Users.AddCount;
        var messageCountBefore = stack.Messages.Messages.Count;

        var result = await stack.Register.ExecuteAsync("Another Name", "active.user@example.com");

        Assert.True(result.IsSuccess);
        Assert.Equal(RegisterPersonalAccount.GenericAcknowledgement, result.Value!.Message);
        Assert.Null(result.Value.DebugToken);
        Assert.Null(result.Value.ExpiresAtUtc);
        Assert.NotEqual(ApplicationErrorCodes.EmailConflict, result.ErrorCode);
        Assert.Equal(addCountBefore, stack.Users.AddCount);
        Assert.Equal(messageCountBefore, stack.Messages.Messages.Count);
        Assert.Single(await stack.Profiles.ListByUserAsync(existing.Id));
    }

    [Fact]
    public async Task Register_duplicate_pending_reissues_token_and_does_not_duplicate_user()
    {
        var stack = CreateStack(exposeDebugTokens: true);
        var first = await stack.Register.ExecuteAsync("Pending User", "pending.user@example.com");
        Assert.True(first.IsSuccess);
        var firstToken = first.Value!.DebugToken!;
        Assert.Equal(1, stack.Users.AddCount);

        var second = await stack.Register.ExecuteAsync("Pending User", "pending.user@example.com");
        Assert.True(second.IsSuccess);
        Assert.Equal(RegisterPersonalAccount.GenericAcknowledgement, second.Value!.Message);
        Assert.NotEqual(firstToken, second.Value.DebugToken);
        Assert.Equal(1, stack.Users.AddCount);
        Assert.Equal(2, stack.Messages.Messages.Count(m =>
            m.Kind == PlatformAuthOutboundMessageKinds.EmailVerification));

        var activate = CreateActivate(stack);
        var oldTokenResult = await activate.ExecuteAsync(firstToken, "SecurePass1!");
        Assert.False(oldTokenResult.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CredentialTokenInvalid, oldTokenResult.ErrorCode);

        var activated = await activate.ExecuteAsync(second.Value.DebugToken!, "SecurePass1!");
        Assert.True(activated.IsSuccess, activated.ErrorMessage);
    }

    [Fact]
    public async Task Register_public_branches_return_uniform_ack_when_debug_tokens_disabled()
    {
        var stack = CreateStack(exposeDebugTokens: false);
        var fresh = await stack.Register.ExecuteAsync("Fresh User", "fresh.user@example.com");
        Assert.True(fresh.IsSuccess);
        Assert.Null(fresh.Value!.DebugToken);
        Assert.Null(fresh.Value.ExpiresAtUtc);

        var active = PlatformUser.Create("dup.active", "Dup Active", "dup.active@example.com", T0);
        await stack.Users.AddAsync(active);

        var duplicateActive = await stack.Register.ExecuteAsync("Dup Active", "dup.active@example.com");
        Assert.True(duplicateActive.IsSuccess);
        Assert.Equal(fresh.Value.Message, duplicateActive.Value!.Message);
        Assert.Null(duplicateActive.Value.DebugToken);
        Assert.Null(duplicateActive.Value.ExpiresAtUtc);

        var pendingStack = CreateStack(exposeDebugTokens: false);
        var pendingFirst = await pendingStack.Register.ExecuteAsync("Dup Pending", "dup.pending@example.com");
        Assert.True(pendingFirst.IsSuccess);
        var pendingSecond = await pendingStack.Register.ExecuteAsync("Dup Pending", "dup.pending@example.com");
        Assert.True(pendingSecond.IsSuccess);
        Assert.Equal(pendingFirst.Value!.Message, pendingSecond.Value!.Message);
        Assert.Null(pendingSecond.Value.DebugToken);
        Assert.Null(pendingSecond.Value.ExpiresAtUtc);
    }

    [Fact]
    public async Task Register_with_pinoy_loan_manager_surface_does_not_create_organization_access()
    {
        var stack = CreateStack(exposeDebugTokens: true);
        var result = await stack.Register.ExecuteAsync(
            "PLM User",
            "plm.user@example.com",
            PlatformAuthPublicSurfaces.PinoyLoanManager);
        Assert.True(result.IsSuccess);
        Assert.Equal(PlatformAuthPublicSurfaces.PinoyLoanManager, stack.Messages.Last!.PublicSurface);

        var user = await stack.Users.GetByNormalizedEmailAsync("plm.user@example.com");
        var profileList = await stack.Profiles.ListByUserAsync(user!.Id);
        Assert.DoesNotContain(profileList, p => p.AccountClass is AccountClass.Platform or AccountClass.Organization);
        Assert.Empty((await stack.Memberships.ListByUserAsync(user.Id, null, 0, 10)).Items);
    }

    [Fact]
    public async Task Register_rejects_unknown_public_surface()
    {
        var stack = CreateStack(exposeDebugTokens: false);
        var result = await stack.Register.ExecuteAsync(
            "New User",
            "new.user@example.com",
            "https://evil.example/callback");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AuthPublicSurfaceInvalid, result.ErrorCode);
        Assert.Null(await stack.Users.GetByNormalizedEmailAsync("new.user@example.com"));
        Assert.Null(stack.Messages.Last);
    }

    [Fact]
    public async Task Activate_sets_password_verifies_email_and_activates()
    {
        var stack = CreateStack(exposeDebugTokens: true);
        var registered = await stack.Register.ExecuteAsync("Activate Me", "activate.me@example.com");
        var opaque = registered.Value!.DebugToken!;

        var activate = CreateActivate(stack);
        var activated = await activate.ExecuteAsync(opaque, "SecurePass1!");
        Assert.True(activated.IsSuccess, activated.ErrorMessage);

        var user = await stack.Users.GetByNormalizedEmailAsync("activate.me@example.com");
        Assert.Equal(AccountStatus.Active, user!.Status);

        var credential = await stack.Credentials.GetByUserIdAsync(user.Id);
        Assert.True(credential!.SupportsPasswordLogin);
        Assert.NotNull(credential.EmailVerifiedAtUtc);
        Assert.True(activated.Value!.HasPassword);
        Assert.True(activated.Value.EmailVerified);

        var reuse = await activate.ExecuteAsync(opaque, "SecurePass1!");
        Assert.False(reuse.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CredentialTokenInvalid, reuse.ErrorCode);
    }

    private static RegistrationStack CreateStack(bool exposeDebugTokens)
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
            Options.Create(new PlatformCredentialLifecycleOptions
            {
                EmailVerificationTokenLifetimeHours = 24,
                ExposeDebugTokens = exposeDebugTokens
            }),
            new SequentialPublicUserIdGenerator());

        return new RegistrationStack(
            users,
            credentials,
            tokens,
            profiles,
            memberships,
            messages,
            register);
    }

    private static ActivatePersonalAccountRegistration CreateActivate(RegistrationStack stack)
    {
        return new ActivatePersonalAccountRegistration(
            stack.Users,
            stack.Credentials,
            stack.Tokens,
            new StubSessionTokenService(),
            new StubPasswordHasher(),
            new NoOpAuditWriter(),
            new NoOpUnitOfWork(),
            new FixedClock(T0),
            Options.Create(new PlatformPasswordOptions
            {
                MinimumLength = 12,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireNonAlphanumeric = true
            }));
    }

    private sealed record RegistrationStack(
        InMemoryPlatformUserRepository Users,
        InMemoryPlatformUserCredentialRepository Credentials,
        InMemoryPlatformCredentialTokenRepository Tokens,
        InMemoryAccountProfileRepository Profiles,
        InMemoryOrganizationMembershipRepository Memberships,
        CapturingAuthOutboundMessageSink Messages,
        RegisterPersonalAccount Register);

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
