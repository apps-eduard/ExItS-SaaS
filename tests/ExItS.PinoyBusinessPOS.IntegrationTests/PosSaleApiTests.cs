using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSaleApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Checkout_prices_from_the_catalog_and_returns_a_sequential_sale_number()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var rice = await CreateProductAsync(client, org, "Bigas", "Kilogram", 62m, sku: "rice-1");
        var coffee = await CreateProductAsync(client, org, "Kape", "Sachet", 8.50m, sku: "kape-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(rice.ProductId, 1.5m), new CheckoutSaleLineRequest(coffee.ProductId, 3m)],
                PosSaleOptions.CashPaymentMethod,
                200m));

        Assert.Equal(org, sale.OrganizationId);
        Assert.Equal(PosSaleOptions.CompletedStatus, sale.Status);
        Assert.Equal(Actor, sale.RecordedBy);
        Assert.StartsWith("SALE-", sale.SaleNumber, StringComparison.Ordinal);
        Assert.EndsWith("-000001", sale.SaleNumber, StringComparison.Ordinal);

        // 62.00 x 1.5 = 93.00 and 8.50 x 3 = 25.50.
        Assert.Equal(118.50m, sale.Subtotal);
        Assert.Equal(118.50m, sale.Total);
        Assert.Equal(200m, sale.AmountTendered);
        Assert.Equal(81.50m, sale.ChangeAmount);
        Assert.Null(sale.GCashReference);

        Assert.Equal(2, sale.Lines.Count);
        var riceLine = sale.Lines.Single(l => l.ProductId == rice.ProductId);
        Assert.Equal("Bigas", riceLine.Name);
        Assert.Equal("rice-1", riceLine.Sku);
        Assert.Equal("Kilogram", riceLine.UnitOfMeasure);
        Assert.Equal(62m, riceLine.UnitPrice);
        Assert.Equal(1.5m, riceLine.Quantity);
        Assert.Equal(93m, riceLine.LineTotal);

        var second = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(coffee.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                10m));
        Assert.EndsWith("-000002", second.SaleNumber, StringComparison.Ordinal);

        using var read = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", org);
        using var readResponse = await client.SendAsync(read);
        readResponse.EnsureSuccessStatusCode();
        var reread = await readResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(sale.SaleNumber, reread!.SaleNumber);
        Assert.Equal(2, reread.Lines.Count);
    }

    [Fact]
    public async Task Checkout_ignores_client_prices_and_uses_the_current_selling_price()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sardinas", "Can", 25m, sku: "sard-1");

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);

        // A client that invents its own price/name fields gets them dropped: the server reads neither.
        using var request = Scoped(HttpMethod.Post, Sales, org);
        request.Content = JsonContent.Create(
            new
            {
                lines = new[] { new { productId = product.ProductId, quantity = 2m, unitPrice = 0.01m, name = "Free" } },
                paymentMethod = PosSaleOptions.CashPaymentMethod,
                amountTendered = 100m,
                total = 0.02m,
                subtotal = 0.02m
            },
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(50m, sale!.Total);
        Assert.Equal("Sardinas", sale.Lines[0].Name);
        Assert.Equal(25m, sale.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task Repeated_products_are_combined_into_one_line()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Tinapay", "Piece", 5m, sku: "pan-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [
                    new CheckoutSaleLineRequest(product.ProductId, 2m),
                    new CheckoutSaleLineRequest(product.ProductId, 3m)
                ],
                PosSaleOptions.CashPaymentMethod,
                50m));

        var line = Assert.Single(sale.Lines);
        Assert.Equal(5m, line.Quantity);
        Assert.Equal(25m, sale.Total);
    }

    [Fact]
    public async Task Manual_gcash_checkout_records_reference_without_tender()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Load", "Piece", 100m, sku: "load-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.ManualGCashPaymentMethod,
                GCashReference: " GC-778899 "));

        Assert.Equal(PosSaleOptions.ManualGCashPaymentMethod, sale.PaymentMethod);
        Assert.Equal("GC-778899", sale.GCashReference);
        Assert.Null(sale.AmountTendered);
        Assert.Null(sale.ChangeAmount);
    }

    [Fact]
    public async Task Checkout_rejects_invalid_quantity_short_tender_and_inactive_or_foreign_products()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        var piece = await CreateProductAsync(client, org, "Softdrinks", "Bottle", 20m, sku: "soft-1");
        var inactive = await CreateProductAsync(client, org, "Luma", "Piece", 10m, sku: "old-1");
        var foreign = await CreateProductAsync(client, otherOrg, "Iba", "Piece", 10m, sku: "iba-1");

        using var deactivate = Scoped(HttpMethod.Post, $"{Products}/{inactive.ProductId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        var fractional = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(piece.ProductId, 1.5m)],
                PosSaleOptions.CashPaymentMethod,
                100m));
        Assert.Equal(HttpStatusCode.BadRequest, fractional.StatusCode);

        var shortTender = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(piece.ProductId, 2m)],
                PosSaleOptions.CashPaymentMethod,
                10m));
        Assert.Equal(HttpStatusCode.BadRequest, shortTender.StatusCode);

        var inactiveSale = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(inactive.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m));
        Assert.Equal(HttpStatusCode.Conflict, inactiveSale.StatusCode);
        Assert.Equal(ApplicationErrorCodes.SaleProductNotActive, await ReadErrorCodeAsync(inactiveSale));

        var foreignSale = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(foreign.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m));
        Assert.Equal(ApplicationErrorCodes.SaleProductNotFound, await ReadErrorCodeAsync(foreignSale));

        var noLines = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([], PosSaleOptions.CashPaymentMethod, 100m));
        Assert.Equal(HttpStatusCode.BadRequest, noLines.StatusCode);

        using var noActor = new HttpRequestMessage(HttpMethod.Post, Sales);
        noActor.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            org.ToString("D"));
        noActor.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(piece.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m),
            options: JsonOptions);
        using var noActorResponse = await client.SendAsync(noActor);
        Assert.Equal(HttpStatusCode.BadRequest, noActorResponse.StatusCode);
    }

    [Fact]
    public async Task Sales_are_isolated_per_organization_for_read_and_void()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var product = await CreateProductAsync(client, orgA, "Asukal", "Kilogram", 70m, sku: "sug-1");
        var sale = await CheckoutAsync(
            client,
            orgA,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                70m));

        using var crossGet = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", orgB);
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        using var crossVoid = Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", orgB);
        crossVoid.Content = JsonContent.Create(new VoidSaleRequest("Not mine"), options: JsonOptions);
        using var crossVoidResponse = await client.SendAsync(crossVoid);
        Assert.Equal(HttpStatusCode.NotFound, crossVoidResponse.StatusCode);

        using var crossList = Scoped(HttpMethod.Get, $"{Sales}?page=1&pageSize=20", orgB);
        using var crossListResponse = await client.SendAsync(crossList);
        crossListResponse.EnsureSuccessStatusCode();
        var page = await crossListResponse.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.DoesNotContain(page!.Items, s => s.SaleId == sale.SaleId);
    }

    [Fact]
    public async Task Void_marks_the_sale_voided_once_and_keeps_totals()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Mantika", "Liter", 90m, sku: "oil-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 0.5m)],
                PosSaleOptions.CashPaymentMethod,
                50m));
        Assert.Equal(45m, sale.Total);

        using var voidRequest = Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", org);
        voidRequest.Content = JsonContent.Create(new VoidSaleRequest("  Mali ang item  "), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        voidResponse.EnsureSuccessStatusCode();

        var voided = await voidResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(PosSaleOptions.VoidedStatus, voided!.Status);
        Assert.Equal("Mali ang item", voided.VoidReason);
        Assert.Equal(Actor, voided.VoidedBy);
        Assert.NotNull(voided.VoidedAtUtc);
        Assert.Equal(45m, voided.Total);
        Assert.Single(voided.Lines);

        using var secondVoid = Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", org);
        secondVoid.Content = JsonContent.Create(new VoidSaleRequest("Again"), options: JsonOptions);
        using var secondVoidResponse = await client.SendAsync(secondVoid);
        Assert.Equal(HttpStatusCode.Conflict, secondVoidResponse.StatusCode);

        using var missingReason = Scoped(HttpMethod.Post, $"{Sales}/{Guid.NewGuid():D}/void", org);
        missingReason.Content = JsonContent.Create(new VoidSaleRequest("Any"), options: JsonOptions);
        using var missingResponse = await client.SendAsync(missingReason);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_status_payment_method_date_and_sale_number()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Gatas", "Can", 40m, sku: "milk-1");

        var cash = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                40m));
        var gcash = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.ManualGCashPaymentMethod));

        using var voidRequest = Scoped(HttpMethod.Post, $"{Sales}/{gcash.SaleId:D}/void", org);
        voidRequest.Content = JsonContent.Create(new VoidSaleRequest("Test"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        voidResponse.EnsureSuccessStatusCode();

        var completed = await ListAsync(client, org, "status=Completed");
        Assert.Contains(completed.Items, s => s.SaleId == cash.SaleId);
        Assert.DoesNotContain(completed.Items, s => s.SaleId == gcash.SaleId);

        var voidedOnly = await ListAsync(client, org, "status=Voided");
        Assert.Contains(voidedOnly.Items, s => s.SaleId == gcash.SaleId);

        var byPayment = await ListAsync(client, org, $"paymentMethod={PosSaleOptions.ManualGCashPaymentMethod}");
        Assert.Contains(byPayment.Items, s => s.SaleId == gcash.SaleId);
        Assert.DoesNotContain(byPayment.Items, s => s.SaleId == cash.SaleId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var inRange = await ListAsync(client, org, $"fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}");
        Assert.Contains(inRange.Items, s => s.SaleId == cash.SaleId);

        var outOfRange = await ListAsync(client, org, $"toDate={today.AddDays(-1):yyyy-MM-dd}");
        Assert.Empty(outOfRange.Items);

        var byNumber = await ListAsync(client, org, $"saleNumber={cash.SaleNumber}");
        Assert.Equal(cash.SaleId, Assert.Single(byNumber.Items).SaleId);

        using var badStatus = Scoped(HttpMethod.Get, $"{Sales}?status=Utang", org);
        using var badStatusResponse = await client.SendAsync(badStatus);
        Assert.Equal(HttpStatusCode.BadRequest, badStatusResponse.StatusCode);

        using var badDate = Scoped(HttpMethod.Get, $"{Sales}?fromDate=not-a-date", org);
        using var badDateResponse = await client.SendAsync(badDate);
        Assert.Equal(HttpStatusCode.BadRequest, badDateResponse.StatusCode);
    }

    [Fact]
    public async Task Checkout_with_same_idempotency_headers_records_once_and_replays()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Noodles", "Pack", 12m, sku: "noodle-1");

        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 2m)],
            PosSaleOptions.CashPaymentMethod,
            50m,
            null,
            Guid.NewGuid());
        var key = "sale-checkout-once";
        var hash = ComputePayloadHash(body);
        var operationId = Guid.NewGuid();

        var first = await PostCheckoutWithIdempotencyAsync(client, org, body, key, hash, operationId);
        var second = await PostCheckoutWithIdempotencyAsync(client, org, body, key, hash, operationId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var created = await first.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        var replay = await second.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(created!.SaleId, replay!.SaleId);
        Assert.Equal(created.SaleNumber, replay.SaleNumber);

        var page = await ListAsync(client, org, "page=1&pageSize=50");
        Assert.Single(page.Items, s => s.SaleId == created.SaleId);

        var otherBody = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 5m)],
            PosSaleOptions.CashPaymentMethod,
            100m,
            null,
            Guid.NewGuid());
        var mismatch = await PostCheckoutWithIdempotencyAsync(
            client,
            org,
            otherBody,
            key,
            ComputePayloadHash(otherBody),
            Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        var conflict = await mismatch.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("conflict_payload_mismatch", conflict.GetProperty("outcomeCode").GetString());
    }

    [Fact]
    public async Task Concurrent_checkouts_receive_distinct_sale_numbers()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Tuyo", "Pack", 15m, sku: "tuyo-1");

        var request = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 1m)],
            PosSaleOptions.CashPaymentMethod,
            20m);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => PostCheckoutAsync(client, org, request)));

        var saleNumbers = new List<string>();
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
            saleNumbers.Add(sale!.SaleNumber);
            response.Dispose();
        }

        Assert.Equal(8, saleNumbers.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Sale_endpoints_enforce_commercial_capabilities()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Kendi", "Piece", 1m, sku: "candy-1");
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                1m));

        using var viewWithoutGrant = Scoped(
            HttpMethod.Get,
            Sales,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreCatalogView);
        using var viewWithoutGrantResponse = await client.SendAsync(viewWithoutGrant);
        Assert.Equal(HttpStatusCode.Forbidden, viewWithoutGrantResponse.StatusCode);

        using var viewWithGrant = Scoped(
            HttpMethod.Get,
            Sales,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreSalesView);
        using var viewWithGrantResponse = await client.SendAsync(viewWithGrant);
        viewWithGrantResponse.EnsureSuccessStatusCode();

        using var createInContinuity = Scoped(
            HttpMethod.Post,
            Sales,
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}");
        createInContinuity.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                1m),
            options: JsonOptions);
        using var createInContinuityResponse = await client.SendAsync(createInContinuity);
        Assert.Equal(HttpStatusCode.Forbidden, createInContinuityResponse.StatusCode);

        using var voidWithoutGrant = Scoped(
            HttpMethod.Post,
            $"{Sales}/{sale.SaleId:D}/void",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate}");
        voidWithoutGrant.Content = JsonContent.Create(new VoidSaleRequest("No grant"), options: JsonOptions);
        using var voidWithoutGrantResponse = await client.SendAsync(voidWithoutGrant);
        Assert.Equal(HttpStatusCode.Forbidden, voidWithoutGrantResponse.StatusCode);

        using var suspended = Scoped(
            HttpMethod.Get,
            Sales,
            org,
            status: PosSubscriptionStatuses.Suspended,
            grants: PosFeatureCodes.StoreSalesView);
        using var suspendedResponse = await client.SendAsync(suspended);
        Assert.Equal(HttpStatusCode.Forbidden, suspendedResponse.StatusCode);

        using var missingOrganization = new HttpRequestMessage(HttpMethod.Get, Sales);
        using var missingOrganizationResponse = await client.SendAsync(missingOrganization);
        Assert.Equal(HttpStatusCode.BadRequest, missingOrganizationResponse.StatusCode);
    }

    [Fact]
    public async Task Sale_endpoints_expose_no_stock_utang_refund_or_gateway_routes()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Sales", "SaleEndpoints.cs"));

        foreach (var forbidden in new[]
                 {
                     "/stock", "/inventory", "/refund", "/return", "/discount", "/tax",
                     "/split", "/gateway", "credit-entries", "StockLevel"
                 })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<PagedResult<PosSaleDto>> ListAsync(HttpClient client, Guid org, string query)
    {
        using var request = Scoped(HttpMethod.Get, $"{Sales}?{query}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.NotNull(page);
        return page!;
    }

    private static async Task<PosSaleDto> CheckoutAsync(HttpClient client, Guid org, CheckoutSaleRequest body)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
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
        string? sku = null)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unitOfMeasure, sellingPrice, null, sku),
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

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
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
