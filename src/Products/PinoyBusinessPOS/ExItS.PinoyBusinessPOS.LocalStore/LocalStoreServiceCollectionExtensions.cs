using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.LocalStore;

public static class LocalStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers SQLite local-store infrastructure including the generic offline outbox.
    /// Callers must also register <see cref="ILocalStoreRootPathProvider"/> and <see cref="ISecureTokenStore"/>.
    /// </summary>
    public static IServiceCollection AddPinoyBusinessPosLocalStore(this IServiceCollection services)
    {
        services.AddSingleton<ILocalDatabasePathResolver, LocalDatabasePathResolver>();
        services.AddSingleton<ILocalDatabaseFactory, LocalDatabaseFactory>();
        services.AddSingleton<ILocalDatabaseMigrator, LocalDatabaseMigrator>();
        services.AddSingleton<ILocalContextManager, LocalContextManager>();
        services.AddSingleton<IDeviceIdentityProvider, DeviceIdentityProvider>();
        services.AddSingleton<ILocalPayloadProtector, AesGcmLocalPayloadProtector>();
        services.AddSingleton<IOfflineOperationQueue, OfflineOperationQueue>();
        services.AddSingleton<ILocalCustomerCreditStore, LocalEncryptedCustomerCreditStore>();
        services.AddSingleton<LocalSellingCatalogAndCashSaleStore>();
        services.AddSingleton<ILocalSellingCatalogStore>(sp => sp.GetRequiredService<LocalSellingCatalogAndCashSaleStore>());
        services.AddSingleton<ILocalCashSaleStore>(sp => sp.GetRequiredService<LocalSellingCatalogAndCashSaleStore>());
        services.AddSingleton<IOfflineRetryClassifier, OfflineRetryClassifier>();
        services.AddSingleton<IOfflineAccessRevalidator, OfflineAccessRevalidator>();
        services.AddSingleton<IOfflineQueueProcessor, OfflineQueueProcessor>();
        return services;
    }
}
