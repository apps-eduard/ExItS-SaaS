using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBuyNowPayLater.Application;

public static class BnplApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddBnplApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IBnplClock, SystemBnplClock>();
        return services;
    }

    /// <summary>
    /// Registers customer use cases. Call after registering <see cref="IBnplCustomerRepository"/>.
    /// </summary>
    public static IServiceCollection AddBnplCustomerUseCases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IBnplClock, SystemBnplClock>();
        services.TryAddScoped<CreateBnplCustomer>();
        services.TryAddScoped<GetBnplCustomer>();
        services.TryAddScoped<SearchBnplCustomers>();
        services.TryAddScoped<UpdateBnplCustomerProfile>();
        services.TryAddScoped<LinkBnplCustomerPersonalIdentity>();
        services.TryAddScoped<LinkBnplCustomerCommerceReference>();
        return services;
    }
}
