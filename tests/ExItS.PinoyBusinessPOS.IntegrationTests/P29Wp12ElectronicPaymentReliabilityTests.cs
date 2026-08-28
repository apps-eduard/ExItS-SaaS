using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class P29Wp12ElectronicPaymentReliabilityTests(PosPostgreSqlFixture fixture)
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
    public async Task A_Electronic_checkout_reserves_stock()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Reserve Item", 5m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 2m)], "Card", null));
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, sale.Status);
        Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, sale.SaleId));

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(5m, onHand);
        Assert.Equal(2m, reserved);
    }

    [Fact]
    public async Task B_Last_stock_reserved_blocks_cash_checkout()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Last Unit", 1m);

        var cardSale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, cardSale.SaleId));

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var cash = Scoped(HttpMethod.Post, Sales, org);
        cash.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 100m),
            options: JsonOptions);
        using var cashResponse = await client.SendAsync(cash);
        Assert.Equal(HttpStatusCode.Conflict, cashResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, await ReadErrorCodeAsync(cashResponse));
    }

    [Fact]
    public async Task C_Paid_consumes_reservation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Consume Item", 3m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "consume-1");
        await SimulateAsync(client, org, attempt.Id, "success");

        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);
        Assert.Equal("Consumed", await GetSaleReservationStateAsync(org, sale.SaleId));
        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(2m, onHand);
        Assert.Equal(0m, reserved);
    }

    [Fact]
    public async Task D_Decline_releases_and_allows_cash()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Decline Item", 1m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "decline-1");
        await SimulateAsync(client, org, attempt.Id, "decline");

        Assert.Equal("Released", await GetSaleReservationStateAsync(org, sale.SaleId));
        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(1m, onHand);
        Assert.Equal(0m, reserved);

        var cash = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 50m));
        Assert.Equal(PosSaleOptions.CompletedStatus, cash.Status);
    }

    [Fact]
    public async Task E_Duplicate_paid_completes_once()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Dup Paid", 2m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "dup-paid");
        var first = await SimulateAsync(client, org, attempt.Id, "success");
        var second = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", first.Status);
        Assert.Equal("Paid", second.Status);
        Assert.Equal(first.Id, second.Id);
        Assert.NotNull(first.CompletedAtUtc);
        Assert.NotNull(second.CompletedAtUtc);

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(1m, onHand);
        Assert.Equal(0m, reserved);
    }

    [Fact]
    public async Task F_Timeout_after_create_recovers_session()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var gateway = (FakePaymentGateway)factory.Services.GetRequiredService<IPaymentGateway>();
        gateway.SetBehavior(FakePaymentGatewayBehavior.TimeoutAfterCreate);
        try
        {
            var client = factory.CreateClient();
            var org = Guid.NewGuid();
            var product = await CreateTrackedProductAsync(client, org, "Timeout Recover", 4m);
            var sale = await CheckoutAsync(
                client,
                org,
                new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));

            var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "timeout-after-1");
            Assert.Equal("RequiresCustomerAction", attempt.Status);
            Assert.StartsWith("fake_", attempt.ProviderReference, StringComparison.Ordinal);
            Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, sale.SaleId));
        }
        finally
        {
            gateway.ResetBehavior();
            gateway.ClearSessions();
        }
    }

    [Fact]
    public async Task G_Definite_failure_releases_reservation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var gateway = (FakePaymentGateway)factory.Services.GetRequiredService<IPaymentGateway>();
        gateway.SetBehavior(FakePaymentGatewayBehavior.DefiniteFailure);
        try
        {
            var client = factory.CreateClient();
            var org = Guid.NewGuid();
            var product = await CreateTrackedProductAsync(client, org, "Definite Fail", 2m);
            var sale = await CheckoutAsync(
                client,
                org,
                new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
            Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, sale.SaleId));

            using var response = await PostAttemptAsync(client, org, sale.SaleId, "Card", "definite-fail-1");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(DomainErrorCodes.PaymentGatewayFailure, await ReadErrorCodeAsync(response));

            Assert.Equal("Released", await GetSaleReservationStateAsync(org, sale.SaleId));
            var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
            Assert.Equal(2m, onHand);
            Assert.Equal(0m, reserved);
        }
        finally
        {
            gateway.ResetBehavior();
            gateway.ClearSessions();
        }
    }

    [Fact]
    public async Task H_Cash_checkout_unchanged_no_reservation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Cash Path", 5m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                "Cash",
                AmountTendered: 100m));
        Assert.Equal(PosSaleOptions.CompletedStatus, sale.Status);
        Assert.Equal("None", await GetSaleReservationStateAsync(org, sale.SaleId));

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(4m, onHand);
        Assert.Equal(0m, reserved);
    }

    [Fact]
    public async Task I_Paid_after_cancel_provider_wins_and_consumes()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Cancel Then Paid", 1m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "cancel-then-paid");
        await CancelAttemptAsync(client, org, attempt.Id);
        Assert.Equal("Released", await GetSaleReservationStateAsync(org, sale.SaleId));

        var paid = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);
        Assert.Equal("Consumed", await GetSaleReservationStateAsync(org, sale.SaleId));

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(0m, onHand);
        Assert.Equal(0m, reserved);
    }

    [Fact]
    public async Task J_Paid_after_expire_provider_wins()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateTrackedProductAsync(client, org, "Expire Then Paid", 1m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "expire-then-paid");
        await SimulateAsync(client, org, attempt.Id, "expire");
        Assert.Equal("Released", await GetSaleReservationStateAsync(org, sale.SaleId));

        var paid = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);

        var (onHand, reserved) = await GetInventoryQtysAsync(org, product.ProductId);
        Assert.Equal(0m, onHand);
        Assert.Equal(0m, reserved);
    }

    [Fact]
    public async Task K_Reconcile_recovers_created_attempt_after_restart_seam()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var gateway = (FakePaymentGateway)factory.Services.GetRequiredService<IPaymentGateway>();
        gateway.SetBehavior(FakePaymentGatewayBehavior.TimeoutBeforeCreate);
        Guid org;
        Guid saleId;
        Guid attemptId;
        try
        {
            var client = factory.CreateClient();
            org = Guid.NewGuid();
            var product = await CreateTrackedProductAsync(client, org, "Restart Reconcile", 3m);
            var sale = await CheckoutAsync(
                client,
                org,
                new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
            saleId = sale.SaleId;

            using var failed = await PostAttemptAsync(client, org, sale.SaleId, "Card", "restart-reconcile-1");
            Assert.Equal(HttpStatusCode.BadRequest, failed.StatusCode);
            Assert.Equal(DomainErrorCodes.PaymentGatewayTimeout, await ReadErrorCodeAsync(failed));

            // Durable Created attempt exists without provider session (simulated process loss).
            attemptId = await GetLatestAttemptIdAsync(org, saleId);
            Assert.Equal("Created", await GetAttemptStatusAsync(org, attemptId));
            Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, saleId));
        }
        finally
        {
            gateway.ResetBehavior();
        }

        gateway.ClearSessions();
        var client2 = factory.CreateClient();
        using var reconcile = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId}/reconcile", org);
        using var reconcileResponse = await client2.SendAsync(reconcile);
        Assert.Equal(HttpStatusCode.OK, reconcileResponse.StatusCode);
        var recovered = await reconcileResponse.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(recovered);
        Assert.Equal("RequiresCustomerAction", recovered!.Status);
        Assert.False(string.IsNullOrWhiteSpace(recovered.ProviderReference));
        Assert.Equal("Reserved", await GetSaleReservationStateAsync(org, saleId));
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
        var value = await cmd.ExecuteScalarAsync();
        Assert.NotNull(value);
        return (string)value!;
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
        using var response = await PostAttemptAsync(client, org, saleId, method, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static Task<HttpResponseMessage> PostAttemptAsync(
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
        using var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/simulate", org);
        request.Content = JsonContent.Create(new SimulatePaymentRequest(outcome), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<PosSaleDto> GetSaleAsync(HttpClient client, Guid org, Guid saleId)
    {
        using var request = Scoped(HttpMethod.Get, $"{Sales}/{saleId:D}", org);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);
        return sale!;
    }

    private static async Task<PosSaleDto> CheckoutAsync(HttpClient client, Guid org, CheckoutSaleRequest body)
    {
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor).ConfigureAwait(false);
        using var request = Scoped(HttpMethod.Post, Sales, org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
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

    private static async Task<PaymentAttemptDto> CancelAttemptAsync(HttpClient client, Guid org, Guid attemptId)
    {
        using var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/cancel", org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private async Task<Guid> GetLatestAttemptIdAsync(Guid orgId, Guid saleId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT id
            FROM pos.payment_attempts
            WHERE organization_id = @org AND sale_id = @sale
            ORDER BY created_at_utc DESC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("org", orgId);
        cmd.Parameters.AddWithValue("sale", saleId);
        var value = await cmd.ExecuteScalarAsync();
        Assert.NotNull(value);
        return (Guid)value!;
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
        var value = await cmd.ExecuteScalarAsync();
        Assert.NotNull(value);
        return (string)value!;
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
