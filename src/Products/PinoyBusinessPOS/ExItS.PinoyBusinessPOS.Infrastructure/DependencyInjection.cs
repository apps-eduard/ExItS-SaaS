using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPosPersistence(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("PosDatabase")
            ?? throw new InvalidOperationException("Connection string 'PosDatabase' is not configured.");

        services.AddDbContext<PosDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPOSCustomerRepository, POSCustomerRepository>();
        services.AddScoped<IPosUnitOfWork, PosUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
