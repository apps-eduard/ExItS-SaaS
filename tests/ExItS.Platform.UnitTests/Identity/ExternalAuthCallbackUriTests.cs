using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class ExternalAuthCallbackUriTests
{
    [Fact]
    public void TryCreate_uses_the_public_personal_origin_and_react_prefix()
    {
        var created = ExternalAuthCallbackUri.TryCreate("https://my.exitsapps.com", out var redirectUri);

        Assert.True(created);
        Assert.Equal(
            "https://my.exitsapps.com/platform-api/api/v1/platform/auth/external/google/callback",
            redirectUri);
    }

    [Fact]
    public void BrowserCompletePath_keeps_the_react_prefix_for_the_public_proxy()
    {
        Assert.Equal(
            "/platform-api/api/v1/platform/auth/external/google/complete",
            ExternalAuthCallbackUri.BrowserCompletePath("google", includeReactPathBase: true));
        Assert.Equal(
            "/api/v1/platform/auth/external/google/complete",
            ExternalAuthCallbackUri.BrowserCompletePath("google", includeReactPathBase: false));
    }

    [Fact]
    public void TryCreate_rejects_http_and_foreign_paths()
    {
        Assert.False(ExternalAuthCallbackUri.TryCreate("http://my.exitsapps.com", out _));
        Assert.False(ExternalAuthCallbackUri.TryCreate("https://my.exitsapps.com/other", out _));
        Assert.False(ExternalAuthCallbackUri.TryCreate("https://user:pass@my.exitsapps.com", out _));
    }
}
