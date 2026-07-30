using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.LocalStore;

public static class LocalStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers SQLite local-store infrastructure. Callers must also register
    /// <see cref="ILocalStoreRootPathProvider"/> (MAUI sandbox path).
    /// </summary>
    public static IServiceCollection AddPinoyBusinessPosLocalStore(this IServiceCollection services)
    {
        services.AddSingleton<ILocalDatabasePathResolver, LocalDatabasePathResolver>();
        services.AddSingleton<ILocalDatabaseFactory, LocalDatabaseFactory>();
        services.AddSingleton<ILocalDatabaseMigrator, LocalDatabaseMigrator>();
        services.AddSingleton<ILocalContextManager, LocalContextManager>();
        services.AddSingleton<IDeviceIdentityProvider, DeviceIdentityProvider>();
        return services;
    }
}
