using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
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

        services.AddScoped<CreateManualSaaSPayment>();
        services.AddScoped<ConfirmSaaSPayment>();
        services.AddScoped<RejectSaaSPayment>();
        services.AddScoped<VoidSaaSPayment>();
        services.AddScoped<ConfirmPaymentAndActivateSubscription>();
        services.AddScoped<SaaSPaymentQueryService>();

        services.AddScoped<CreateFeatureOverride>();
        services.AddScoped<RevokeFeatureOverride>();
        services.AddScoped<GenerateEntitlementSnapshot>();
        services.AddScoped<ReconcileEntitlementSnapshot>();
        services.AddScoped<EntitlementQueryService>();
        services.AddScoped<FeatureOverrideQueryService>();

        services.AddScoped<CatalogQueryService>();
        services.AddScoped<AdminPortfolioQueryService>();

        var clock = new TestUtcClock(utcNow ?? new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero));
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(clock);

        return services.BuildServiceProvider();
    }

    public sealed class TestUtcClock : IClock
    {
        public TestUtcClock(DateTimeOffset utcNow) => UtcNow = utcNow;

        public DateTimeOffset UtcNow { get; set; }

        public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
    }
}
