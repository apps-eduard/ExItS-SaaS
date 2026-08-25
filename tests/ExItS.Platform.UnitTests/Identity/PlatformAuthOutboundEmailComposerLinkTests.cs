using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthOutboundEmailComposerLinkTests
{
    [Fact]
    public void Email_verification_links_use_admin_activate_account_on_auth_public_base()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "new.user@exits.local",
            "verify-token-xyz",
            DateTimeOffset.Parse("2026-08-25T12:00:00Z"));

        var (subject, body) = PlatformAuthOutboundEmailComposer.Compose(
            message,
            "http://127.0.0.1:8095");

        Assert.Equal("Verify your ExItS account", subject);
        Assert.Contains(
            "http://127.0.0.1:8095/admin/activate-account?token=verify-token-xyz",
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain(":8090/", body, StringComparison.Ordinal);
        Assert.DoesNotContain(":5177/", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Password_reset_links_use_admin_reset_password_on_auth_public_base()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.PasswordReset,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "user@exits.local",
            "reset-token-abc",
            DateTimeOffset.Parse("2026-08-25T12:00:00Z"));

        var (_, body) = PlatformAuthOutboundEmailComposer.Compose(
            message,
            "http://127.0.0.1:8095");

        Assert.Contains(
            "http://127.0.0.1:8095/admin/reset-password?token=reset-token-abc",
            body,
            StringComparison.Ordinal);
        Assert.DoesNotContain(":8090/", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PlatformEmail_is_not_configured_without_admin_public_base_url(string adminBase)
    {
        var opts = new PlatformEmailDeliveryOptions
        {
            SmtpHost = "127.0.0.1",
            SmtpPort = 1025,
            FromAddress = "noreply@exits.local",
            AdminPublicBaseUrl = adminBase
        };

        Assert.False(opts.IsConfigured);
    }

    [Fact]
    public void PlatformEmail_is_configured_for_mailpit_local_validation_shape()
    {
        var opts = new PlatformEmailDeliveryOptions
        {
            SmtpHost = "127.0.0.1",
            SmtpPort = 1025,
            UseSsl = false,
            FromAddress = "noreply@exits.local",
            AdminPublicBaseUrl = "http://127.0.0.1:8095"
        };

        Assert.True(opts.IsConfigured);
    }
}
