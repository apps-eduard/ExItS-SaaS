using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class RecoveryEmailUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Request_confirm_skip_do_not_grant_membership_or_roles()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var tokenService = new StubSessionTokenService();
        var lifecycle = Options.Create(new PlatformCredentialLifecycleOptions
        {
            ExposeDebugTokens = true,
            EmailVerificationTokenLifetimeHours = 24
        });

        var user = PlatformUser.Create("social1", "Social User", "social1@gmail.com", T0);
        await users.AddAsync(user);
        await credentials.AddAsync(PlatformUserCredential.CreateForExternalLogin(user.Id, T0, emailVerified: true));

        var request = new RequestRecoveryEmailChange(
            users, credentials, tokens, tokenService, messages, audit, uow, clock, lifecycle);
        var requestResult = await request.ExecuteAsync(user.Id.Value, "backup@example.com");
        Assert.True(requestResult.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(requestResult.Value!.DebugToken));
        Assert.Equal(PlatformAuthOutboundMessageKinds.RecoveryEmailVerification, messages.Last!.Kind);
        Assert.Equal("backup@example.com", messages.Last.Email);

        var pending = await credentials.GetByUserIdAsync(user.Id);
        Assert.Equal("backup@example.com", pending!.PendingRecoveryNormalizedEmail);
        Assert.False(pending.HasVerifiedRecoveryEmail);

        var confirm = new ConfirmRecoveryEmailChange(
            users, credentials, tokens, tokenService, audit, uow, clock);
        var confirmResult = await confirm.ExecuteAsync(requestResult.Value.DebugToken);
        Assert.True(confirmResult.IsSuccess);
        Assert.True(confirmResult.Value!.RecoveryEmailVerified);
        Assert.Equal("backup@example.com", confirmResult.Value.RecoveryEmail);
        Assert.False(confirmResult.Value.NeedsRecoveryEmailPrompt);

        // Recovery email must not imply organization/product privileges in credential status.
        Assert.False(confirmResult.Value.HasPassword);
    }

    [Fact]
    public async Task Skip_clears_prompt_without_setting_recovery_email()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var user = PlatformUser.Create("social2", "Social Two", "social2@example.com", T0);
        await users.AddAsync(user);
        await credentials.AddAsync(PlatformUserCredential.CreateForExternalLogin(user.Id, T0, emailVerified: true));

        var skip = new SkipRecoveryEmailPrompt(users, credentials, audit, uow, clock);
        var result = await skip.ExecuteAsync(user.Id.Value);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.NeedsRecoveryEmailPrompt);
        Assert.False(result.Value.RecoveryEmailVerified);
        Assert.Null(result.Value.RecoveryEmail);
    }

    [Fact]
    public async Task Password_reset_delivers_to_verified_recovery_email_when_matched()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var tokenService = new StubSessionTokenService();
        var lifecycle = Options.Create(new PlatformCredentialLifecycleOptions
        {
            ExposeDebugTokens = true,
            PasswordResetTokenLifetimeMinutes = 30
        });

        var user = PlatformUser.Create("owner3", "Owner Three", "owner3@example.com", T0);
        await users.AddAsync(user);
        var credential = PlatformUserCredential.Create(
            user.Id,
            "hash",
            PlatformUserCredential.AspNetCoreIdentityV3,
            T0);
        credential.BeginRecoveryEmailChange("recovery3@example.com", T0);
        credential.ConfirmRecoveryEmail(T0.AddMinutes(1));
        await credentials.AddAsync(credential);

        var forgot = new RequestPasswordReset(
            users, credentials, tokens, tokenService, messages, audit, uow, clock, lifecycle);
        var result = await forgot.ExecuteAsync("recovery3@example.com");
        Assert.True(result.IsSuccess);
        Assert.NotNull(messages.Last);
        Assert.Equal(PlatformAuthOutboundMessageKinds.PasswordReset, messages.Last.Kind);
        Assert.Equal("recovery3@example.com", messages.Last.Email);
        Assert.Equal(user.Id.Value, messages.Last.UserId);
    }

    [Fact]
    public async Task Request_rejects_recovery_email_same_as_primary()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var tokens = new InMemoryPlatformCredentialTokenRepository();
        var messages = new CapturingAuthOutboundMessageSink();
        var audit = new NoOpAuditWriter();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var tokenService = new StubSessionTokenService();
        var lifecycle = Options.Create(new PlatformCredentialLifecycleOptions { ExposeDebugTokens = true });

        var user = PlatformUser.Create("social4", "Social Four", "social4@example.com", T0);
        await users.AddAsync(user);
        await credentials.AddAsync(PlatformUserCredential.CreateForExternalLogin(user.Id, T0, emailVerified: true));

        var request = new RequestRecoveryEmailChange(
            users, credentials, tokens, tokenService, messages, audit, uow, clock, lifecycle);
        var result = await request.ExecuteAsync(user.Id.Value, "social4@example.com");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.RecoveryEmailInvalid, result.ErrorCode);
    }
}
