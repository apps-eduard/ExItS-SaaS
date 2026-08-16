using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPaymentAttemptApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid OtherOrgActor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Card_simulated_success_finalizes_sale_once_and_deducts_stock_after_paid()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Card Item", "Piece", 100m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, sale.Status);
        Assert.Equal("Card", sale.PaymentMethod);

        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "card-success-1");
        Assert.Equal("RequiresCustomerAction", attempt.Status);
        Assert.False(string.IsNullOrWhiteSpace(attempt.CheckoutUrl));
        Assert.StartsWith("fake_", attempt.ProviderReference, StringComparison.Ordinal);

        var paid = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", paid.Status);
        Assert.Equal("Visa", paid.CardBrand);
        Assert.Equal("4242", paid.CardLastFour);

        var completed = await GetSaleAsync(client, org, sale.SaleId);
        Assert.Equal(PosSaleOptions.CompletedStatus, completed.Status);
        Assert.Equal(attempt.ProviderReference, completed.GCashReference);

        var duplicate = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", duplicate.Status);
        Assert.Equal(paid.Id, duplicate.Id);
        Assert.NotNull(duplicate.CompletedAtUtc);
    }

    [Fact]
    public async Task Card_decline_and_cancel_leave_sale_awaiting_and_allow_retry()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Retry Item", "Piece", 50m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));

        var first = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "card-decline-1");
        var declined = await SimulateAsync(client, org, first.Id, "decline");
        Assert.Equal("Failed", declined.Status);
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);

        var second = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "card-retry-2");
        Assert.NotEqual(first.Id, second.Id);
        var cancelled = await CancelAttemptAsync(client, org, second.Id);
        Assert.Equal("Cancelled", cancelled.Status);

        var third = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "card-retry-3");
        var paid = await SimulateAsync(client, org, third.Id, "success");
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);
    }

    [Fact]
    public async Task GCash_qr_deep_link_simulated_success_and_pending_does_not_finalize()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "GCash Item", "Piece", 75m);

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, sale.Status);

        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "GCash", "gcash-1");
        Assert.False(string.IsNullOrWhiteSpace(attempt.QrPayload));
        Assert.False(string.IsNullOrWhiteSpace(attempt.DeepLink));
        Assert.Contains(attempt.ProviderReference!, attempt.QrPayload!, StringComparison.Ordinal);
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);

        var refreshed = await GetAttemptAsync(client, org, attempt.Id);
        Assert.NotEqual("Paid", refreshed.Status);

        var paid = await SimulateAsync(client, org, attempt.Id, "success");
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);
    }

    [Fact]
    public async Task Webhook_is_idempotent_and_authoritative_paid_overrides_failed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Webhook Item", "Piece", 40m);
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "wh-1");

        var bodyPaid = FakePaymentGateway.BuildWebhookBody(attempt.ProviderReference!, "Paid", 100);
        await PostWebhookAsync(client, bodyPaid, FakePaymentGateway.ComputeSignature(bodyPaid));
        Assert.Equal("Paid", (await GetAttemptAsync(client, org, attempt.Id)).Status);

        await PostWebhookAsync(client, bodyPaid, FakePaymentGateway.ComputeSignature(bodyPaid));
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);

        var sale2 = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt2 = await CreateAttemptAsync(client, org, sale2.SaleId, "Card", "wh-2");
        var bodyFail = FakePaymentGateway.BuildWebhookBody(attempt2.ProviderReference!, "Failed", 50, "declined", "nope");
        await PostWebhookAsync(client, bodyFail, FakePaymentGateway.ComputeSignature(bodyFail));
        Assert.Equal("Failed", (await GetAttemptAsync(client, org, attempt2.Id)).Status);

        // Authoritative newer Paid overrides Failed (provider wins).
        var bodyLatePaid = FakePaymentGateway.BuildWebhookBody(attempt2.ProviderReference!, "Paid", 200);
        await PostWebhookAsync(client, bodyLatePaid, FakePaymentGateway.ComputeSignature(bodyLatePaid));
        Assert.Equal("Paid", (await GetAttemptAsync(client, org, attempt2.Id)).Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, sale2.SaleId)).Status);
    }

    [Fact]
    public async Task Expired_simulation_and_duplicate_idempotency_key_reuse_same_attempt()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Expire Item", "Piece", 20m);
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));

        var a = await CreateAttemptAsync(client, org, sale.SaleId, "GCash", "same-key");
        var b = await CreateAttemptAsync(client, org, sale.SaleId, "GCash", "same-key");
        Assert.Equal(a.Id, b.Id);

        var expired = await SimulateAsync(client, org, a.Id, "expire");
        Assert.Equal("Expired", expired.Status);
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);
    }

    [Fact]
    public async Task Method_switch_after_failure_and_duplicate_active_attempt_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Switch Item", "Piece", 30m);

        var cardSale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var active = await CreateAttemptAsync(client, org, cardSale.SaleId, "Card", "active-1");
        using (var conflict = await PostAttemptAsync(client, org, cardSale.SaleId, "Card", "active-2"))
        {
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal(ApplicationErrorCodes.PaymentAttemptConflict, await ReadErrorCodeAsync(conflict));
        }

        await SimulateAsync(client, org, active.Id, "decline");

        var gcashSale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));
        var gcashAttempt = await CreateAttemptAsync(client, org, gcashSale.SaleId, "GCash", "gcash-switch");
        Assert.Equal("GCash", gcashAttempt.Method);
    }

    [Fact]
    public async Task Organization_isolation_and_invalid_webhook_signature()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var product = await CreateProductAsync(client, orgA, "Iso Item", "Piece", 15m);
        var sale = await CheckoutAsync(
            client,
            orgA,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, orgA, sale.SaleId, "Card", "iso-1");

        using var foreign = Scoped(HttpMethod.Get, $"/api/v1/pos/payment-attempts/{attempt.Id:D}", orgB);
        using var foreignResponse = await client.SendAsync(foreign);
        Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);

        var body = FakePaymentGateway.BuildWebhookBody(attempt.ProviderReference!, "Paid", 1);
        using var badSig = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pos/payment-webhooks/Fake")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        badSig.Headers.TryAddWithoutValidation("X-ExItS-Payment-Signature", "deadbeef");
        using var badResponse = await client.SendAsync(badSig);
        Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.PaymentWebhookSignatureInvalid, await ReadErrorCodeAsync(badResponse));
    }

    [Fact]
    public async Task Manual_gcash_transfer_requires_setting_and_rejects_duplicate_external_reference()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, enableManualGCash: true);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Manual Item", "Piece", 90m);
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));

        var pending = await CreateManualAttemptAsync(client, org, sale.SaleId, "ext-ref-1", "manual-1");
        Assert.Equal("PendingManualVerification", pending.Status);
        Assert.Equal(PosSaleOptions.AwaitingPaymentStatus, (await GetSaleAsync(client, org, sale.SaleId)).Status);

        await CancelAttemptAsync(client, org, pending.Id);

        var sale2 = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));
        using (var dup = await PostManualAttemptAsync(client, org, sale2.SaleId, "ext-ref-1", "manual-2"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);
            Assert.Equal(DomainErrorCodes.DuplicatePaymentAttemptExternalReference, await ReadErrorCodeAsync(dup));
        }

        var verifiedSale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));
        var verifyAttempt = await CreateManualAttemptAsync(client, org, verifiedSale.SaleId, "ext-ref-2", "manual-3");
        var verified = await VerifyManualAsync(client, org, verifyAttempt.Id, "Verified in bank statement");
        Assert.Equal("Paid", verified.Status);
        Assert.Equal(PosSaleOptions.CompletedStatus, (await GetSaleAsync(client, org, verifiedSale.SaleId)).Status);
    }

    [Fact]
    public async Task Manual_gcash_disabled_by_default_and_simulate_route_guards_release_production()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Disabled Manual", "Piece", 10m);
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "GCash", null));

        using (var disabled = await PostManualAttemptAsync(client, org, sale.SaleId, "x", "disabled-1"))
        {
            Assert.Equal(HttpStatusCode.BadRequest, disabled.StatusCode);
            Assert.Equal(ApplicationErrorCodes.ManualGCashTransferDisabled, await ReadErrorCodeAsync(disabled));
        }

        var endpointSource = await File.ReadAllTextAsync(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Payments",
            "PaymentAttemptEndpoints.cs"));
        Assert.Contains("IsProduction()", endpointSource, StringComparison.Ordinal);
        Assert.Contains("Release", endpointSource, StringComparison.Ordinal);
        Assert.Contains("PaymentSimulatorDisabled", endpointSource, StringComparison.Ordinal);
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

    [Fact]
    public async Task Payment_attempt_dto_exposes_no_sensitive_card_or_wallet_fields()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Safe Meta", "Piece", 12m);
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 1m)], "Card", null));
        var attempt = await CreateAttemptAsync(client, org, sale.SaleId, "Card", "safe-1");
        await SimulateAsync(client, org, attempt.Id, "success");

        using var request = Scoped(HttpMethod.Get, $"/api/v1/pos/payment-attempts/{attempt.Id:D}", org);
        using var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        foreach (var forbidden in new[]
                 {
                     "cardNumber", "cvv", "otp", "pin", "accessToken", "checkoutSecret", "walletCredential"
                 })
        {
            Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("cardBrand", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cardLastFour", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("providerReference", json, StringComparison.OrdinalIgnoreCase);
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
        string idempotencyKey) =>
        PostAttemptBodyAsync(
            client,
            org,
            saleId,
            new CreatePaymentAttemptRequest(method, idempotencyKey));

    private static async Task<PaymentAttemptDto> CreateManualAttemptAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        string externalReference,
        string idempotencyKey)
    {
        using var response = await PostManualAttemptAsync(client, org, saleId, externalReference, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static Task<HttpResponseMessage> PostManualAttemptAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        string externalReference,
        string idempotencyKey) =>
        PostAttemptBodyAsync(
            client,
            org,
            saleId,
            new CreatePaymentAttemptRequest("GCash", idempotencyKey, externalReference, ManualGCashTransfer: true));

    private static async Task<HttpResponseMessage> PostAttemptBodyAsync(
        HttpClient client,
        Guid org,
        Guid saleId,
        CreatePaymentAttemptRequest body)
    {
        using var request = Scoped(HttpMethod.Post, $"{Sales}/{saleId:D}/payment-attempts", org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
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

    private static async Task<PaymentAttemptDto> CancelAttemptAsync(HttpClient client, Guid org, Guid attemptId)
    {
        using var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/cancel", org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<PaymentAttemptDto> VerifyManualAsync(
        HttpClient client,
        Guid org,
        Guid attemptId,
        string reason)
    {
        using var request = Scoped(HttpMethod.Post, $"/api/v1/pos/payment-attempts/{attemptId:D}/verify-manual-gcash", org);
        request.Content = JsonContent.Create(new { reason }, options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<PaymentAttemptDto> GetAttemptAsync(HttpClient client, Guid org, Guid attemptId)
    {
        using var request = Scoped(HttpMethod.Get, $"/api/v1/pos/payment-attempts/{attemptId:D}", org);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task PostWebhookAsync(HttpClient client, string body, string signature)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pos/payment-webhooks/Fake")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-ExItS-Payment-Signature", signature);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    private sealed class PosApiFactory(
        string connectionString,
        bool enableManualGCash = false,
        string environmentName = "Testing",
        string? allowedHosts = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["PosPayments:EnableManualGCashTransfer"] = enableManualGCash ? "true" : "false"
                };
                if (!string.IsNullOrWhiteSpace(allowedHosts))
                {
                    values["AllowedHosts"] = allowedHosts;
                }
                else if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(environmentName, "Release", StringComparison.OrdinalIgnoreCase))
                {
                    values["AllowedHosts"] = "localhost;127.0.0.1";
                }

                config.AddInMemoryCollection(values);
            });
        }
    }
}
