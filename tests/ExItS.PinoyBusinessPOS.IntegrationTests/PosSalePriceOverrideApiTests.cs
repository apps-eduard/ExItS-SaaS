using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// RMAP-B01 sale price override API: capability gates, manager ceiling, Owner unlimited, offline
/// fail-closed, audit rows, and catalog isolation (Today's Price unchanged).
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSalePriceOverrideApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Sales = "/api/v1/pos/sales";
    private const string Quote = "/api/v1/pos/sales/quote";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Customers = "/api/v1/pos/customers";
    private const string Reason = "Negotiated walk-in price";

    private const string ManagerGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreSalesOverridePrice}";

    private const string OwnerGrants =
        $"{ManagerGrants},{PosFeatureCodes.StoreSalesOverridePriceUnlimited}";

    private const string CashierGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}";

    private const string OrgAdminLikeGrants =
        $"{PosFeatureCodes.StoreCatalogView},{PosFeatureCodes.StoreCatalogManage}," +
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}";

    [Fact]
    public async Task Cashier_grants_deny_any_override()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sardinas", "Can", 100m, "ovr-cash-1");

        using var denied = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(150m, Reason, LineNumber: 1)]),
            CashierGrants);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(150)]
    [InlineData(200)]
    public async Task Manager_deviation_at_or_below_100_percent_passes(decimal requested)
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, $"ovr-mgr-{requested}");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                500m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(requested, Reason, LineNumber: 1)]),
            ManagerGrants);

        Assert.Equal(requested, Assert.Single(sale.Lines).UnitPrice);
        Assert.Equal(requested, sale.Subtotal);
        await AssertAuditRowAsync(factory, org, sale.SaleId, 100m, requested);
    }

    [Fact]
    public async Task Manager_200_01_is_denied()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Mantika", "Liter", 100m, "ovr-mgr-deny");

        using var denied = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                500m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(200.01m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(DomainErrorCodes.SalePriceOverrideExceedsManagerLimit, await ReadErrorCodeAsync(denied));
    }

    [Fact]
    public async Task Manager_zero_and_blank_reason_are_denied()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Gatas", "Can", 100m, "ovr-mgr-zero");

        using var zero = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(0m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(DomainErrorCodes.SalePriceOverrideInvalidAmount, await ReadErrorCodeAsync(zero));

        using var blank = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                150m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(150m, "   ", LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Equal(DomainErrorCodes.SalePriceOverrideReasonRequired, await ReadErrorCodeAsync(blank));
    }

    [Fact]
    public async Task Owner_250_passes_and_zero_denied()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coffee", "Piece", 100m, "ovr-own-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                500m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(250m, Reason, LineNumber: 1)]),
            OwnerGrants);
        Assert.Equal(250m, Assert.Single(sale.Lines).UnitPrice);

        using var zero = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(0m, Reason, LineNumber: 1)]),
            OwnerGrants);
        Assert.Equal(DomainErrorCodes.SalePriceOverrideInvalidAmount, await ReadErrorCodeAsync(zero));
    }

    [Fact]
    public async Task OrgAdmin_like_grants_without_override_feature_deny_over_100()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Soap", "Piece", 100m, "ovr-orgadmin-1");

        using var denied = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                500m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(250m, Reason, LineNumber: 1)]),
            OrgAdminLikeGrants);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task No_override_leaves_catalog_price_and_checkout_unchanged()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Eggs", "Piece", 180m, "ovr-none-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m),
            ManagerGrants);

        Assert.Equal(180m, Assert.Single(sale.Lines).UnitPrice);
        Assert.Equal(180m, sale.Subtotal);

        using var get = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var fresh = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(180m, fresh!.SellingPrice);
    }

    [Fact]
    public async Task Todays_price_baseline_is_isolated_from_override()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Onion", "Kilogram", 80m, "ovr-today-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(100m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(100m, Assert.Single(sale.Lines).UnitPrice);

        using var get = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var fresh = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(80m, fresh!.SellingPrice);
    }

    [Fact]
    public async Task Override_plus_b03_percent_and_payments_cash_gcash_utang()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Noodles", "Piece", 100m, "ovr-pay-1");
        var customer = await CreateCustomerAsync(client, org, "Ana Override");

        var managerPlusDiscount =
            $"{ManagerGrants},{PosFeatureCodes.StoreSalesApplyCommercialDiscount}," +
            $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate}";

        var cash = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(150m, Reason, LineNumber: 1)],
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, Reason)]),
            managerPlusDiscount);
        Assert.Equal(150m, cash.Lines[0].UnitPrice);
        Assert.Equal(135m, cash.Subtotal);

        var gcash = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.ManualGCashPaymentMethod,
                PriceOverrides: [new SalePriceOverrideIntentRequest(120m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(120m, gcash.Total);

        var utang = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer.CustomerId,
                PriceOverrides: [new SalePriceOverrideIntentRequest(110m, Reason, LineNumber: 1)]),
            managerPlusDiscount);
        Assert.Equal(110m, utang.Total);
        Assert.NotNull(utang.LinkedCreditEntryId);
    }

    [Fact]
    public async Task Stale_expected_baseline_conflicts()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sugar", "Kilogram", 100m, "ovr-stale-1");

        using var denied = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                PriceOverrides:
                [
                    new SalePriceOverrideIntentRequest(
                        150m,
                        Reason,
                        LineNumber: 1,
                        ExpectedBaselineUnitPrice: 70m)
                ]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(DomainErrorCodes.SalePriceOverrideStaleBaseline, await ReadErrorCodeAsync(denied));
    }

    [Fact]
    public async Task Offline_snapshot_with_override_is_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Tomato", "Kilogram", 100m, "ovr-off-1");

        using var denied = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [
                    new CheckoutSaleLineRequest(
                        product.ProductId,
                        1m,
                        UnitPriceSnapshot: 100m,
                        UnitOfMeasure: "Kilogram",
                        SellingMode: "PerItem",
                        LineTotal: 100m)
                ],
                PosSaleOptions.CashPaymentMethod,
                100m,
                SaleId: Guid.NewGuid(),
                PriceOverrides: [new SalePriceOverrideIntentRequest(120m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SalePriceOverrideOfflineNotSupported, await ReadErrorCodeAsync(denied));
    }

    [Fact]
    public async Task Quote_returns_baseline_and_applied_unit_price()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Salt", "Piece", 40m, "ovr-quote-1");

        using var request = Scoped(Quote, org, ManagerGrants);
        request.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                PriceOverrides: [new SalePriceOverrideIntentRequest(50m, Reason, LineNumber: 1)]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<PosSaleQuoteDto>(JsonOptions);
        Assert.NotNull(quote);
        Assert.Equal(100m, quote!.Subtotal);
        var line = Assert.Single(quote.Lines);
        Assert.Equal(50m, line.UnitPrice);
        Assert.Equal(40m, line.BaselineUnitPrice);
        var ovr = Assert.Single(quote.PriceOverrides!);
        Assert.Equal(40m, ovr.BaselineUnitPrice);
        Assert.Equal(50m, ovr.AppliedUnitPrice);
    }

    [Fact]
    public async Task Idempotent_replay_does_not_duplicate_override_audit()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Vinegar", "Bottle", 50m, "ovr-idemp-1");
        var saleId = Guid.NewGuid();

        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 1m)],
            PosSaleOptions.CashPaymentMethod,
            100m,
            SaleId: saleId,
            PriceOverrides: [new SalePriceOverrideIntentRequest(60m, Reason, LineNumber: 1)]);

        var first = await CheckoutAsync(client, org, body, ManagerGrants);
        var second = await CheckoutAsync(client, org, body, ManagerGrants);
        Assert.Equal(first.SaleId, second.SaleId);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var count = await db.SalePriceOverrideAdjustments
            .CountAsync(a => a.OrganizationId == org && a.SaleId == saleId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Cross_org_product_cannot_be_overridden()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var product = await CreateProductAsync(client, orgA, "Foreign", "Piece", 100m, "ovr-xorg-1");

        using var denied = await PostAsync(
            client,
            orgB,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                200m,
                PriceOverrides: [new SalePriceOverrideIntentRequest(120m, Reason, LineNumber: 1)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SaleProductNotFound, await ReadErrorCodeAsync(denied));
    }

    private static async Task AssertAuditRowAsync(
        PosApiFactory factory,
        Guid org,
        Guid saleId,
        decimal baseline,
        decimal applied)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var row = await db.SalePriceOverrideAdjustments
            .SingleAsync(a => a.OrganizationId == org && a.SaleId == saleId);
        Assert.Equal(baseline, row.BaselineUnitPrice);
        Assert.Equal(applied, row.AppliedUnitPrice);
        Assert.False(string.IsNullOrWhiteSpace(row.Reason));
        Assert.NotEqual(Guid.Empty, row.AppliedBy);
        Assert.NotEqual(Guid.Empty, row.SaleLineId);
    }

    private static async Task<PosSaleDto> CheckoutAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body,
        string? grants = null)
    {
        using var response = await PostAsync(client, org, body, grants);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        return sale!;
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body,
        string? grants = null)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = Scoped(Sales, org, grants);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string sku,
        string? sellingMode = null)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unitOfMeasure, sellingPrice, null, sku, SellingMode: sellingMode),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task<POSCustomerDto> CreateCustomerAsync(HttpClient client, Guid org, string displayName)
    {
        using var request = Scoped(HttpMethod.Post, Customers, org);
        request.Content = JsonContent.Create(
            new CreateCustomerRequest(displayName, null, null, null),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);
        return customer!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(string path, Guid organizationId, string? grants) =>
        Scoped(
            HttpMethod.Post,
            path,
            organizationId,
            grants is null ? null : PosSubscriptionStatuses.Active,
            grants);

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));

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

    private sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }
}
