using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.IntegrationTests.Support;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPlatformSpineCollection.Name)]
public sealed class PosPlatformCommercialSpineIntegrationTests(PosPlatformSpineFixture fixture) : IAsyncLifetime
{
    private PlatformCommercialSpineSupport.PlatformSpineApiFactory _platformFactory = null!;
    private HttpClient _platformClient = null!;

    public Task InitializeAsync()
    {
        return EnsurePlatformReadyAsync();
    }

    private async Task EnsurePlatformReadyAsync()
    {
        PlatformTokenIntrospectionClient.ClearCacheForTests();
        await PlatformCommercialSpineSupport.EnsureMvpCatalogAsync(fixture.PlatformConnectionString);
        _platformFactory = new PlatformCommercialSpineSupport.PlatformSpineApiFactory(fixture.PlatformConnectionString);
        _platformClient = _platformFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
    }

    public Task DisposeAsync()
    {
        _platformClient.Dispose();
        _platformFactory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Real_growth_introspection_includes_advanced_credit_and_ordering_grants()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-growth");
        var introspection = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            growth.AccessToken);

        Assert.True(introspection.GetProperty("active").GetBoolean());
        Assert.True(introspection.GetProperty("productAccessAllowed").GetBoolean());
        Assert.Equal("Trialing", introspection.GetProperty("subscriptionStatus").GetString());
        Assert.True(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.CustomerCreditCreate));
        Assert.True(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.StoreAdvancedReports));
        Assert.True(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.StoreCustomerOrdering));
        Assert.True(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.StoreDeliveryOrders));
    }

    [Fact]
    public async Task Real_starter_introspection_excludes_credit_and_advanced_reports()
    {
        var starter = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Starter,
            "spine-starter");
        var introspection = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            starter.AccessToken);

        Assert.False(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.CustomerCreditCreate));
        Assert.False(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.StoreAdvancedReports));
        Assert.False(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            introspection,
            FeatureCode.StoreExport));
    }

    [Fact]
    public async Task Real_growth_device_registration_allows_three_then_blocks_fourth()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-dev");
        var branchId = await GetMainBranchIdAsync(growth);

        for (var index = 0; index < 3; index++)
        {
            var result = await PlatformCommercialSpineSupport.TryRegisterDeviceAsync(
                fixture.PlatformConnectionString,
                growth.OrganizationId,
                branchId,
                $"install-growth-{index}-{Guid.NewGuid():N}",
                $"Growth Device {index}");
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        var blocked = await PlatformCommercialSpineSupport.TryRegisterDeviceAsync(
            fixture.PlatformConnectionString,
            growth.OrganizationId,
            branchId,
            $"install-growth-blocked-{Guid.NewGuid():N}",
            "Growth Device Blocked");
        Assert.False(blocked.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceCapacityExceeded, blocked.ErrorCode);
    }

    [Fact]
    public async Task Real_growth_to_pro_upgrade_increases_device_capacity()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-up");
        var branchId = await GetMainBranchIdAsync(growth);

        for (var index = 0; index < 3; index++)
        {
            await PlatformCommercialSpineSupport.RegisterDeviceAsync(
                fixture.PlatformConnectionString,
                growth.OrganizationId,
                branchId,
                $"install-up-{index}-{Guid.NewGuid():N}",
                $"Upgrade Device {index}");
        }

        using var upgrade = PlatformCommercialSpineSupport.Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{growth.OrganizationId:D}/subscriptions/{growth.SubscriptionId:D}/upgrade",
            growth.SessionToken,
            new
            {
                planKey = MvpPosPlanCodes.Pro,
                billingCycle = "Monthly",
                idempotencyKey = $"upgrade-{Guid.NewGuid():N}"
            });
        (await _platformClient.SendAsync(upgrade)).EnsureSuccessStatusCode();

        var branchAfterUpgrade = await GetMainBranchIdAsync(growth);
        var fourth = await PlatformCommercialSpineSupport.TryRegisterDeviceAsync(
            fixture.PlatformConnectionString,
            growth.OrganizationId,
            branchAfterUpgrade,
            $"install-up-4-{Guid.NewGuid():N}",
            "Upgrade Device 4");
        Assert.True(fourth.IsSuccess, fourth.ErrorMessage);
    }

    [Fact]
    public async Task Real_suspend_blocks_pos_catalog_via_bearer_introspection()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-sus");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        using (var catalog = PlatformCommercialSpineSupport.PosBearerGet(
                   "/api/v1/pos/catalog/products",
                   growth.AccessToken))
        {
            (await posClient.Client.SendAsync(catalog)).EnsureSuccessStatusCode();
        }

        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/suspend",
            null)).EnsureSuccessStatusCode();

        var refreshedToken = await PlatformCommercialSpineSupport.IssueProductAccessTokenAsync(
            _platformClient,
            growth.SessionToken,
            growth.OrganizationId);
        var introspection = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            refreshedToken);
        Assert.Equal("Suspended", introspection.GetProperty("subscriptionStatus").GetString());

        using var blockedCatalog = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/catalog/products",
            refreshedToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await posClient.Client.SendAsync(blockedCatalog)).StatusCode);
    }

    [Fact]
    public async Task Real_reactivate_restores_pos_catalog_authorization()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-react");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/suspend",
            null)).EnsureSuccessStatusCode();
        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/reactivate",
            null)).EnsureSuccessStatusCode();

        var refreshedToken = await PlatformCommercialSpineSupport.IssueProductAccessTokenAsync(
            _platformClient,
            growth.SessionToken,
            growth.OrganizationId);

        using var catalog = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/catalog/products",
            refreshedToken);
        (await posClient.Client.SendAsync(catalog)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Real_starter_pos_blocks_operational_report()
    {
        var starter = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Starter,
            "spine-stpos");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        using var classic = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/reports/sales?fromDate=2026-07-01&toDate=2026-07-01",
            starter.AccessToken);
        (await posClient.Client.SendAsync(classic)).EnsureSuccessStatusCode();

        using var advanced = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            starter.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await posClient.Client.SendAsync(advanced)).StatusCode);
    }

    [Fact]
    public async Task Real_growth_pos_allows_operational_report_via_introspection_grants()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-grpos");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        using var advanced = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            growth.AccessToken);
        (await posClient.Client.SendAsync(advanced)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Real_cross_org_device_capacity_is_isolated()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-xg");
        var starter = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Starter,
            "spine-xs");

        var growthBranch = await GetMainBranchIdAsync(growth);
        var starterBranch = await GetMainBranchIdAsync(starter);

        for (var index = 0; index < 3; index++)
        {
            await PlatformCommercialSpineSupport.RegisterDeviceAsync(
                fixture.PlatformConnectionString,
                growth.OrganizationId,
                growthBranch,
                $"install-xg-{index}-{Guid.NewGuid():N}",
                $"Growth {index}");
        }

        await PlatformCommercialSpineSupport.RegisterDeviceAsync(
            fixture.PlatformConnectionString,
            starter.OrganizationId,
            starterBranch,
            $"install-xs-0-{Guid.NewGuid():N}",
            "Starter 0");

        var starterSecond = await PlatformCommercialSpineSupport.TryRegisterDeviceAsync(
            fixture.PlatformConnectionString,
            starter.OrganizationId,
            starterBranch,
            $"install-xs-1-{Guid.NewGuid():N}",
            "Starter 1");
        Assert.False(starterSecond.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PosDeviceCapacityExceeded, starterSecond.ErrorCode);
    }

    [Fact]
    public async Task Real_cross_org_advanced_report_entitlements_do_not_leak()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-xgr");
        var starter = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Starter,
            "spine-xsr");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        using var growthReport = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            growth.AccessToken);
        (await posClient.Client.SendAsync(growthReport)).EnsureSuccessStatusCode();

        PlatformTokenIntrospectionClient.ClearCacheForTests();
        await using var starterPosClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        var starterIntro = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            starter.AccessToken);
        Assert.False(PlatformCommercialSpineSupport.IntrospectionHasFeature(
            starterIntro,
            FeatureCode.StoreAdvancedReports));

        using var starterReport = PlatformCommercialSpineSupport.PosBearerGet(
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            starter.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await starterPosClient.Client.SendAsync(starterReport)).StatusCode);
    }

    [Fact]
    public async Task Real_fresh_access_token_reflects_subscription_change_without_polling()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-refresh");
        var before = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            growth.AccessToken);
        Assert.Equal("Trialing", before.GetProperty("subscriptionStatus").GetString());

        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/suspend",
            null)).EnsureSuccessStatusCode();

        var after = await PlatformCommercialSpineSupport.IntrospectAsync(
            _platformClient,
            await PlatformCommercialSpineSupport.IssueProductAccessTokenAsync(
                _platformClient,
                growth.SessionToken,
                growth.OrganizationId));
        Assert.Equal("Suspended", after.GetProperty("subscriptionStatus").GetString());
    }

    [Fact]
    public async Task Real_suspend_blocks_pos_sale_mutation_via_bearer_introspection()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-sale-sus");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);
        var product = await PosSpinePosApiHelpers.CreateCatalogProductAsync(
            posClient.Client,
            growth.AccessToken);
        await PosSpinePosApiHelpers.EnsureOpenShiftAsync(posClient.Client, growth.AccessToken);

        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/suspend",
            null)).EnsureSuccessStatusCode();

        var refreshedToken = await PlatformCommercialSpineSupport.IssueProductAccessTokenAsync(
            _platformClient,
            growth.SessionToken,
            growth.OrganizationId);

        using var blockedSale = await PosSpinePosApiHelpers.CheckoutAsync(
            posClient.Client,
            refreshedToken,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                50m));
        Assert.Equal(HttpStatusCode.Forbidden, blockedSale.StatusCode);
    }

    [Fact]
    public async Task Real_suspend_blocks_new_utang_credit_via_bearer_introspection()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-cred-sus");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);
        var customer = await PosSpinePosApiHelpers.CreateCustomerAsync(
            posClient.Client,
            growth.AccessToken,
            "Suspend Credit Customer");

        (await _platformClient.PostAsync(
            $"/api/v1/platform/subscriptions/{growth.SubscriptionId:D}/suspend",
            null)).EnsureSuccessStatusCode();

        var refreshedToken = await PlatformCommercialSpineSupport.IssueProductAccessTokenAsync(
            _platformClient,
            growth.SessionToken,
            growth.OrganizationId);

        using var blockedCredit = await PosSpinePosApiHelpers.CreateCreditEntryAsync(
            posClient.Client,
            refreshedToken,
            customer.CustomerId,
            25m,
            "blocked");
        Assert.Equal(HttpStatusCode.Forbidden, blockedCredit.StatusCode);
    }

    [Fact]
    public async Task Real_starter_pos_allows_cash_and_gcash_but_denies_utang()
    {
        var starter = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Starter,
            "spine-st-sale");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);
        var product = await PosSpinePosApiHelpers.CreateCatalogProductAsync(
            posClient.Client,
            starter.AccessToken,
            "starter-sale");
        await PosSpinePosApiHelpers.EnsureOpenShiftAsync(posClient.Client, starter.AccessToken);

        using var cashSale = await PosSpinePosApiHelpers.CheckoutAsync(
            posClient.Client,
            starter.AccessToken,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                50m));
        Assert.Equal(HttpStatusCode.Created, cashSale.StatusCode);

        using var gcashSale = await PosSpinePosApiHelpers.CheckoutAsync(
            posClient.Client,
            starter.AccessToken,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.ManualGCashPaymentMethod,
                GCashReference: "GCASH-SPINE-1"));
        Assert.Equal(HttpStatusCode.Created, gcashSale.StatusCode);

        using var utangSale = await PosSpinePosApiHelpers.CheckoutAsync(
            posClient.Client,
            starter.AccessToken,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, utangSale.StatusCode);
    }

    [Fact]
    public async Task Real_growth_pos_allows_utang_checkout_via_introspection_grants()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-gr-utang");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);
        var product = await PosSpinePosApiHelpers.CreateCatalogProductAsync(
            posClient.Client,
            growth.AccessToken,
            "growth-utang");
        await PosSpinePosApiHelpers.EnsureOpenShiftAsync(posClient.Client, growth.AccessToken);
        var customer = await PosSpinePosApiHelpers.CreateCustomerAsync(
            posClient.Client,
            growth.AccessToken,
            "Growth Utang Customer");

        using var utangSale = await PosSpinePosApiHelpers.CheckoutAsync(
            posClient.Client,
            growth.AccessToken,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer.CustomerId));
        Assert.Equal(HttpStatusCode.Created, utangSale.StatusCode);
    }

    [Fact]
    public async Task Real_ordering_and_delivery_grants_authorize_pos_customer_order_routes()
    {
        var growth = await PlatformCommercialSpineSupport.StartMvpBusinessAsync(
            _platformClient,
            MvpPosPlanCodes.Growth,
            "spine-order");
        await using var posClient = PlatformCommercialSpineSupport.CreatePosClientScope(
            fixture.PosConnectionString,
            _platformFactory);

        using var listOrders = PosSpinePosApiHelpers.PosBearer(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{growth.OrganizationId:D}/customer-orders",
            growth.AccessToken);
        (await posClient.Client.SendAsync(listOrders)).EnsureSuccessStatusCode();

        var orderId = Guid.NewGuid();
        using var accept = PosSpinePosApiHelpers.PosBearer(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{growth.OrganizationId:D}/customer-orders/{orderId:D}/accept",
            growth.AccessToken);
        var acceptResponse = await posClient.Client.SendAsync(accept);
        Assert.NotEqual(HttpStatusCode.Forbidden, acceptResponse.StatusCode);
    }

    private async Task<Guid> GetMainBranchIdAsync(SpineBusinessContext context)
    {
        using var branches = PlatformCommercialSpineSupport.Authed(
            HttpMethod.Get,
            $"/api/v1/platform/organizations/{context.OrganizationId:D}/branches",
            context.SessionToken);
        var response = await _platformClient.SendAsync(branches);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            PlatformCommercialSpineSupport.JsonOptions);
        return body.EnumerateArray().First(item => item.GetProperty("isPrimary").GetBoolean())
            .GetProperty("id")
            .GetGuid();
    }
}
