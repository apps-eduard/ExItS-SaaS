using ExItS.Platform.Api.Identity;
using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.IntegrationTests;

public sealed class PlatformSessionCookiePolicyTests
{
    [Fact]
    public void Production_http_cookie_remains_secure_even_when_local_validation_is_enabled()
    {
        var header = AppendCookie(scheme: "http", Environments.Production, localValidationEnabled: true);
        AssertSetCookieFlags(header, secure: true);
    }

    [Fact]
    public void Generic_staging_http_cookie_remains_secure()
    {
        var header = AppendCookie(scheme: "http", "Staging", localValidationEnabled: false);
        AssertSetCookieFlags(header, secure: true);
    }

    [Fact]
    public void Local_validation_http_is_the_insecure_cookie_exception()
    {
        var header = AppendCookie(scheme: "http", "Staging", localValidationEnabled: true);
        AssertSetCookieFlags(header, secure: false);
    }

    [Fact]
    public void Local_validation_https_cookie_is_secure()
    {
        var header = AppendCookie(scheme: "https", "Staging", localValidationEnabled: true);
        AssertSetCookieFlags(header, secure: true);
    }

    [Fact]
    public void Development_http_keeps_existing_allow_http_auth_cookies_behavior()
    {
        var header = AppendCookie(scheme: "http", Environments.Development, localValidationEnabled: false);
        AssertSetCookieFlags(header, secure: false);
    }

    private static string AppendCookie(string scheme, string environmentName, bool localValidationEnabled)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = scheme;
        var env = new TestHostEnvironment { EnvironmentName = environmentName };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalValidation:Enabled"] = localValidationEnabled ? "true" : "false"
            })
            .Build();
        var options = new PlatformSessionOptions { CookieName = ".ExItS.Platform.Auth" };

        AuthEndpoints.AppendSessionCookie(
            http,
            "session-token-value",
            DateTimeOffset.UtcNow.AddHours(1),
            options,
            env,
            configuration);

        Assert.True(http.Response.Headers.SetCookie.Count > 0);
        return http.Response.Headers.SetCookie.ToString();
    }

    private static void AssertSetCookieFlags(string header, bool secure)
    {
        Assert.Contains(".ExItS.Platform.Auth=", header, StringComparison.Ordinal);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionToken", header, StringComparison.OrdinalIgnoreCase);
        if (secure)
        {
            Assert.Matches("(?i)(^|;\\s)secure(;|$)", header);
        }
        else
        {
            Assert.DoesNotMatch("(?i)(^|;\\s)secure(;|$)", header);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Staging;
        public string ApplicationName { get; set; } = "ExItS.Platform.Api";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
