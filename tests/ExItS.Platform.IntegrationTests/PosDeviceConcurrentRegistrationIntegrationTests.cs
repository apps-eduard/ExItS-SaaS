using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PosDeviceConcurrentRegistrationIntegrationTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 22, 6, 0, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Concurrent_final_slot_registrations_allow_exactly_one_success()
    {
        await using var provider = BuildProvider(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();
        var startTrial = provider.GetRequiredService<StartTrialSubscription>();
        var branches = provider.GetRequiredService<IOrganizationBranchRepository>();
        var devices = provider.GetRequiredService<IPosDeviceRepository>();

        var productCode = ProductCode.PinoyBusinessPos;
        await createProduct.ExecuteAsync(productCode, "POS Device Concurrency");
        var plan = (await createPlan.ExecuteAsync(
            productCode,
            "single-device",
            "Single Device",
            description: null,
            maxBranches: 1,
            maxActiveStaff: 3,
            maxActivePosDevices: 1,
            maxActiveBusinessTypes: 1,
            customerCreditEnabled: false,
            advancedReportsEnabled: false,
            exportEnabled: false,
            trialAllowed: true,
            defaultTrialDays: 14,
            sortOrder: 10,
            monthlyPrice: 0m,
            annualPrice: 0m,
            currencyCode: "PHP")).Value!;
        await activatePlan.ExecuteAsync(plan.Id);
        var version = (await createVersion
            .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, Array.Empty<FeatureGrantSpec>(), T0)
            .ConfigureAwait(false)).Value!;
        await publish.ExecuteAsync(plan.Id, 1).ConfigureAwait(false);
        var trial = (await createTrial
            .ExecuteAsync(productCode, "Trial", TimeSpan.FromDays(14), Array.Empty<FeatureGrantSpec>(), Array.Empty<FeatureGrantSpec>())
            .ConfigureAwait(false)).Value!;

        var org = (await createOrg.ExecuteAsync("Device Concurrency Org", Unique("devconcorg")).ConfigureAwait(false)).Value!;
        Assert.True((await startTrial.ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id).ConfigureAwait(false)).IsSuccess);

        var uow = provider.GetRequiredService<IPlatformUnitOfWork>();
        var branch = OrganizationBranch.CreateMainBranch(org.Id, T0);
        await branches.AddAsync(branch).ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);

        var tasks = Enumerable.Range(0, 2).Select(async index =>
        {
            await using var scope = provider.CreateAsyncScope();
            var scopedRegister = scope.ServiceProvider.GetRequiredService<RegisterCurrentDevice>();
            return await scopedRegister.ExecuteAsync(
                org.Id,
                new RegisterPosDeviceCommand(
                    branch.Id.Value,
                    $"install-concurrent-{index}-{Guid.NewGuid():N}",
                    $"Device {index}")).ConfigureAwait(false);
        });
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        Assert.Equal(1, results.Count(result => result.IsSuccess));
        Assert.Equal(
            1,
            results.Count(result => result.ErrorCode == ApplicationErrorCodes.PosDeviceCapacityExceeded));
        Assert.Equal(1, await devices.CountActiveAsync(org.Id).ConfigureAwait(false));
    }

    private static ServiceProvider BuildProvider(string connectionString)
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
        services.AddScoped<StartTrialSubscription>();
        services.AddScoped<RegisterCurrentDevice>();
        services.AddSingleton<IClock>(new CommercialTestServices.TestUtcClock(T0));
        return services.BuildServiceProvider();
    }
}
