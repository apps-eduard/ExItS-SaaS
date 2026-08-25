using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>POS-COM-INT-01: Platform subscription / entitlement enforcement readiness.</summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCommercialIntegrationReadinessTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Sales = "/api/v1/pos/sales";

    private static readonly string CashSaleGrants =
        $"{PosFeatureCodes.StoreCatalogView},{PosFeatureCodes.StoreCatalogManage}," +
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreShiftsView},{PosFeatureCodes.StoreShiftsManage}," +
        $"{PosFeatureCodes.StoreRegistersView},{PosFeatureCodes.StoreRegistersManage}";

    [Fact]
    public async Task Testing_default_without_headers_uses_development_commercial_default()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: false);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(HttpMethod.Get, Products, org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Strict_mode_without_commercial_headers_fails_closed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(HttpMethod.Get, Products, org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(ApplicationErrorCodes.CommercialAccessUnknown, problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Strict_mode_does_not_merge_missing_feature_grants()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var view = CreateScopedRequest(
            HttpMethod.Get,
            Products,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreCatalogView);
        using var viewResponse = await client.SendAsync(view);
        viewResponse.EnsureSuccessStatusCode();

        using var create = CreateScopedRequest(
            HttpMethod.Post,
            Products,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreCatalogView);
        create.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest("Bigas", "Kilogram", 50m, null, "strict-merge"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Strict_starter_like_grants_allow_cash_and_block_utang()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, CashSaleGrants);
        await EnsureOpenShiftWithGrantsAsync(client, org, Actor, CashSaleGrants);

        using var cashSale = CreateScopedRequest(
            HttpMethod.Post,
            Sales,
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            CashSaleGrants);
        cashSale.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                50m),
            options: JsonOptions);
        using var cashResponse = await client.SendAsync(cashSale);
        Assert.Equal(HttpStatusCode.Created, cashResponse.StatusCode);

        var customer = await CreateCustomerAsync(
            client,
            org,
            $"{CashSaleGrants},{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}",
            "Utang Rosa");
        using var utangSale = CreateScopedRequest(
            HttpMethod.Post,
            Sales,
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            CashSaleGrants);
        utangSale.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer.CustomerId),
            options: JsonOptions);
        using var utangResponse = await client.SendAsync(utangSale);
        Assert.Equal(HttpStatusCode.Forbidden, utangResponse.StatusCode);
    }

    [Fact]
    public async Task Suspended_subscription_blocks_financial_mutation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var createProduct = CreateScopedRequest(
            HttpMethod.Post,
            Products,
            org,
            status: PosSubscriptionStatuses.Suspended,
            grants: CashSaleGrants);
        createProduct.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest("Bigas", "Kilogram", 50m, null, "rice-susp"),
            options: JsonOptions);
        using var response = await client.SendAsync(createProduct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Expired_subscription_allows_continuity_read_and_blocks_new_credit()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var continuity = $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditRepay}";
        var setupGrants = $"{continuity},{PosFeatureCodes.CustomerCreditCreate}";

        var customer = await CreateCustomerAsync(client, org, setupGrants, "Expired Ana");

        using var list = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/customers?page=1&pageSize=20",
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();

        using var createCredit = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries",
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        createCredit.Content = JsonContent.Create(new CreateCreditEntryRequest(10m, "blocked"));
        using var creditResponse = await client.SendAsync(createCredit);
        Assert.Equal(HttpStatusCode.Forbidden, creditResponse.StatusCode);
    }

    [Fact]
    public async Task Cross_org_commercial_headers_do_not_authorize_other_org()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var grants = $"{PosFeatureCodes.StoreCatalogView},{PosFeatureCodes.StoreCatalogManage}";

        var product = await CreateProductAsync(client, orgA, grants);
        using var crossGet = CreateScopedRequest(
            HttpMethod.Get,
            $"{Products}/{product.ProductId:D}",
            orgB,
            status: PosSubscriptionStatuses.Active,
            grants: grants);
        using var crossResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);
    }

    [Fact]
    public async Task Bearer_introspection_binds_organization_and_denies_product_access()
    {
        var org = Guid.NewGuid();
        var user = Guid.NewGuid();
        await using var factory = new PosApiFactory(
            fixture.ConnectionString,
            strictCommercial: true,
            introspection: _ => new PlatformTokenIntrospectionResult(
                Active: true,
                UserId: user,
                OrganizationId: org,
                ProductCode: "pinoy-business-pos",
                ProductAccessAllowed: false,
                SubscriptionStatus: PosSubscriptionStatuses.Active,
                EnabledFeatureCodes: [PosFeatureCodes.StoreCatalogView]));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer test-token");

        using var request = CreateScopedRequest(HttpMethod.Get, Products, org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Strict_bearer_introspection_does_not_merge_development_grants()
    {
        var org = Guid.NewGuid();
        var user = Guid.NewGuid();
        await using var factory = new PosApiFactory(
            fixture.ConnectionString,
            strictCommercial: true,
            introspection: _ => new PlatformTokenIntrospectionResult(
                Active: true,
                UserId: user,
                OrganizationId: org,
                ProductCode: "pinoy-business-pos",
                ProductAccessAllowed: true,
                SubscriptionStatus: PosSubscriptionStatuses.Active,
                EnabledFeatureCodes: [PosFeatureCodes.StoreSalesView]));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer strict-token");

        using var request = CreateScopedRequest(HttpMethod.Get, Products, org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(ApplicationErrorCodes.CommercialCapabilityDenied, problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Reports_view_without_grant_is_denied_in_strict_mode()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreSalesView);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Strict_classic_sales_report_allowed_without_advanced_grant()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreReportsView);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Strict_operational_report_denied_without_store_advanced_reports()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            PosFeatureCodes.StoreReportsView);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Strict_operational_report_allowed_with_store_advanced_reports()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var grants = $"{PosFeatureCodes.StoreReportsView},{PosFeatureCodes.StoreAdvancedReports}";

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales-summary?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            grants);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Strict_customer_ordering_list_denied_without_grant()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{org:D}/customer-orders",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            PosFeatureCodes.StoreSalesView);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Strict_customer_ordering_list_allowed_with_grant()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{org:D}/customer-orders",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            PosFeatureCodes.StoreCustomerOrdering);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Strict_delivery_management_denied_without_store_delivery_orders()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{org:D}/customer-orders/{orderId:D}/accept",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            PosFeatureCodes.StoreCustomerOrdering);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Strict_delivery_management_allowed_with_store_delivery_orders()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var grants = $"{PosFeatureCodes.StoreCustomerOrdering},{PosFeatureCodes.StoreDeliveryOrders}";

        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/organizations/{org:D}/customer-orders/{orderId:D}/accept",
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            grants);
        using var response = await client.SendAsync(request);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Non_strict_testing_without_headers_still_allows_customer_ordering_list()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: false);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/organizations/{org:D}/customer-orders",
            org,
            Actor);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Strict_gcash_sale_allowed_without_customer_credit_grant()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, strictCommercial: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, CashSaleGrants);
        await EnsureOpenShiftWithGrantsAsync(client, org, Actor, CashSaleGrants);

        using var sale = CreateScopedRequest(
            HttpMethod.Post,
            Sales,
            org,
            Actor,
            PosSubscriptionStatuses.Active,
            CashSaleGrants);
        sale.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.ManualGCashPaymentMethod,
                GCashReference: "GCASH-TEST-1"),
            options: JsonOptions);
        using var response = await client.SendAsync(sale);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task EnsureOpenShiftWithGrantsAsync(
        HttpClient client,
        Guid organizationId,
        Guid actorId,
        string grants)
    {
        using var current = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/cashier-shifts/current",
            organizationId,
            actorId,
            PosSubscriptionStatuses.Active,
            grants);
        using var currentResponse = await client.SendAsync(current);
        if (currentResponse.IsSuccessStatusCode)
        {
            var existing = await currentResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
            if (existing is not null)
            {
                return;
            }
        }

        using var createRegister = CreateScopedRequest(
            HttpMethod.Post,
            "/api/v1/pos/registers",
            organizationId,
            actorId,
            PosSubscriptionStatuses.Active,
            grants);
        createRegister.Content = JsonContent.Create(
            new CreateRegisterRequest($"Register {Guid.NewGuid():N}"),
            options: JsonOptions);
        using var registerResponse = await client.SendAsync(createRegister);
        registerResponse.EnsureSuccessStatusCode();
        var register = await registerResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions);
        Assert.NotNull(register);

        using var open = CreateScopedRequest(
            HttpMethod.Post,
            "/api/v1/pos/cashier-shifts",
            organizationId,
            actorId,
            PosSubscriptionStatuses.Active,
            grants);
        open.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register!.RegisterId, 0m),
            options: JsonOptions);
        using var openResponse = await client.SendAsync(open);
        openResponse.EnsureSuccessStatusCode();
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string grants,
        string sku = "prod-1")
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            Products,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: grants);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest("Bigas", "Kilogram", 50m, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task<POSCustomerDto> CreateCustomerAsync(
        HttpClient client,
        Guid org,
        string grants,
        string name,
        string? status = null)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            "/api/v1/pos/customers",
            org,
            status: status ?? PosSubscriptionStatuses.Active,
            grants: grants);
        request.Content = JsonContent.Create(new CreateCustomerRequest(name, null, null, null));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);
        return customer!;
    }

    private static HttpRequestMessage CreateScopedRequest(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? actorId = null,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        if (actorId is not null)
        {
            request.Headers.TryAddWithoutValidation(
                PosOrganizationHeaders.ActorHeaderName,
                actorId.Value.ToString("D"));
        }

        if (status is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, status);
        }

        if (grants is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        }

        return request;
    }

    private sealed class PosApiFactory(
        string connectionString,
        bool strictCommercial = false,
        Func<string, PlatformTokenIntrospectionResult>? introspection = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["CommercialValidation:Strict"] = strictCommercial ? "true" : "false"
                });
            });

            if (introspection is not null)
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPlatformTokenIntrospectionClient>();
                    services.AddSingleton<IPlatformTokenIntrospectionClient>(
                        new StubPlatformTokenIntrospectionClient(introspection));
                });
            }
        }
    }

    private sealed class StubPlatformTokenIntrospectionClient(
        Func<string, PlatformTokenIntrospectionResult> handler) : IPlatformTokenIntrospectionClient
    {
        public Task<PlatformTokenIntrospectionResult> IntrospectAsync(
            string accessToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(handler(accessToken));
    }
}
