using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthCallbackResolverTests
{
    private static readonly DateTimeOffset Expires = DateTimeOffset.Parse("2026-08-19T12:00:00Z");

    [Fact]
    public void Missing_surface_uses_admin_activation_and_reset_paths()
    {
        var verification = Message(PlatformAuthOutboundMessageKinds.EmailVerification, publicSurface: null);
        var reset = Message(PlatformAuthOutboundMessageKinds.PasswordReset, publicSurface: null);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            verification, "https://admin.example/", null, false, out var activate));
        Assert.Equal("https://admin.example/admin/activate-account?token=opaque%2Ftoken", activate);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            reset, "https://admin.example", null, false, out var resetUrl));
        Assert.Equal("https://admin.example/admin/reset-password?token=opaque%2Ftoken", resetUrl);
    }

    [Fact]
    public void Pinoy_loan_manager_surface_uses_explicit_plm_base()
    {
        var verification = Message(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            PlatformAuthPublicSurfaces.PinoyLoanManager);
        var reset = Message(
            PlatformAuthOutboundMessageKinds.PasswordReset,
            PlatformAuthPublicSurfaces.PinoyLoanManager);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            verification,
            "https://admin.example",
            "http://localhost:4176",
            allowHttpLoopbackPublicUrls: true,
            out var activate));
        Assert.Equal("http://localhost:4176/activate-account?token=opaque%2Ftoken", activate);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            reset,
            "https://admin.example",
            "http://localhost:4176",
            allowHttpLoopbackPublicUrls: true,
            out var resetUrl));
        Assert.Equal("http://localhost:4176/reset-password?token=opaque%2Ftoken", resetUrl);
    }

    [Fact]
    public void Production_plm_http_origin_is_fail_closed()
    {
        var verification = Message(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            PlatformAuthPublicSurfaces.PinoyLoanManager);

        Assert.False(PlatformAuthCallbackResolver.TryCreateLink(
            verification,
            "https://admin.example",
            "http://localhost:4176",
            allowHttpLoopbackPublicUrls: false,
            out var url));
        Assert.Equal(string.Empty, url);

        Assert.False(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl("http://evil.example", false));
        Assert.False(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl("https://loans.example/path?x=1", false));
        Assert.False(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl("https://user:pass@loans.example", false));
        Assert.True(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl("https://loans.example", false));
    }

    [Fact]
    public void Invitation_and_recovery_email_are_not_plm_callback_kinds()
    {
        var invitation = Message(PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation, PlatformAuthPublicSurfaces.PinoyLoanManager);
        var recovery = Message(PlatformAuthOutboundMessageKinds.RecoveryEmailVerification, PlatformAuthPublicSurfaces.PinoyLoanManager);

        Assert.False(PlatformAuthCallbackResolver.TryCreateLink(
            invitation, "https://admin.example", "http://localhost:4176", true, out _));
        Assert.False(PlatformAuthCallbackResolver.TryCreateLink(
            recovery, "https://admin.example", "http://localhost:4176", true, out _));
    }

    [Fact]
    public void Composer_keeps_admin_invitation_and_recovery_links()
    {
        var invitation = new PlatformAuthOutboundMessage(
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation,
            Guid.Empty,
            "maria@gmail.com",
            "invite-token-abc",
            Expires,
            OrganizationName: "ABC Sari-Sari Store",
            RoleDisplay: "Staff",
            ContactEmail: "maria@gmail.com",
            PublicSurface: PlatformAuthPublicSurfaces.PinoyLoanManager);
        var recovery = Message(
            PlatformAuthOutboundMessageKinds.RecoveryEmailVerification,
            PlatformAuthPublicSurfaces.PinoyLoanManager);

        var (_, invitationBody) = PlatformAuthOutboundEmailComposer.Compose(
            invitation,
            "https://admin.example",
            "http://localhost:4176",
            allowHttpLoopbackPublicUrls: true);
        var (_, recoveryBody) = PlatformAuthOutboundEmailComposer.Compose(
            recovery,
            "https://admin.example",
            "http://localhost:4176",
            allowHttpLoopbackPublicUrls: true);

        Assert.Contains("/admin/accept-organization-invitation?token=invite-token-abc", invitationBody, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost:4176", invitationBody, StringComparison.Ordinal);
        Assert.Contains("/admin/confirm-recovery-email?token=", recoveryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost:4176", recoveryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("/reset-password", recoveryBody, StringComparison.Ordinal);
        Assert.DoesNotContain("/activate-account", recoveryBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Composer_omits_plm_link_when_origin_is_not_allowed()
    {
        var verification = Message(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            PlatformAuthPublicSurfaces.PinoyLoanManager);
        var (_, body) = PlatformAuthOutboundEmailComposer.Compose(
            verification,
            "https://admin.example",
            "http://evil.example",
            allowHttpLoopbackPublicUrls: false);

        Assert.DoesNotContain("evil.example", body, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/activate-account", body, StringComparison.Ordinal);
        Assert.Contains("could not include an activation or reset link", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pinoy_business_pos_surface_uses_explicit_pos_base()
    {
        var verification = Message(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            PlatformAuthPublicSurfaces.PinoyBusinessPos);
        var reset = Message(
            PlatformAuthOutboundMessageKinds.PasswordReset,
            PlatformAuthPublicSurfaces.PinoyBusinessPos);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            verification,
            "https://admin.example",
            pinoyLoanManagerPublicBaseUrl: null,
            allowHttpLoopbackPublicUrls: true,
            out var activate,
            pinoyBusinessPosPublicBaseUrl: "http://localhost:5177"));
        Assert.Equal("http://localhost:5177/activate-account?token=opaque%2Ftoken", activate);

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            reset,
            "https://admin.example",
            pinoyLoanManagerPublicBaseUrl: null,
            allowHttpLoopbackPublicUrls: true,
            out var resetUrl,
            pinoyBusinessPosPublicBaseUrl: "http://localhost:5177"));
        Assert.Equal("http://localhost:5177/reset-password?token=opaque%2Ftoken", resetUrl);
    }

    [Fact]
    public void Unknown_surface_is_rejected()
    {
        var unknown = PlatformAuthPublicSurfaces.Normalize("https://evil.example/callback");
        Assert.False(unknown.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AuthPublicSurfaceInvalid, unknown.ErrorCode);

        var empty = PlatformAuthPublicSurfaces.Normalize("  ");
        Assert.True(empty.IsSuccess);
        Assert.Null(empty.Value);

        var pos = PlatformAuthPublicSurfaces.Normalize("pinoy-business-pos");
        Assert.True(pos.IsSuccess);
        Assert.Equal(PlatformAuthPublicSurfaces.PinoyBusinessPos, pos.Value);
    }

    [Fact]
    public void ReadBrowserPublicOrigin_prefers_origin_and_strips_referer_path()
    {
        Assert.Equal(
            "http://100.64.1.8:5177",
            PlatformAuthCallbackResolver.ReadBrowserPublicOrigin(
                "http://100.64.1.8:5177",
                "http://127.0.0.1:5177/sign-in"));
        Assert.Equal(
            "http://127.0.0.1:5177",
            PlatformAuthCallbackResolver.ReadBrowserPublicOrigin(
                null,
                "http://127.0.0.1:5177/sign-in?continue=/onboarding"));
        Assert.Null(PlatformAuthCallbackResolver.ReadBrowserPublicOrigin(null, null));
        Assert.Null(PlatformAuthCallbackResolver.ReadBrowserPublicOrigin("http://evil.example/callback?token=x", null));
    }

    [Fact]
    public void Align_swaps_loopback_host_for_allowed_tailscale_origin_on_same_port()
    {
        var allowed = new[]
        {
            "http://localhost:5177",
            "http://127.0.0.1:5177",
            "http://100.64.1.8:5177",
            "http://localhost:8095",
            "http://100.64.1.8:8095",
        };

        Assert.Equal(
            "http://100.64.1.8:5177",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://localhost:5177",
                "http://100.64.1.8:5177",
                allowed,
                allowHttpLoopbackPublicUrls: true));
        Assert.Equal(
            "http://localhost:5177",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://127.0.0.1:5177",
                "http://localhost:5177",
                allowed,
                allowHttpLoopbackPublicUrls: true));
        Assert.Equal(
            "http://100.64.1.8:8095",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://127.0.0.1:8095",
                "http://100.64.1.8:8095",
                allowed,
                allowHttpLoopbackPublicUrls: true));
    }

    [Fact]
    public void Align_does_not_mix_ports_or_unlisted_origins_or_production()
    {
        var allowed = new[] { "http://localhost:5177", "http://100.64.1.8:5177", "http://100.64.1.8:8095" };

        Assert.Equal(
            "http://localhost:5177",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://localhost:5177",
                "http://100.64.1.8:8095",
                allowed,
                allowHttpLoopbackPublicUrls: true));
        Assert.Equal(
            "http://localhost:5177",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://localhost:5177",
                "http://203.0.113.9:5177",
                allowed,
                allowHttpLoopbackPublicUrls: true));
        Assert.Equal(
            "http://localhost:5177",
            PlatformAuthCallbackResolver.AlignPublicBaseUrlWithRequestOrigin(
                "http://localhost:5177",
                "http://100.64.1.8:5177",
                allowed,
                allowHttpLoopbackPublicUrls: false));
    }

    [Fact]
    public void Tailscale_http_pos_origin_is_allowed_only_when_listed()
    {
        var allowed = new[] { "http://100.64.1.8:5177" };
        Assert.True(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl(
            "http://100.64.1.8:5177",
            allowHttpLoopbackPublicUrls: true,
            allowed));
        Assert.False(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl(
            "http://100.64.1.8:5177",
            allowHttpLoopbackPublicUrls: true));
        Assert.False(PlatformAuthCallbackResolver.IsAllowedPublicBaseUrl(
            "http://100.64.1.8:5177",
            allowHttpLoopbackPublicUrls: false,
            allowed));
    }

    [Fact]
    public void Pinoy_business_pos_surface_uses_aligned_tailscale_base()
    {
        var verification = Message(
            PlatformAuthOutboundMessageKinds.EmailVerification,
            PlatformAuthPublicSurfaces.PinoyBusinessPos);
        var allowed = new[] { "http://100.64.1.8:5177" };

        Assert.True(PlatformAuthCallbackResolver.TryCreateLink(
            verification,
            "http://127.0.0.1:8095",
            pinoyLoanManagerPublicBaseUrl: null,
            allowHttpLoopbackPublicUrls: true,
            out var activate,
            pinoyBusinessPosPublicBaseUrl: "http://100.64.1.8:5177",
            allowedHttpOrigins: allowed));
        Assert.Equal("http://100.64.1.8:5177/activate-account?token=opaque%2Ftoken", activate);
    }

    private static PlatformAuthOutboundMessage Message(string kind, string? publicSurface) =>
        new(kind, Guid.Empty, "user@example.com", "opaque/token", Expires, PublicSurface: publicSurface);
}
