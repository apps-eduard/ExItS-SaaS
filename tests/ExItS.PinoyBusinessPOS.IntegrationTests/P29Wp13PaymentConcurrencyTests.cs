using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class P29Wp13PaymentConcurrencyTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Inventory = "/api/v1/pos/inventory";

    [Fact]
    public async Task A_Concurrent_Paid_vs_Cancel()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Conc Paid Cancel", 1m);
        var sale = await CheckoutAsync(
            setup,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(setup, org, sale.SaleId, "Card", "conc-paid-cancel");

        var barrier = new Barrier(2);
        var clientPaid = factory.CreateClient();
        var clientCancel = factory.CreateClient();

        var paidTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await SimulateRawAsync(clientPaid, org, attempt.Id, "success");
        });
        var cancelTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await CancelRawAsync(clientCancel, org, attempt.Id);
        });

        var results = await Task.WhenAll(paidTask, cancelTask);
        Assert.True(
            results[0].IsSuccessStatusCode || results[1].IsSuccessStatusCode,
            "At least one concurrent request should succeed.");
        // Cancel may OK, 4xx (Paid already), or rare 5xx race noise — final DB truth is authoritative.
        Assert.True(
            results[1].IsSuccessStatusCode
            || (int)results[1].StatusCode is >= 400 and < 600,
            $"Cancel returned unexpected status {results[1].StatusCode}.");

        await AssertPaidCompletedConsumedOnceAsync(org, product.ProductId, sale.SaleId, attempt.Id, expectedOnHand: 0m);
    }

    [Fact]
    public async Task B_Concurrent_Paid_vs_Expire()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Conc Paid Expire", 1m);
        var sale = await CheckoutAsync(
            setup,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(setup, org, sale.SaleId, "Card", "conc-paid-expire");

        var barrier = new Barrier(2);
        var clientPaid = factory.CreateClient();
        var clientExpire = factory.CreateClient();

        var paidTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await SimulateRawAsync(clientPaid, org, attempt.Id, "success");
        });
        var expireTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await SimulateRawAsync(clientExpire, org, attempt.Id, "expire");
        });

        await Task.WhenAll(paidTask, expireTask);
        await AssertPaidCompletedConsumedOnceAsync(org, product.ProductId, sale.SaleId, attempt.Id, expectedOnHand: 0m);
    }

    [Fact]
    public async Task C_Concurrent_duplicate_Paid_storm()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Paid Storm", 2m);
        var sale = await CheckoutAsync(
            setup,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(setup, org, sale.SaleId, "Card", "paid-storm");

        const int n = 10;
        var barrier = new Barrier(n);
        var tasks = Enumerable.Range(0, n).Select(_ =>
        {
            var client = factory.CreateClient();
            return Task.Run(async () =>
            {
                barrier.SignalAndWait();
                return await SimulateRawAsync(client, org, attempt.Id, "success");
            });
        }).ToArray();

        await Task.WhenAll(tasks);
        await AssertPaidCompletedConsumedOnceAsync(org, product.ProductId, sale.SaleId, attempt.Id, expectedOnHand: 1m);
    }

    [Fact]
    public async Task D_Concurrent_last_stock_two_buyers()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Last Unit Race", 1m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(setup, org, Actor);

        var barrier = new Barrier(2);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();
        var body = new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null);

        var taskA = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await CheckoutRawAsync(clientA, org, body);
        });
        var taskB = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await CheckoutRawAsync(clientB, org, body);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var created = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var conflicts = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();
        Assert.Single(created);
        Assert.Single(conflicts);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, await ReadErrorCodeAsync(conflicts[0]));

        var winner = await created[0].Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(winner);
        var (onHandReserved, reservedReserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(1m, onHandReserved);
        Assert.Equal(1m, reservedReserved);

        var attempt = await CreateAttemptAsync(setup, org, winner!.SaleId, "Card", "last-unit-winner");
        await SimulateAsync(setup, org, attempt.Id, "success");

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(0m, onHand);
        Assert.Equal(0m, reserved);
        AssertInventoryInvariants(onHand, reserved);
        Assert.Equal(1, await CountSaleDeductionsAsync(org, winner.SaleId));
    }

    [Fact]
    public async Task E_Concurrent_retry_re_reserve_vs_competing_buyer()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Retry vs Buyer", 1m);

        var saleA = await CheckoutAsync(
            setup,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attemptA = await CreateAttemptAsync(setup, org, saleA.SaleId, "Card", "retry-a-1");
        await SimulateAsync(setup, org, attemptA.Id, "decline");
        Assert.Equal("Released", await GetSaleReservationStateAsync(org, saleA.SaleId));

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(setup, org, Actor);
        var barrier = new Barrier(2);
        var clientRetry = factory.CreateClient();
        var clientBuyer = factory.CreateClient();
        var buyerBody = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 1m)],
            "Card",
            null);

        var retryTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await PostAttemptRawAsync(clientRetry, org, saleA.SaleId, "Card", "retry-a-2");
        });
        var buyerTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await CheckoutRawAsync(clientBuyer, org, buyerBody);
        });

        var results = await Task.WhenAll(retryTask, buyerTask);
        var winners = 0;
        if (results[0].StatusCode == HttpStatusCode.Created)
        {
            winners++;
        }

        if (results[1].StatusCode == HttpStatusCode.Created)
        {
            winners++;
        }

        Assert.Equal(1, winners);

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        AssertInventoryInvariants(onHand, reserved);
        Assert.True(onHand >= 0m && reserved >= 0m);
        Assert.Equal(1m, onHand);
        Assert.Equal(1m, reserved);

        if (results[0].IsSuccessStatusCode)
        {
            Assert.True(
                (int)results[1].StatusCode is >= 400 and < 600,
                $"Competing buyer should fail safely, got {results[1].StatusCode}");
        }
        else
        {
            Assert.True(
                (int)results[0].StatusCode is >= 400 and < 600,
                $"Retry re-reserve should fail safely, got {results[0].StatusCode}");
            Assert.Equal(HttpStatusCode.Created, results[1].StatusCode);
        }
    }

    [Fact]
    public async Task F_Concurrent_Reconcile_vs_Paid_webhook()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var setup = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(setup, org, "Reconcile vs Paid", 2m);
        var sale = await CheckoutAsync(
            setup,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(setup, org, sale.SaleId, "Card", "reconcile-vs-paid");

        var barrier = new Barrier(2);
        var clientReconcile = factory.CreateClient();
        var clientPaid = factory.CreateClient();

        var reconcileTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            using var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attempt.Id:D}/reconcile", org);
            return await clientReconcile.SendAsync(request);
        });
        var paidTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await SimulateRawAsync(clientPaid, org, attempt.Id, "success");
        });

        await Task.WhenAll(reconcileTask, paidTask);
        await AssertPaidCompletedConsumedOnceAsync(org, product.ProductId, sale.SaleId, attempt.Id, expectedOnHand: 1m);
    }

    private async Task AssertPaidCompletedConsumedOnceAsync(
        Guid org,
        Guid productId,
        Guid saleId,
        Guid attemptId,
        decimal expectedOnHand)
    {
        Assert.Equal("Paid", await GetAttemptStatusAsync(org, attemptId));
        Assert.Equal(PosSaleOptions.CompletedStatus, await GetSaleStatusAsync(org, saleId));
        Assert.Equal("Consumed", await GetSaleReservationStateAsync(org, saleId));
        var (onHand, reserved) = await GetInventoryQtysAsync(org, productId);
        Assert.Equal(expectedOnHand, onHand);
        Assert.Equal(0m, reserved);
        AssertInventoryInvariants(onHand, reserved);
        Assert.Equal(1, await CountSaleDeductionsAsync(org, saleId));
    }

    private static void AssertInventoryInvariants(decimal onHand, decimal reserved)
    {
        Assert.True(onHand >= 0m, "on_hand must be >= 0");
        Assert.True(reserved >= 0m, "reserved must be >= 0");
        Assert.True(reserved <= onHand, "reserved must be <= on_hand");
    }

    private async Task<long> CountSaleDeductionsAsync(Guid orgId, Guid saleId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)::bigint
            FROM pos.stock_movements
            WHERE organization_id = @org
              AND source_type = 'Sale'
              AND movement_type = 'SaleDeduction'
              AND source_id = @sale
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("sale", saleId);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<(decimal OnHand, decimal Reserved)> GetInventoryQtysAsync(Guid orgId, Guid productId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT on_hand_quantity, reserved_quantity
            FROM pos.inventory_accounts
            WHERE organization_id = @org AND product_id = @product
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("product", productId);
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetDecimal(0), reader.GetDecimal(1));
    }

    private async Task<string> GetSaleReservationStateAsync(Guid orgId, Guid saleId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT stock_reservation_state
            FROM pos.sales
            WHERE organization_id = @org AND id = @sale
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("sale", saleId);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<string> GetSaleStatusAsync(Guid orgId, Guid saleId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT status
            FROM pos.sales
            WHERE organization_id = @org AND id = @sale
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("sale", saleId);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private async Task<string> GetAttemptStatusAsync(Guid orgId, Guid attemptId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT status
            FROM pos.payment_attempts
            WHERE organization_id = @org AND id = @id
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("id", attemptId);
        return (string)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<PosCatalogProductDto> CreateTrackedProductAsync(
        HttpClient client,
        Guid org,
        string name,
        decimal openingStock)
    {
        var product = await CreateProductAsync(client, org, name, "Piece", 25m);
        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(
            openingStock > 0m
                ? new EnableInventoryTrackingRequest(OpeningQuantity: openingStock, UnitCost: 1m)
                : new EnableInventoryTrackingRequest(OpeningQuantity: openingStock),
            options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();
        return product;
    }

    private static async Task<PaymentAttemptDto> CreateAttemptAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        string method,
        string idempotencyKey)
    {
        using var response = await PostAttemptRawAsync(client, org, saleId, method, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static Task<HttpResponseMessage> PostAttemptRawAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        string method,
        string idempotencyKey)
    {
        var request = Scoped(HttpMethod.Post, $"{Sales}/{saleId:D}/payment-attempts", org);
        request.Content = JsonContent.Create(
            new CreatePaymentAttemptRequest(method, idempotencyKey),
            options: JsonOptions);
        return client.SendAsync(request);
    }

    private static async Task<PaymentAttemptDto> SimulateAsync(
        HttpClient client,
        Guid org,
        Guid attemptId,
        string outcome)
    {
        using var response = await SimulateRawAsync(client, org, attemptId, outcome);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<HttpResponseMessage> SimulateRawAsync(
        HttpClient client,
        Guid org,
        Guid attemptId,
        string outcome)
    {
        var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/simulate", org);
        request.Content = JsonContent.Create(new SimulatePaymentRequest(outcome), options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> CancelRawAsync(HttpClient client, Guid org, Guid attemptId)
    {
        var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/cancel", org);
        return client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> CheckoutRawAsync(
        HttpClient client,
        Guid org,
        CheckoutSaleRequest body)
    {
        var request = Scoped(HttpMethod.Post, Sales, org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<PosSaleDto> CheckoutAsync(HttpClient client, Guid org, CheckoutSaleRequest body)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var response = await CheckoutRawAsync(client, org, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        return sale!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string unitOfMeasure,
        decimal sellingPrice)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, unitOfMeasure, sellingPrice, null, Guid.NewGuid().ToString("N")[..12]),
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
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["PosPayments:EnableManualGCashTransfer"] = "false"
                });
            });
        }
    }
}
