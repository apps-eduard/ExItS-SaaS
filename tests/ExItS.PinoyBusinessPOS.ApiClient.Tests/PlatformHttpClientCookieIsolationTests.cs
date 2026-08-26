namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

/// <summary>
/// PWEB-20 CSRF compatibility: Platform-facing POS clients must not replay
/// <c>.ExItS.Platform.Auth</c> from a cookie jar on introspect/bind/revoke.
/// </summary>
public sealed class PlatformHttpClientCookieIsolationTests
{
    [Fact]
    public void Platform_http_handler_disables_cookie_jar()
    {
        using var handler = DependencyInjection.CreatePlatformHttpMessageHandler();
        var sockets = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.False(sockets.UseCookies);
        Assert.False(sockets.AllowAutoRedirect);
    }

    [Fact]
    public void AddPosApiClient_source_disables_platform_cookie_jar()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "DependencyInjection.cs"));

        Assert.Contains("ConfigurePrimaryHttpMessageHandler(CreatePlatformHttpMessageHandler)", source, StringComparison.Ordinal);
        Assert.Contains("UseCookies = false", source, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
