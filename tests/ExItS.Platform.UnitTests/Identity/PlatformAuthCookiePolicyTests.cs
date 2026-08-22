using ExItS.Platform.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthCookiePolicyTests
{
    [Fact]
    public void Production_always_requires_secure_session_cookie()
    {
        var env = new FakeHostEnvironment(Environments.Production);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LocalValidation:Enabled"] = "false" })
            .Build();
        var request = new DefaultHttpContext { Request = { IsHttps = false } }.Request;

        Assert.True(PlatformAuthCookiePolicy.SessionCookieSecure(request, env, config));
    }

    [Fact]
    public void Staging_without_local_validation_requires_secure_session_cookie()
    {
        var env = new FakeHostEnvironment("Staging");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LocalValidation:Enabled"] = "false" })
            .Build();
        var request = new DefaultHttpContext { Request = { IsHttps = false } }.Request;

        Assert.True(PlatformAuthCookiePolicy.SessionCookieSecure(request, env, config));
    }

    [Fact]
    public void Local_validation_staging_allows_http_session_cookie()
    {
        var env = new FakeHostEnvironment("Staging");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LocalValidation:Enabled"] = "true" })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.IsHttps = false;

        Assert.False(PlatformAuthCookiePolicy.SessionCookieSecure(httpContext.Request, env, config));
    }

    [Fact]
    public void Local_validation_staging_still_secures_session_cookie_on_https()
    {
        var env = new FakeHostEnvironment("Staging");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LocalValidation:Enabled"] = "true" })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.IsHttps = true;

        Assert.True(PlatformAuthCookiePolicy.SessionCookieSecure(httpContext.Request, env, config));
    }

    [Fact]
    public void Local_validation_never_relaxes_production_secure_policy()
    {
        var env = new FakeHostEnvironment(Environments.Production);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LocalValidation:Enabled"] = "true" })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.IsHttps = false;

        Assert.True(PlatformAuthCookiePolicy.SessionCookieSecure(httpContext.Request, env, config));
    }

    [Fact]
    public void Development_uses_same_as_request()
    {
        var env = new FakeHostEnvironment(Environments.Development);
        var config = new ConfigurationBuilder().Build();
        var http = new DefaultHttpContext();
        http.Request.IsHttps = false;
        Assert.False(PlatformAuthCookiePolicy.SessionCookieSecure(http.Request, env, config));

        http.Request.IsHttps = true;
        Assert.True(PlatformAuthCookiePolicy.SessionCookieSecure(http.Request, env, config));
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
