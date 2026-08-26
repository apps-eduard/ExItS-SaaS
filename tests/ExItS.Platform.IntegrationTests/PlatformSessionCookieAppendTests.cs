using ExItS.Platform.Api.Identity;
using ExItS.Platform.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.IntegrationTests;

public sealed class PlatformSessionCookieAppendTests
{
    private static readonly PlatformSessionOptions Options = new()
    {
        CookieName = ".ExItS.Platform.Auth"
    };

    [Fact]
    public void Production_http_set_cookie_is_secure_httponly_and_samesite_lax()
    {
        var header = Append(
            environmentName: Environments.Production,
            localValidationEnabled: false,
            https: false);

        Assert.True(HasFlag(header, "httponly"));
        Assert.True(HasFlag(header, "secure"));
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".ExItS.Platform.Auth=", header, StringComparison.Ordinal);
    }

    [Fact]
    public void Generic_staging_http_set_cookie_stays_secure()
    {
        var header = Append(
            environmentName: Environments.Staging,
            localValidationEnabled: false,
            https: false);

        Assert.True(HasFlag(header, "httponly"));
        Assert.True(HasFlag(header, "secure"));
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_validation_http_set_cookie_omits_secure_and_keeps_httponly_samesite()
    {
        var header = Append(
            environmentName: Environments.Staging,
            localValidationEnabled: true,
            https: false);

        Assert.True(HasFlag(header, "httponly"));
        Assert.False(HasFlag(header, "secure"));
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_validation_https_set_cookie_is_secure()
    {
        var header = Append(
            environmentName: Environments.Staging,
            localValidationEnabled: true,
            https: true);

        Assert.True(HasFlag(header, "httponly"));
        Assert.True(HasFlag(header, "secure"));
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_validation_disabled_staging_http_does_not_use_http_exception()
    {
        Assert.False(PlatformSessionCookiePolicy.AllowHttpAuthCookies(
            isProduction: false,
            isDevelopment: false,
            isTesting: false,
            localValidationEnabled: false));
        var header = Append(
            environmentName: Environments.Staging,
            localValidationEnabled: false,
            https: false);
        Assert.True(HasFlag(header, "secure"));
    }

    private static string Append(string environmentName, bool localValidationEnabled, bool https)
    {
        var http = new DefaultHttpContext();
        http.Request.Scheme = https ? "https" : "http";
        var env = new StubHostEnvironment { EnvironmentName = environmentName };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LocalValidation:Enabled"] = localValidationEnabled ? "true" : "false"
            })
            .Build();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .BuildServiceProvider();

        AuthEndpoints.AppendSessionCookie(
            http,
            "opaque-session-token",
            DateTimeOffset.UtcNow.AddHours(1),
            Options,
            env,
            config);

        return http.Response.Headers.SetCookie.ToString();
    }

    private static bool HasFlag(string setCookie, string flag) =>
        setCookie
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, flag, StringComparison.OrdinalIgnoreCase));

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Staging;
        public string ApplicationName { get; set; } = "ExItS.Platform.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
