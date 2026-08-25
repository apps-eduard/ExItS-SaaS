using ExItS.PinoyBusinessPOS.Api.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExItS.PinoyBusinessPOS.UnitTests.Api;

public sealed class PosCommercialValidationTests
{
    [Fact]
    public void Strict_mode_disables_development_grant_merge_in_testing()
    {
        var configuration = Config(("CommercialValidation:Strict", "true"));
        var environment = new TestHostEnvironment(Environments.Development);

        Assert.False(PosCommercialValidation.ShouldMergeDevelopmentGrants(environment, configuration));
        Assert.False(PosCommercialValidation.AllowsDevelopmentDefaultHeaders(environment, configuration));
    }

    [Fact]
    public void Non_strict_testing_still_allows_development_convenience()
    {
        var configuration = Config();
        var environment = new TestHostEnvironment(Environments.Development);

        Assert.True(PosCommercialValidation.ShouldMergeDevelopmentGrants(environment, configuration));
        Assert.True(PosCommercialValidation.AllowsDevelopmentDefaultHeaders(environment, configuration));
    }

    [Fact]
    public void Local_validation_merge_disabled_when_strict()
    {
        var configuration = Config(
            ("CommercialValidation:Strict", "true"),
            ("LocalValidation:Enabled", "true"));
        var environment = new TestHostEnvironment("Staging");

        Assert.False(PosCommercialValidation.ShouldMergeDevelopmentGrants(environment, configuration));
    }

    [Fact]
    public void Local_validation_merge_enabled_when_not_strict()
    {
        var configuration = Config(("LocalValidation:Enabled", "true"));
        var environment = new TestHostEnvironment("Staging");

        Assert.True(PosCommercialValidation.ShouldMergeDevelopmentGrants(environment, configuration));
    }

    private static IConfiguration Config(params (string Key, string Value)[] values)
    {
        var dict = values.ToDictionary(v => v.Key, v => (string?)v.Value, StringComparer.Ordinal);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
