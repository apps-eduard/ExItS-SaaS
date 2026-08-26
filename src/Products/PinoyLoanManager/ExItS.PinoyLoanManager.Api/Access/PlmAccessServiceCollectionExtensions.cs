using ExItS.PinoyLoanManager.Application.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyLoanManager.Api.Access;

public static class PlmAccessServiceCollectionExtensions
{
    /// <summary>
    /// Registers the fail-closed PLM operational access boundary.
    /// Default provider is unavailable until an approved D-P12-03 transport exists.
    /// </summary>
    public static IServiceCollection AddPlmAccessBoundary(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IPlmAccessContextProvider, UnavailablePlmAccessContextProvider>();
        services.TryAddSingleton<IPlmOperationalAccessGuard, PlmOperationalAccessGuard>();
        return services;
    }
}
