using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthOutboundEmailComposerTests
{
    [Fact]
    public void Activation_email_uses_react_admin_activate_path_and_supplied_base_url()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            Guid.Empty,
            "new.user@example.com",
            "opaque-activation-token",
            DateTimeOffset.Parse("2026-08-22T12:00:00Z"));

        var (subject, body) = PlatformAuthOutboundEmailComposer.Compose(
            message,
            "http://localhost:8095");

        Assert.Equal("Verify your ExItS account", subject);
        Assert.Contains("http://localhost:8095/admin/activate-account?token=opaque-activation-token", body, StringComparison.Ordinal);
        Assert.DoesNotContain(":8090/", body, StringComparison.Ordinal);
        Assert.DoesNotContain("100.120.79.81", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Password_reset_email_uses_react_admin_reset_path_and_supplied_base_url()
    {
        var message = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.PasswordReset,
            Guid.Empty,
            "new.user@example.com",
            "opaque-reset-token",
            DateTimeOffset.Parse("2026-08-22T12:00:00Z"));

        var (_, body) = PlatformAuthOutboundEmailComposer.Compose(
            message,
            "http://100.64.1.20:8095");

        Assert.Contains("http://100.64.1.20:8095/admin/reset-password?token=opaque-reset-token", body, StringComparison.Ordinal);
        Assert.DoesNotContain(":8090/", body, StringComparison.Ordinal);
        Assert.DoesNotContain("100.120.79.81", body, StringComparison.Ordinal);
    }
}
