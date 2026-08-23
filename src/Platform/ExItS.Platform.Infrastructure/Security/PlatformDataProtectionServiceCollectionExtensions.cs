using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Security;

public static class PlatformDataProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var keysPath = configuration[PlatformDataProtectionDefaults.KeysPathConfigurationKey];
        var dataProtection = services
            .AddDataProtection()
            .SetApplicationName(PlatformDataProtectionDefaults.ApplicationName);

        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            return services;
        }

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Production requires DataProtection:KeysPath for persisted encryption key storage.");
        }

        return services;
    }
}
