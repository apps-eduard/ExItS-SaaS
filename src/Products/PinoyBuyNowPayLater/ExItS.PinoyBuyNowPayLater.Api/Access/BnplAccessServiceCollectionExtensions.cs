using ExItS.PinoyBuyNowPayLater.Application.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBuyNowPayLater.Api.Access;

public static class BnplAccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fail-closed BNPL operational access boundary.
    /// Default provider is unavailable until an approved trusted transport exists (D-P12-03).
    /// </summary>
    public static IServiceCollection AddBnplAccessBoundary(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IBnplAccessContextProvider, UnavailableBnplAccessContextProvider>();
        services.TryAddSingleton<IBnplOperationalAccessGuard, BnplOperationalAccessGuard>();
        return services;
    }
}
