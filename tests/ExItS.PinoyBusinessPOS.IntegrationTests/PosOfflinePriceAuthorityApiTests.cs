using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Offline;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// RMAP-21 Review Repair 01, end to end: a sale queued offline is recorded at the price the shop
/// showed the customer, not at whatever the catalog says when the device finally reconnects.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosOfflinePriceAuthorityApiTests(PosPostgreSqlFixture fixture)
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
    private const string Authorities = "/api/v1/pos/offline-price-authorities";

    [Fact]
    public async Task Authorities_are_issued_for_the_products_the_sell_floor_browsed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var rice = await CreateProductAsync(client, org, "Bigas", "Kilogram", 62m, sku: "auth-rice-1");
        var coffee = await CreateProductAsync(client, org, "Kape", "Sachet", 8.50m, sku: "auth-kape-1");

        var issued = await IssueAsync(client, org, [rice.ProductId, coffee.ProductId]);

        Assert.Equal(2, issued.Authorities.Count);
        Assert.True(issued.ExpiresAtUtc > issued.IssuedAtUtc);
        Assert.Equal(TimeSpan.FromHours(8), issued.ExpiresAtUtc - issued.IssuedAtUtc);

        var riceAuthority = issued.Authorities.Single(a => a.ProductId == rice.ProductId);
        Assert.Equal(org, riceAuthority.OrganizationId);
        Assert.Equal(62m, riceAuthority.UnitPrice);
        Assert.Equal("Kilogram", riceAuthority.UnitOfMeasure);
        Assert.Matches("^[0-9a-f]{64}$", riceAuthority.Signature);
        Assert.Equal(8.50m, issued.Authorities.Single(a => a.ProductId == coffee.ProductId).UnitPrice);
    }

    [Fact]
    public async Task Issuing_refuses_an_unknown_product_rather_than_leasing_a_guess()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = Scoped(HttpMethod.Post, Authorities, org);
        request.Content = JsonContent.Create(
            new IssueOfflinePriceAuthoritiesRequest([Guid.NewGuid()]),
            options: JsonOptions);
        using var response = await client.SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(ApplicationErrorCodes.SaleProductNotFound, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Queued_sale_keeps_the_leased_price_after_the_catalog_price_rises()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Softdrinks", "Bottle", 100m, sku: "auth-rise-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        // The owner raises the shelf price while the cashier is still offline.
        await UpdatePriceAsync(client, org, product, 120m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(authority, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                SaleId: Guid.NewGuid()));

        var line = Assert.Single(sale.Lines);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(100m, line.LineTotal);
        Assert.Equal(100m, sale.Total);
        Assert.Equal(100m, sale.AmountTendered);
        Assert.Equal(0m, sale.ChangeAmount);
    }

    [Fact]
    public async Task Queued_sale_keeps_the_leased_price_after_the_catalog_price_falls_and_invents_no_change()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Sabon", "Piece", 100m, sku: "auth-fall-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        await UpdatePriceAsync(client, org, product, 80m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(authority, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                SaleId: Guid.NewGuid()));

        // Live repricing to 80 would have manufactured 20 pesos of change the cashier never handed back.
        Assert.Equal(100m, sale.Total);
        Assert.Equal(0m, sale.ChangeAmount);
        Assert.Equal(100m, Assert.Single(sale.Lines).UnitPrice);
    }

    [Fact]
    public async Task Weighted_half_kilo_is_billed_at_the_leased_kilo_price()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var tomato = await CreateProductAsync(
            client,
            org,
            "Kamatis",
            "Kilogram",
            120m,
            sku: "auth-tom-1",
            sellingMode: "ByWeight");
        var authority = Assert.Single((await IssueAsync(client, org, [tomato.ProductId])).Authorities);

        await UpdatePriceAsync(client, org, tomato, 150m, sellingMode: "ByWeight");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(authority, 0.5m)],
                PosSaleOptions.CashPaymentMethod,
                60m,
                SaleId: Guid.NewGuid()));

        var line = Assert.Single(sale.Lines);
        Assert.Equal(120m, line.UnitPrice);
        Assert.Equal(0.5m, line.Quantity);
        Assert.Equal(60.00m, line.LineTotal);
        Assert.Equal(60.00m, sale.Total);
        Assert.Equal(0m, sale.ChangeAmount);
    }

    [Fact]
    public async Task Checkout_rejects_a_lease_whose_price_was_edited_on_the_device()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, sku: "auth-tamper-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        var forged = authority with { UnitPrice = 1m };

        using var response = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(forged, 1m)],
                PosSaleOptions.CashPaymentMethod,
                1m,
                SaleId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.OfflinePriceAuthorityTampered, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Checkout_rejects_a_lease_issued_to_another_organization()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var seller = Guid.NewGuid();
        var neighbour = Guid.NewGuid();

        var neighbourProduct = await CreateProductAsync(client, neighbour, "Bigas", "Kilogram", 10m, sku: "auth-org-1");
        var neighbourAuthority = Assert.Single((await IssueAsync(client, neighbour, [neighbourProduct.ProductId])).Authorities);

        // The seller stocks the same product, but may not sell it on the neighbour's lease.
        var sellerProduct = await CreateProductAsync(client, seller, "Bigas", "Kilogram", 100m, sku: "auth-org-2");
        var borrowed = neighbourAuthority with { ProductId = sellerProduct.ProductId };

        using var response = await PostCheckoutAsync(
            client,
            seller,
            new CheckoutSaleRequest(
                [LineFor(borrowed, 1m)],
                PosSaleOptions.CashPaymentMethod,
                10m,
                SaleId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Re-pointing the product also breaks the signature, so the server never has to decide
        // which of the two lies to report; either way it refuses to record money.
        Assert.Equal(ApplicationErrorCodes.OfflinePriceAuthorityTampered, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Checkout_rejects_an_expired_lease()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Asukal", "Kilogram", 100m, sku: "auth-exp-1");

        // Genuinely signed with the test deployment's key, but its window closed yesterday.
        var expired = SignLocally(
            org,
            product.ProductId,
            100m,
            "Kilogram",
            "PerItem",
            DateTimeOffset.UtcNow.AddHours(-30),
            DateTimeOffset.UtcNow.AddHours(-22));

        using var response = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(expired, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m,
                SaleId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.OfflinePriceAuthorityExpired, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Checkout_rejects_a_lease_sale_where_one_line_carries_no_lease()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var leased = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, sku: "auth-partial-1");
        var unleased = await CreateProductAsync(client, org, "Kape", "Sachet", 10m, sku: "auth-partial-2");
        var authority = Assert.Single((await IssueAsync(client, org, [leased.ProductId])).Authorities);

        using var response = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(authority, 1m), new CheckoutSaleLineRequest(unleased.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                110m,
                SaleId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.OfflinePriceAuthorityRequiredOnEveryLine,
            await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Checkout_rejects_a_lease_sale_without_the_client_sale_id_it_was_queued_under()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, sku: "auth-nosaleid-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        using var response = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [LineFor(authority, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
            await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Quote_refuses_leases_because_an_online_cart_prices_from_the_catalog()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, sku: "auth-quote-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        using var request = Scoped(HttpMethod.Post, Quote, org);
        request.Content = JsonContent.Create(
            new CheckoutSaleRequest([LineFor(authority, 1m)], PosSaleOptions.CashPaymentMethod, 100m),
            options: JsonOptions);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.OfflinePriceAuthorityOnlineNotSupported,
            await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Replaying_a_queued_lease_sale_records_it_once_and_a_different_payload_conflicts()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 100m, sku: "auth-idem-1");
        var authority = Assert.Single((await IssueAsync(client, org, [product.ProductId])).Authorities);

        var body = new CheckoutSaleRequest(
            [LineFor(authority, 2m)],
            PosSaleOptions.CashPaymentMethod,
            200m,
            SaleId: Guid.NewGuid());
        const string key = "offline-authority-sale-once";
        var operationId = Guid.NewGuid();

        using var first = await PostCheckoutWithIdempotencyAsync(
            client, org, body, key, ComputePayloadHash(body), operationId);
        using var replay = await PostCheckoutWithIdempotencyAsync(
            client, org, body, key, ComputePayloadHash(body), operationId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);

        var created = await first.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        var replayed = await replay.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(created!.SaleId, replayed!.SaleId);
        Assert.Equal(200m, created.Total);
        Assert.Equal(200m, replayed.Total);

        var otherBody = body with { Lines = [LineFor(authority, 3m)], AmountTendered = 300m };
        using var mismatch = await PostCheckoutWithIdempotencyAsync(
            client, org, otherBody, key, ComputePayloadHash(otherBody), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        var conflict = await mismatch.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("conflict_payload_mismatch", conflict.GetProperty("outcomeCode").GetString());
    }

    /// <summary>
    /// Mirrors the line the React client queues offline: the lease plus the amounts it printed on
    /// the customer's receipt, which the server checks against the lease rather than trusting.
    /// </summary>
    private static CheckoutSaleLineRequest LineFor(OfflinePriceAuthorityDto authority, decimal quantity) =>
        new(
            authority.ProductId,
            quantity,
            UnitPriceSnapshot: authority.UnitPrice,
            UnitOfMeasure: authority.UnitOfMeasure,
            SellingMode: authority.SellingMode,
            LineTotal: decimal.Round(authority.UnitPrice * quantity, 2, MidpointRounding.AwayFromZero),
            SellingUnitId: authority.SellingUnitId,
            OfflinePriceAuthority: new OfflinePriceAuthorityToken(
                authority.AuthorityId,
                authority.OrganizationId,
                authority.ProductId,
                authority.Signature,
                authority.IssuedAtUtc,
                authority.ExpiresAtUtc,
                authority.UnitPrice,
                authority.UnitOfMeasure,
                authority.SellingMode,
                authority.BranchId,
                authority.SellingUnitId));

    /// <summary>
    /// Signs a lease with the deployment key the Testing host uses, so a test can present a
    /// genuinely signed lease whose validity window it controls.
    /// </summary>
    private static OfflinePriceAuthorityDto SignLocally(
        Guid organizationId,
        Guid productId,
        decimal unitPrice,
        string unitOfMeasure,
        string sellingMode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var authorityId = Guid.NewGuid();
        var issued = DateTimeOffset.FromUnixTimeSeconds(issuedAtUtc.ToUnixTimeSeconds());
        var expires = DateTimeOffset.FromUnixTimeSeconds(expiresAtUtc.ToUnixTimeSeconds());
        var canonical = OfflinePriceAuthoritySigning.Canonicalize(
            authorityId,
            organizationId,
            null,
            productId,
            null,
            unitPrice,
            unitOfMeasure,
            sellingMode,
            issued,
            expires);

        return new OfflinePriceAuthorityDto(
            authorityId,
            organizationId,
            null,
            productId,
            null,
            unitPrice,
            unitOfMeasure,
            sellingMode,
            issued,
            expires,
            OfflinePriceAuthoritySigning.Sign(OfflinePriceAuthorityOptions.DevelopmentSigningKey, canonical));
    }

    private static async Task<IssueOfflinePriceAuthoritiesResponse> IssueAsync(
        HttpClient client,
        Guid org,
        List<Guid> productIds,
        List<Guid>? sellingUnitIds = null)
    {
        using var request = Scoped(HttpMethod.Post, Authorities, org);
        request.Content = JsonContent.Create(
            new IssueOfflinePriceAuthoritiesRequest(productIds, sellingUnitIds),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var issued = await response.Content.ReadFromJsonAsync<IssueOfflinePriceAuthoritiesResponse>(JsonOptions);
        Assert.NotNull(issued);
        return issued!;
    }

    private static async Task<PosSaleDto> CheckoutAsync(HttpClient client, Guid org, CheckoutSaleRequest body)
    {
        using var response = await PostCheckoutAsync(client, org, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        return sale!;
    }

    private static async Task<HttpResponseMessage> PostCheckoutAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = Scoped(HttpMethod.Post, Sales, org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostCheckoutWithIdempotencyAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body,
        string idempotencyKey,
        string payloadHash,
        Guid operationId)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = Scoped(HttpMethod.Post, Sales, org);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", payloadHash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", operationId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.SaleCheckout);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static string ComputePayloadHash(CheckoutSaleRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice,
        string? sku = null,
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

    private static async Task UpdatePriceAsync(
        HttpClient client,
        Guid org,
        PosCatalogProductDto product,
        decimal sellingPrice,
        string? sellingMode = null)
    {
        using var request = Scoped(HttpMethod.Put, $"{Products}/{product.ProductId:D}", org);
        request.Content = JsonContent.Create(
            new UpdatePosCatalogProductRequest(
                product.Name,
                product.UnitOfMeasure,
                sellingPrice,
                Sku: product.Sku,
                SellingMode: sellingMode),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
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
