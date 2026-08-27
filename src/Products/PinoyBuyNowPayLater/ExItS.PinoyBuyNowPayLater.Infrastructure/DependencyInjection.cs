using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure;

public static class BnplInfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "BnplDatabase";

    /// <summary>
    /// Registers BNPL operational persistence and customer use cases. Does not call Database.Migrate().
    /// </summary>
    public static IServiceCollection AddBnplPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<BnplDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IBnplCustomerRepository, BnplCustomerRepository>();
        services.AddScoped<IBnplFinancingApplicationRepository, BnplFinancingApplicationRepository>();
        services.AddScoped<IBnplUnitOfWork, BnplUnitOfWork>();
        services.AddBnplCustomerUseCases();
        return services;
    }

    /// <summary>
    /// Registers persistence when <c>BnplDatabase</c> is configured; otherwise leaves customer
    /// repositories unregistered (health/access-only hosts remain valid).
    /// </summary>
    public static IServiceCollection AddBnplPersistenceIfConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        return services.AddBnplPersistence(configuration);
    }
}
