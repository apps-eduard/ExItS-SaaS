using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformRecoveryEmailCredentialTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId UserId = PlatformUserId.New();

    [Fact]
    public void External_login_needs_recovery_prompt_until_skip_or_verify()
    {
        var credential = PlatformUserCredential.CreateForExternalLogin(UserId, T0, emailVerified: true);
        Assert.True(credential.NeedsRecoveryEmailPrompt);

        credential.SkipRecoveryEmailPrompt(T0.AddMinutes(1));
        Assert.False(credential.NeedsRecoveryEmailPrompt);
    }

    [Fact]
    public void Password_credential_does_not_need_social_recovery_prompt()
    {
        var credential = PlatformUserCredential.Create(
            UserId,
            "hash-value",
            PlatformUserCredential.AspNetCoreIdentityV3,
            T0);
        Assert.False(credential.NeedsRecoveryEmailPrompt);
    }

    [Fact]
    public void Confirm_recovery_email_requires_pending_and_marks_verified()
    {
        var credential = PlatformUserCredential.CreateForExternalLogin(UserId, T0, emailVerified: true);
        Assert.Throws<DomainException>(() => credential.ConfirmRecoveryEmail(T0));

        credential.BeginRecoveryEmailChange("recovery@example.com", T0);
        credential.ConfirmRecoveryEmail(T0.AddMinutes(1));
        Assert.True(credential.HasVerifiedRecoveryEmail);
        Assert.Equal("recovery@example.com", credential.RecoveryNormalizedEmail);
        Assert.Null(credential.PendingRecoveryNormalizedEmail);
        Assert.False(credential.NeedsRecoveryEmailPrompt);
    }

    [Fact]
    public void Clear_recovery_email_removes_verified_address()
    {
        var credential = PlatformUserCredential.CreateForExternalLogin(UserId, T0, emailVerified: true);
        credential.BeginRecoveryEmailChange("recovery@example.com", T0);
        credential.ConfirmRecoveryEmail(T0.AddMinutes(1));
        credential.ClearRecoveryEmail(T0.AddMinutes(2));
        Assert.False(credential.HasVerifiedRecoveryEmail);
        Assert.Null(credential.RecoveryNormalizedEmail);
        Assert.True(credential.NeedsRecoveryEmailPrompt);
    }
}
