using ExItS.Platform.Infrastructure.Security;
using ExItS.Platform.Infrastructure.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.UnitTests.Settings;

public sealed class PlatformDataProtectionTests
{
    [Fact]
    public void Persisted_key_ring_round_trips_platform_settings_secret_across_restarts()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), "exits-platform-dp-test", Guid.NewGuid().ToString("N"));
        try
        {
            var protectedValue = ProtectWithNewHost(keysPath, "smtp-secret-value");
            var roundTripped = UnprotectWithNewHost(keysPath, protectedValue);
            Assert.Equal("smtp-secret-value", roundTripped);
        }
        finally
        {
            if (Directory.Exists(keysPath))
            {
                Directory.Delete(keysPath, recursive: true);
            }
        }
    }

    [Fact]
    public void Production_startup_requires_configured_keys_path()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = new HostEnvironmentStub("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            PlatformDataProtectionServiceCollectionExtensions.AddPlatformDataProtection(
                new ServiceCollection(),
                configuration,
                environment));

        Assert.Contains("DataProtection:KeysPath", ex.Message, StringComparison.Ordinal);
    }

    private static string ProtectWithNewHost(string keysPath, string plaintext)
    {
        using var provider = BuildProvider(keysPath);
        var protector = new PlatformSettingsSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
        return protector.Protect(plaintext);
    }

    private static string UnprotectWithNewHost(string keysPath, string protectedValue)
    {
        using var provider = BuildProvider(keysPath);
        var protector = new PlatformSettingsSecretProtector(provider.GetRequiredService<IDataProtectionProvider>());
        return protector.Unprotect(protectedValue);
    }

    private static ServiceProvider BuildProvider(string keysPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [PlatformDataProtectionDefaults.KeysPathConfigurationKey] = keysPath,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformDataProtection(configuration, new HostEnvironmentStub("Staging"));
        return services.BuildServiceProvider();
    }

    private sealed class HostEnvironmentStub(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "ExItS.Platform.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
