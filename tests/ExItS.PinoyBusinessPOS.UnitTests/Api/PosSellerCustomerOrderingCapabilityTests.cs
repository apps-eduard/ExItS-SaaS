using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.UnitTests.Api;

public sealed class PosSellerCustomerOrderingCapabilityTests
{
    [Fact]
    public async Task Strict_testing_respects_commercial_grants_for_delivery()
    {
        var accessor = new PosCommercialAccessAccessor
        {
            Current = new PosCommercialAccess(
                PosSubscriptionStatuses.Active,
                [PosFeatureCodes.StoreCustomerOrdering],
                IsKnown: true)
        };

        var capability = CreateCapability(strict: true, accessor);
        var result = await capability.ResolveAsync(Guid.NewGuid());

        Assert.True(result.CanCustomerOrder);
        Assert.False(result.CanCustomerDelivery);
    }

    [Fact]
    public async Task Strict_testing_allows_delivery_when_delivery_grant_present()
    {
        var accessor = new PosCommercialAccessAccessor
        {
            Current = new PosCommercialAccess(
                PosSubscriptionStatuses.Active,
                [PosFeatureCodes.StoreCustomerOrdering, PosFeatureCodes.StoreDeliveryOrders],
                IsKnown: true)
        };

        var capability = CreateCapability(strict: true, accessor);
        var result = await capability.ResolveAsync(Guid.NewGuid());

        Assert.True(result.CanCustomerOrder);
        Assert.True(result.CanCustomerDelivery);
    }

    [Fact]
    public async Task Strict_testing_denies_ordering_without_grants()
    {
        var accessor = new PosCommercialAccessAccessor
        {
            Current = new PosCommercialAccess(
                PosSubscriptionStatuses.Active,
                [],
                IsKnown: true)
        };

        var capability = CreateCapability(strict: true, accessor);
        var result = await capability.ResolveAsync(Guid.NewGuid());

        Assert.False(result.CanCustomerOrder);
        Assert.False(result.CanCustomerDelivery);
    }

    [Fact]
    public async Task Non_strict_testing_keeps_ordering_convenience_bypass()
    {
        var accessor = new PosCommercialAccessAccessor
        {
            Current = PosCommercialAccess.Unknown
        };

        var capability = CreateCapability(strict: false, accessor);
        var result = await capability.ResolveAsync(Guid.NewGuid());

        Assert.True(result.CanCustomerOrder);
        Assert.True(result.CanCustomerDelivery);
    }

    private static PosSellerCustomerOrderingCapability CreateCapability(
        bool strict,
        IPosCommercialAccessAccessor accessor)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [PosCommercialValidation.StrictConfigKey] = strict ? "true" : "false"
            })
            .Build();

        return new PosSellerCustomerOrderingCapability(
            new HttpClient(),
            new HttpContextAccessor(),
            Options.Create(new PlatformAuthOptions()),
            new TestHostEnvironment("Testing"),
            configuration,
            accessor);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
