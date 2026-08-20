using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// RMAP-B03 commercial sale discount API surface: the discount capability gate, the non-persisting
/// quote endpoint, and the fail-closed offline snapshot rule.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSaleCommercialDiscountApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string Sales = "/api/v1/pos/sales";
    private const string Quote = "/api/v1/pos/sales/quote";
    private const string Products = "/api/v1/pos/catalog/products";

    private const string Reason = "Bulk buyer courtesy";

    /// <summary>What a store manager or owner carries: sales plus the discount grant.</summary>
    private const string ManagerGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}," +
        $"{PosFeatureCodes.StoreSalesApplyCommercialDiscount}";

    /// <summary>What a cashier carries: sales without the discount grant.</summary>
    private const string CashierGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}";

    [Fact]
    public async Task Checkout_without_discounts_still_reports_gross_equal_to_net()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sardinas", "Can", 25m, "disc-none-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                PosSaleOptions.CashPaymentMethod,
                200m));

        Assert.Equal(100m, sale.Subtotal);
        Assert.Equal(100m, sale.GrossSubtotal);
        Assert.Equal(0m, sale.DiscountTotal);
        Assert.Equal(0m, sale.LineDiscountTotal);
        Assert.Equal(0m, sale.SaleDiscountTotal);

        var line = Assert.Single(sale.Lines);
        Assert.Equal(100m, line.LineTotal);
        Assert.Equal(100m, line.GrossLineTotal);
        Assert.Equal(0m, line.LineDiscountAmount);
        Assert.Equal(0m, line.SaleDiscountAllocatedAmount);
    }

    [Fact]
    public async Task Manager_can_apply_a_sale_and_line_discount_and_the_read_back_reconciles()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var rice = await CreateProductAsync(client, org, "Bigas", "Kilogram", 60m, "disc-rice-1");
        var oil = await CreateProductAsync(client, org, "Mantika", "Liter", 100m, "disc-oil-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [
                    new CheckoutSaleLineRequest(rice.ProductId, 5m),
                    new CheckoutSaleLineRequest(oil.ProductId, 2m)
                ],
                PosSaleOptions.CashPaymentMethod,
                1_000m,
                Discounts:
                [
                    new CommercialDiscountIntentRequest("Line", "FixedAmount", 50m, Reason, LineNumber: 1),
                    new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, Reason)
                ]),
            grants: ManagerGrants);

        // 300 + 200 gross; line 1 loses 50, leaving a 450 sale-level base and a 45 sale discount.
        Assert.Equal(500m, sale.GrossSubtotal);
        Assert.Equal(50m, sale.LineDiscountTotal);
        Assert.Equal(45m, sale.SaleDiscountTotal);
        Assert.Equal(95m, sale.DiscountTotal);
        Assert.Equal(405m, sale.Subtotal);
        Assert.Equal(405m, sale.Total);
        Assert.Equal(595m, sale.ChangeAmount);
        Assert.Equal(sale.Subtotal, sale.Lines.Sum(l => l.LineTotal));

        var riceLine = sale.Lines.Single(l => l.ProductId == rice.ProductId);
        Assert.Equal(300m, riceLine.GrossLineTotal);
        Assert.Equal(50m, riceLine.LineDiscountAmount);
        Assert.Equal(25m, riceLine.SaleDiscountAllocatedAmount);
        Assert.Equal(225m, riceLine.LineTotal);
        // Quantity and unit price are never rewritten by a discount.
        Assert.Equal(5m, riceLine.Quantity);
        Assert.Equal(60m, riceLine.UnitPrice);

        using var read = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", org);
        using var readResponse = await client.SendAsync(read);
        readResponse.EnsureSuccessStatusCode();
        var reread = await readResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);

        Assert.Equal(500m, reread!.GrossSubtotal);
        Assert.Equal(95m, reread.DiscountTotal);
        Assert.Equal(405m, reread.Subtotal);
        Assert.Equal(225m, reread.Lines.Single(l => l.ProductId == rice.ProductId).LineTotal);
    }

    [Fact]
    public async Task Cashier_grants_are_rejected_only_when_the_cart_carries_a_discount()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Kape", "Sachet", 10m, "disc-cashier-1");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);

        using var denied = Scoped(Sales, org, CashierGrants);
        denied.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, Reason)]),
            options: JsonOptions);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        // The same cashier still checks out normally when no discount is requested.
        using var allowed = Scoped(Sales, org, CashierGrants);
        allowed.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                100m),
            options: JsonOptions);
        using var allowedResponse = await client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.Created, allowedResponse.StatusCode);
    }

    [Fact]
    public async Task Quote_previews_the_discount_without_recording_a_sale()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Asukal", "Kilogram", 70m, "disc-quote-1");

        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 3m)],
            PosSaleOptions.CashPaymentMethod,
            Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, Reason)]);

        using var request = Scoped(Quote, org, ManagerGrants);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var quote = await response.Content.ReadFromJsonAsync<PosSaleQuoteDto>(JsonOptions);
        Assert.Equal(210m, quote!.GrossSubtotal);
        Assert.Equal(21m, quote.SaleDiscountTotal);
        Assert.Equal(21m, quote.DiscountTotal);
        Assert.Equal(189m, quote.Subtotal);
        Assert.Equal(189m, quote.Total);

        var line = Assert.Single(quote.Lines);
        Assert.Equal(1, line.LineNumber);
        Assert.Equal(product.ProductId, line.ProductId);
        Assert.Equal(210m, line.GrossLineTotal);
        Assert.Equal(21m, line.SaleDiscountAllocatedAmount);
        Assert.Equal(189m, line.LineTotal);
        Assert.Equal(3m, line.Quantity);

        var discount = Assert.Single(quote.Discounts);
        Assert.Equal("Sale", discount.Scope);
        Assert.Equal("Percentage", discount.Method);
        Assert.Equal(10m, discount.RequestedValue);
        Assert.Equal(21m, discount.CalculatedAmount);
        Assert.Equal(Reason, discount.Reason);
        Assert.Null(discount.LineNumber);

        // Nothing was persisted: no sale, so no sale number was consumed either.
        using var list = Scoped(HttpMethod.Get, $"{Sales}?page=1&pageSize=20", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task Quote_requires_the_discount_grant_when_discounts_are_present()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Gatas", "Can", 40m, "disc-quote-2");

        using var denied = Scoped(Quote, org, CashierGrants);
        denied.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 5m, Reason)]),
            options: JsonOptions);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        using var allowed = Scoped(Quote, org, CashierGrants);
        allowed.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod),
            options: JsonOptions);
        using var allowedResponse = await client.SendAsync(allowed);
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        var quote = await allowedResponse.Content.ReadFromJsonAsync<PosSaleQuoteDto>(JsonOptions);
        Assert.Equal(0m, quote!.DiscountTotal);
        Assert.Equal(40m, quote.Subtotal);
    }

    [Fact]
    public async Task Offline_snapshot_checkout_with_a_discount_is_rejected_but_still_syncs_undiscounted()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var tomato = await CreateProductAsync(
            client, org, "Kamatis", "Kilogram", 150m, "disc-offline-1", sellingMode: "ByWeight");

        using var rejected = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [
                    new CheckoutSaleLineRequest(
                        tomato.ProductId,
                        1.200m,
                        UnitPriceSnapshot: 120m,
                        UnitOfMeasure: "Kilogram",
                        SellingMode: "ByWeight",
                        LineTotal: 144.00m)
                ],
                PosSaleOptions.CashPaymentMethod,
                144m,
                SaleId: Guid.NewGuid(),
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, Reason)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.SaleDiscountOfflineNotSupported,
            await ReadErrorCodeAsync(rejected));

        // Legacy offline sync without a discount is untouched by the new rule.
        using var accepted = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [
                    new CheckoutSaleLineRequest(
                        tomato.ProductId,
                        1.200m,
                        UnitPriceSnapshot: 120m,
                        UnitOfMeasure: "Kilogram",
                        SellingMode: "ByWeight",
                        LineTotal: 144.00m)
                ],
                PosSaleOptions.CashPaymentMethod,
                144m,
                SaleId: Guid.NewGuid()),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var sale = await accepted.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(144m, sale!.Total);
        Assert.Equal(144m, sale.GrossSubtotal);
        Assert.Equal(0m, sale.DiscountTotal);
    }

    [Fact]
    public async Task Invalid_discount_intents_are_rejected_with_domain_error_codes()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Tinapay", "Piece", 5m, "disc-invalid-1");

        var lines = new List<CheckoutSaleLineRequest> { new(product.ProductId, 2m) };

        using var blankReason = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                lines,
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 10m, "   ")]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, blankReason.StatusCode);
        Assert.Equal(DomainErrorCodes.SaleDiscountReasonRequired, await ReadErrorCodeAsync(blankReason));

        using var overEligible = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                lines,
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "FixedAmount", 10.01m, Reason)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, overEligible.StatusCode);
        Assert.Equal(DomainErrorCodes.SaleDiscountExceedsEligible, await ReadErrorCodeAsync(overEligible));

        using var badScope = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                lines,
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Promotion", "Percentage", 10m, Reason)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, badScope.StatusCode);
        Assert.Equal(DomainErrorCodes.SaleDiscountInvalidScope, await ReadErrorCodeAsync(badScope));

        using var unmatchedLine = await PostAsync(
            client,
            org,
            new CheckoutSaleRequest(
                lines,
                PosSaleOptions.CashPaymentMethod,
                100m,
                Discounts: [new CommercialDiscountIntentRequest("Line", "Percentage", 10m, Reason, LineNumber: 7)]),
            ManagerGrants);
        Assert.Equal(HttpStatusCode.BadRequest, unmatchedLine.StatusCode);
        Assert.Equal(DomainErrorCodes.SaleDiscountLineUnmatched, await ReadErrorCodeAsync(unmatchedLine));
    }

    [Fact]
    public async Task A_full_discount_records_a_zero_total_cash_sale()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Kendi", "Piece", 1m, "disc-zero-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 3m)],
                PosSaleOptions.CashPaymentMethod,
                0m,
                Discounts: [new CommercialDiscountIntentRequest("Sale", "Percentage", 100m, Reason)]),
            grants: ManagerGrants);

        Assert.Equal(3m, sale.GrossSubtotal);
        Assert.Equal(3m, sale.DiscountTotal);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal(0m, sale.Total);
        Assert.Equal(0m, sale.AmountTendered);
        Assert.Equal(0m, sale.ChangeAmount);
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
