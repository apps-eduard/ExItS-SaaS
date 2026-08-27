using ExItS.PinoyBuyNowPayLater.Application.Common;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
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
    /// Registers customer and financing use cases. Call after registering repositories.
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
        services.TryAddScoped<CreateBnplFinancingApplication>();
        services.TryAddScoped<GetBnplFinancingApplication>();
        services.TryAddScoped<SearchBnplFinancingApplications>();
        services.TryAddScoped<UpdateBnplFinancingApplicationDraft>();
        services.TryAddScoped<SubmitBnplFinancingApplication>();
        services.TryAddScoped<ApproveBnplFinancingEligibility>();
        services.TryAddScoped<DeclineBnplFinancingEligibility>();
        services.TryAddScoped<CreateBnplFinancingOffer>();
        services.TryAddScoped<AttachBnplInstallmentPlan>();
        services.TryAddScoped<GetBnplInstallmentPlan>();
        services.TryAddScoped<AcceptBnplFinancingOffer>();
        services.TryAddScoped<ApproveBnplFinancingApplication>();
        services.TryAddScoped<DeclineBnplFinancingApplication>();
        services.TryAddScoped<CancelBnplFinancingApplication>();
        return services;
    }
}
