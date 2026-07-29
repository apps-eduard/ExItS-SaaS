using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformPersistence(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("PlatformDatabase")
            ?? throw new InvalidOperationException("Connection string 'PlatformDatabase' is not configured.");

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFeatureDefinitionRepository, FeatureDefinitionRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ITrialDefinitionRepository, TrialDefinitionRepository>();
        services.AddScoped<IPlatformOrganizationRepository, PlatformOrganizationRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISaaSPaymentRepository, SaaSPaymentRepository>();
        services.AddScoped<IFeatureOverrideRepository, FeatureOverrideRepository>();
        services.AddScoped<IEntitlementSnapshotRepository, EntitlementSnapshotRepository>();
        services.AddScoped<IAdminPortfolioReadStore, AdminPortfolioReadStore>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IProductAccessAssignmentRepository, ProductAccessAssignmentRepository>();
        services.AddScoped<IPlatformUnitOfWork, PlatformUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEntitlementRefreshPolicy, ProvisionalEntitlementRefreshPolicy>();

        return services;
    }
}
