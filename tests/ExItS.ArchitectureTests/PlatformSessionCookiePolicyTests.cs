namespace ExItS.ArchitectureTests;

public sealed class PlatformSessionCookiePolicyTests
{
    [Fact]
    public void Production_http_cookie_remains_secure()
    {
        Assert.True(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: true,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: false,
            requestIsHttps: false));
        Assert.True(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: true,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: true,
            requestIsHttps: false));
    }

    [Fact]
    public void Generic_staging_http_does_not_become_insecure()
    {
        Assert.True(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: false,
            requestIsHttps: false));
    }

    [Fact]
    public void Local_validation_http_allows_non_secure_cookie()
    {
        Assert.False(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: true,
            requestIsHttps: false));
    }

    [Fact]
    public void Local_validation_https_stays_secure()
    {
        Assert.True(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: true,
            requestIsHttps: true));
    }

    [Fact]
    public void Local_validation_disabled_keeps_staging_http_secure()
    {
        Assert.False(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.AllowHttpAuthCookies(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: false));
        Assert.True(ExItS.Platform.Api.Identity.PlatformSessionCookiePolicy.IsSecure(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: false,
            requestIsHttps: false));
    }
}

public sealed class MobileReactBrowserAuthArchitectureTests
{
    [Fact]
    public void Platform_session_cookie_uses_shared_local_validation_http_policy()
    {
        var root = FindRepositoryRoot();
        var policy = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Identity", "PlatformSessionCookiePolicy.cs"));
        var webUi = File.ReadAllText(Path.Combine(
            root, "src", "Shared", "ExItS.Web.UI", "ExItSLocalValidationCookies.cs"));
        var auth = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Identity", "AuthEndpoints.cs"));

        Assert.Contains("LocalValidation:Enabled", policy, StringComparison.Ordinal);
        Assert.Contains("IsProduction()", policy, StringComparison.Ordinal);
        Assert.Contains("localValidationEnabled && !isProduction", policy, StringComparison.Ordinal);
        Assert.Contains("LocalValidation:Enabled", webUi, StringComparison.Ordinal);
        Assert.Contains("!environment.IsProduction()", webUi, StringComparison.Ordinal);
        Assert.Contains("SessionCookieSecure", webUi, StringComparison.Ordinal);
        Assert.Contains("PlatformSessionCookiePolicy.IsSecure", auth, StringComparison.Ordinal);
        Assert.Contains("HttpOnly = true", auth, StringComparison.Ordinal);
        Assert.Contains("SameSite = SameSiteMode.Lax", auth, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "var secure = !(env.IsDevelopment() || env.IsEnvironment(\"Testing\"))",
            auth,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_cors_does_not_add_react_dev_origins()
    {
        var root = FindRepositoryRoot();
        var launch = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Properties", "launchSettings.json"));
        var pipeline = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformSecurityPipeline.cs"));
        Assert.DoesNotContain("5175", launch, StringComparison.Ordinal);
        Assert.DoesNotContain("4175", launch, StringComparison.Ordinal);
        Assert.DoesNotContain("5175", pipeline, StringComparison.Ordinal);
        Assert.DoesNotContain("4175", pipeline, StringComparison.Ordinal);
        Assert.Contains("http://localhost:8090", launch, StringComparison.Ordinal);
        Assert.Contains("http://localhost:8093", launch, StringComparison.Ordinal);
        Assert.Contains("http://localhost:8094", launch, StringComparison.Ordinal);
    }

    [Fact]
    public void React_client_uses_same_origin_platform_proxy()
    {
        var root = FindRepositoryRoot();
        var client = Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Client");
        var http = File.ReadAllText(Path.Combine(client, "src", "api", "http.ts"));
        var platformHttp = File.ReadAllText(Path.Combine(client, "src", "api", "platform", "platform-http.ts"));
        var proxyHelpers = File.ReadAllText(Path.Combine(client, "src", "pwa", "platform-api-proxy.ts"));
        var viteProxy = File.ReadAllText(Path.Combine(client, "vite.platform-api-proxy.ts"));
        var vite = File.ReadAllText(Path.Combine(client, "vite.config.ts"));
        var browserSession = File.ReadAllText(Path.Combine(client, "src", "api", "platform", "browser-session.ts"));

        Assert.Contains("\"/platform-api\"", http, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_BASE_PATH = \"/platform-api\"", platformHttp, StringComparison.Ordinal);
        Assert.Contains("must stay on the relative /platform-api origin", platformHttp, StringComparison.Ordinal);
        Assert.Contains("createPlatformApiProxy", vite, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_PROXY_PREFIX", viteProxy, StringComparison.Ordinal);
        Assert.Contains("resolvePlatformApiProxyTarget", viteProxy, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1", viteProxy, StringComparison.Ordinal);
        Assert.Contains("localhost", viteProxy, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1", proxyHelpers, StringComparison.Ordinal);
        Assert.Contains("localhost", proxyHelpers, StringComparison.Ordinal);
        Assert.DoesNotContain("10.0.2.2:8091", viteProxy, StringComparison.Ordinal);
        Assert.DoesNotContain("10.0.2.2:8091", proxyHelpers, StringComparison.Ordinal);
        Assert.Contains("toBrowserSessionSnapshot", browserSession, StringComparison.Ordinal);
        Assert.Contains("sessionToken", browserSession, StringComparison.Ordinal);
        Assert.Contains("delete safe.sessionToken", browserSession, StringComparison.Ordinal);
        Assert.Contains("assertBrowserStorageHasNoSessionToken", browserSession, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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
