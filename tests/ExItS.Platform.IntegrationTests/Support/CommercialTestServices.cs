using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests.Support;

internal static class CommercialTestServices
{
    public static ServiceProvider Build(string connectionString, DateTimeOffset? utcNow = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);

        services.AddScoped<CreateProduct>();
        services.AddScoped<ActivateProduct>();
        services.AddScoped<CreateFeatureDefinition>();
        services.AddScoped<CreatePlan>();
        services.AddScoped<ActivatePlan>();
        services.AddScoped<CreateDraftPlanVersion>();
        services.AddScoped<PublishExistingPlanVersion>();
        services.AddScoped<CreateTrialDefinition>();

        services.AddScoped<CreatePlatformOrganization>();
        services.AddScoped<SuspendPlatformOrganization>();
        services.AddScoped<OrganizationQueryService>();

        services.AddScoped<StartTrialSubscription>();
        services.AddScoped<ActivateSubscription>();
        services.AddScoped<EnterSubscriptionGracePeriod>();
        services.AddScoped<MarkSubscriptionPastDue>();
        services.AddScoped<SuspendSubscription>();
        services.AddScoped<ReactivateSubscription>();
        services.AddScoped<CancelSubscription>();
        services.AddScoped<ExpireSubscription>();
        services.AddScoped<SubscriptionQueryService>();

        services.AddSingleton<IClock>(new FixedUtcClock(utcNow ?? new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero)));

        return services.BuildServiceProvider();
    }

    private sealed class FixedUtcClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
