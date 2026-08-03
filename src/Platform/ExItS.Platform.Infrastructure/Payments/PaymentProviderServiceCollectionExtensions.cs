using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Payments;

public static class PaymentProviderServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformPaymentProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<PaymentProviderOptions>(configuration.GetSection(PaymentProviderOptions.SectionName));

        var paymentsSection = configuration.GetSection(PaymentProviderOptions.SectionName);
        var provider = paymentsSection.GetValue<string>("Provider");
        var localValidationEnabled = configuration.GetValue<bool>("LocalValidation:Enabled");

        if (string.IsNullOrWhiteSpace(provider) || string.Equals(provider, PaymentProviderNames.None, StringComparison.OrdinalIgnoreCase))
        {
            if (localValidationEnabled && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
            {
                provider = PaymentProviderNames.LocalValidation;
            }
            else
            {
                provider = PaymentProviderNames.None;
            }
        }

        if (string.Equals(provider, PaymentProviderNames.LocalValidation, StringComparison.OrdinalIgnoreCase)
            && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Payments:Provider=LocalValidation is forbidden in Production.");
        }

        if (string.Equals(provider, PaymentProviderNames.LocalValidation, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IPaymentProvider, LocalValidationPaymentProvider>();
        }
        else
        {
            services.AddScoped<IPaymentProvider, NullPaymentProvider>();
        }

        return services;
    }
}
