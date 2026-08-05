using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class ExternalAuthReturnUrlTests
{
    [Fact]
    public void Sanitize_defaults_blank_to_admin_callback()
    {
        Assert.Equal(ExternalAuthReturnUrl.DefaultAdminCallback, ExternalAuthReturnUrl.Sanitize(null, false));
        Assert.Equal(ExternalAuthReturnUrl.DefaultAdminCallback, ExternalAuthReturnUrl.Sanitize("  ", true));
    }

    [Fact]
    public void Sanitize_keeps_relative_admin_paths()
    {
        Assert.Equal("/admin/external-login-callback", ExternalAuthReturnUrl.Sanitize("/admin/external-login-callback", false));
    }

    [Fact]
    public void Sanitize_allows_maui_callback_scheme()
    {
        Assert.Equal(
            ExternalAuthReturnUrl.MauiCallbackUrl,
            ExternalAuthReturnUrl.Sanitize("exitspos://auth/callback?ignored=1", allowDevLocalhostAbsolute: false));
    }

    [Fact]
    public void Sanitize_blocks_arbitrary_absolute_urls()
    {
        Assert.Equal(
            ExternalAuthReturnUrl.DefaultAdminCallback,
            ExternalAuthReturnUrl.Sanitize("https://evil.example/phish", allowDevLocalhostAbsolute: true));
    }

    [Fact]
    public void Sanitize_allows_localhost_only_when_dev_flag_set()
    {
        Assert.Equal(
            "http://localhost:5100/admin/external-login-callback",
            ExternalAuthReturnUrl.Sanitize("http://localhost:5100/admin/external-login-callback", true));
        Assert.Equal(
            ExternalAuthReturnUrl.DefaultAdminCallback,
            ExternalAuthReturnUrl.Sanitize("http://localhost:5100/admin/external-login-callback", false));
    }
}
