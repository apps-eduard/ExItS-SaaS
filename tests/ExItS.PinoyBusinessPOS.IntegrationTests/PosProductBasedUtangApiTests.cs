using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosProductBasedUtangApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Customers = "/api/v1/pos/customers";

    [Fact]
    public async Task Utang_checkout_creates_sale_and_credit_atomically_with_optional_due_date()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 62m, sku: "utang-rice-1");
        var customer = await CreateCustomerAsync(client, org, "Aling Utang");
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7));
        var saleId = Guid.NewGuid();
        var creditEntryId = Guid.NewGuid();

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1.5m)],
                PosSaleOptions.UtangPaymentMethod,
                SaleId: saleId,
                CustomerId: customer.CustomerId,
                DueDate: dueDate,
                CreditEntryId: creditEntryId));

        Assert.Equal(PosSaleOptions.UtangPaymentMethod, sale.PaymentMethod);
        Assert.Equal(saleId, sale.SaleId);
        Assert.Equal(customer.CustomerId, sale.CustomerId);
        Assert.Equal(creditEntryId, sale.LinkedCreditEntryId);
        Assert.Equal(93m, sale.Total);
        Assert.Null(sale.AmountTendered);
        Assert.Null(sale.ChangeAmount);

        using var saleGet = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", org);
        using var saleGetResponse = await client.SendAsync(saleGet);
        saleGetResponse.EnsureSuccessStatusCode();
        var enriched = await saleGetResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(dueDate, enriched!.LinkedCreditDueDate);
        Assert.Equal(customer.DisplayName, enriched.CustomerDisplayName);
        Assert.Equal(93m, enriched.CustomerOutstandingAfter);

        using var creditGet = Scoped(
            HttpMethod.Get,
            $"{Customers}/{customer.CustomerId:D}/credit-entries/{creditEntryId:D}",
            org);
        using var creditResponse = await client.SendAsync(creditGet);
        creditResponse.EnsureSuccessStatusCode();
        var credit = await creditResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.NotNull(credit);
        Assert.Equal(93m, credit!.Amount);
        Assert.Equal(ProductBasedUtangRemarks.ForSaleNumber(sale.SaleNumber), credit.Remarks);
        Assert.Equal(sale.SaleId, credit.SourceSaleId);
        Assert.Equal(dueDate, credit.CurrentDueDate);
        Assert.Equal("Active", credit.Status);
    }

    [Fact]
    public async Task Zero_total_utang_is_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Freebie", "Piece", 0m, sku: "utang-zero-1");
        var customer = await CreateCustomerAsync(client, org, "Zero Utang");

        using var response = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer.CustomerId,
                CreditEntryId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DomainErrorCodes.SaleUtangTotalMustBePositive, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Inactive_and_cross_org_customers_are_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Sardinas", "Can", 25m, sku: "utang-sard-1");
        var inactive = await CreateCustomerAsync(client, org, "Inactive Utang");
        var foreign = await CreateCustomerAsync(client, otherOrg, "Foreign Utang");

        using var deactivate = Scoped(HttpMethod.Post, $"{Customers}/{inactive.CustomerId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        using var inactiveSale = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: inactive.CustomerId,
                CreditEntryId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Conflict, inactiveSale.StatusCode);
        Assert.Equal(DomainErrorCodes.CustomerNotActive, await ReadErrorCodeAsync(inactiveSale));

        using var foreignSale = await PostCheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: foreign.CustomerId,
                CreditEntryId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, foreignSale.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CustomerNotFound, await ReadErrorCodeAsync(foreignSale));
    }

    [Fact]
    public async Task Utang_checkout_replays_idempotently_for_same_sale_and_credit_ids()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Kape", "Sachet", 8.50m, sku: "utang-kape-1");
        var customer = await CreateCustomerAsync(client, org, "Idempotent Utang");
        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 2m)],
            PosSaleOptions.UtangPaymentMethod,
            SaleId: Guid.NewGuid(),
            CustomerId: customer.CustomerId,
            CreditEntryId: Guid.NewGuid());

        var key = "utang-checkout-once";
        var hash = ComputePayloadHash(body);
        var operationId = Guid.NewGuid();

        var first = await PostCheckoutWithIdempotencyAsync(client, org, body, key, hash, operationId);
        var second = await PostCheckoutWithIdempotencyAsync(client, org, body, key, hash, operationId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var created = await first.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        var replay = await second.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(created!.SaleId, replay!.SaleId);
        Assert.Equal(created.LinkedCreditEntryId, replay.LinkedCreditEntryId);

        using var list = Scoped(HttpMethod.Get, $"{Sales}?page=1&pageSize=50", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.Single(page!.Items, s => s.SaleId == created.SaleId);
    }

    [Fact]
    public async Task Utang_checkout_payload_mismatch_conflicts()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Tinapay", "Piece", 5m, sku: "utang-pan-1");
        var customer = await CreateCustomerAsync(client, org, "Mismatch Utang");
        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 1m)],
            PosSaleOptions.UtangPaymentMethod,
            SaleId: Guid.NewGuid(),
            CustomerId: customer.CustomerId,
            CreditEntryId: Guid.NewGuid());
        var key = "utang-checkout-mismatch";
        var hash = ComputePayloadHash(body);
        var operationId = Guid.NewGuid();

        var first = await PostCheckoutWithIdempotencyAsync(client, org, body, key, hash, operationId);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var otherBody = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 3m)],
            PosSaleOptions.UtangPaymentMethod,
            SaleId: Guid.NewGuid(),
            CustomerId: customer.CustomerId,
            CreditEntryId: Guid.NewGuid());
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
    public async Task Void_reverses_linked_credit_atomically_and_blocks_standalone_reverse()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Mantika", "Liter", 90m, sku: "utang-oil-1");
        var customer = await CreateCustomerAsync(client, org, "Void Utang");
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.UtangPaymentMethod,
                SaleId: Guid.NewGuid(),
                CustomerId: customer.CustomerId,
                CreditEntryId: Guid.NewGuid()));

        using var reverse = Scoped(
            HttpMethod.Post,
            $"{Customers}/{customer.CustomerId:D}/credit-entries/{sale.LinkedCreditEntryId:D}/reverse",
            org);
        reverse.Content = JsonContent.Create(new ReverseCreditEntryRequest("Should fail"), options: JsonOptions);
        using var reverseResponse = await client.SendAsync(reverse);
        Assert.Equal(HttpStatusCode.Conflict, reverseResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.CreditReversalRequiresSaleVoid,
            await ReadErrorCodeAsync(reverseResponse));

        using var voidRequest = Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", org);
        voidRequest.Content = JsonContent.Create(new VoidSaleRequest("Wrong cart"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        voidResponse.EnsureSuccessStatusCode();
        var voided = await voidResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(PosSaleOptions.VoidedStatus, voided!.Status);

        using var creditGet = Scoped(
            HttpMethod.Get,
            $"{Customers}/{customer.CustomerId:D}/credit-entries/{sale.LinkedCreditEntryId:D}",
            org);
        using var creditResponse = await client.SendAsync(creditGet);
        creditResponse.EnsureSuccessStatusCode();
        var credit = await creditResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.Equal("Reversed", credit!.Status);

        using var summary = Scoped(HttpMethod.Get, $"{Customers}/{customer.CustomerId:D}/credit-summary", org);
        using var summaryResponse = await client.SendAsync(summary);
        summaryResponse.EnsureSuccessStatusCode();
        var outstanding = await summaryResponse.Content.ReadFromJsonAsync<CustomerCreditSummaryDto>(JsonOptions);
        Assert.Equal(0m, outstanding!.OutstandingAmount);
    }

    [Fact]
    public async Task Void_after_repayment_is_blocked()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Gatas", "Can", 40m, sku: "utang-milk-1");
        var customer = await CreateCustomerAsync(client, org, "Repay Utang");
        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                PosSaleOptions.UtangPaymentMethod,
                SaleId: Guid.NewGuid(),
                CustomerId: customer.CustomerId,
                CreditEntryId: Guid.NewGuid()));
        Assert.Equal(80m, sale.Total);

        using var repay = Scoped(HttpMethod.Post, $"{Customers}/{customer.CustomerId:D}/repayments", org);
        repay.Content = JsonContent.Create(new CreateRepaymentRequest(30m, "Partial"), options: JsonOptions);
        using var repayResponse = await client.SendAsync(repay);
        Assert.Equal(HttpStatusCode.Created, repayResponse.StatusCode);

        using var voidRequest = Scoped(HttpMethod.Post, $"{Sales}/{sale.SaleId:D}/void", org);
        voidRequest.Content = JsonContent.Create(new VoidSaleRequest("Too late"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        Assert.Equal(HttpStatusCode.Conflict, voidResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.SaleVoidBlockedBySubsequentUtangActivity,
            await ReadErrorCodeAsync(voidResponse));
    }

    [Fact]
    public async Task Utang_checkout_requires_create_sale_and_create_credit_grants()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Kendi", "Piece", 1m, sku: "utang-candy-1");
        var customer = await CreateCustomerAsync(client, org, "Capability Utang");
        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 1m)],
            PosSaleOptions.UtangPaymentMethod,
            CustomerId: customer.CustomerId,
            CreditEntryId: Guid.NewGuid());

        using var missingCredit = Scoped(
            HttpMethod.Post,
            Sales,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate},{PosFeatureCodes.CustomerCreditView}");
        missingCredit.Content = JsonContent.Create(body, options: JsonOptions);
        using var missingCreditResponse = await client.SendAsync(missingCredit);
        Assert.Equal(HttpStatusCode.Forbidden, missingCreditResponse.StatusCode);

        using var missingSale = Scoped(
            HttpMethod.Post,
            Sales,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditCreate},{PosFeatureCodes.StoreSalesView}");
        missingSale.Content = JsonContent.Create(body, options: JsonOptions);
        using var missingSaleResponse = await client.SendAsync(missingSale);
        Assert.Equal(HttpStatusCode.Forbidden, missingSaleResponse.StatusCode);

        using var pastDue = Scoped(
            HttpMethod.Post,
            Sales,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: string.Join(',', UtangCapabilityPolicy.DefaultDevelopmentGrants));
        pastDue.Content = JsonContent.Create(body, options: JsonOptions);
        using var pastDueResponse = await client.SendAsync(pastDue);
        Assert.Equal(HttpStatusCode.Forbidden, pastDueResponse.StatusCode);
    }

    [Fact]
    public async Task Cash_sale_still_works_without_customer_fields()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Load", "Piece", 100m, sku: "utang-cash-1");

        var sale = await CheckoutAsync(
            client,
            org,
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 1m)],
                PosSaleOptions.CashPaymentMethod,
                100m));

        Assert.Equal(PosSaleOptions.CashPaymentMethod, sale.PaymentMethod);
        Assert.Null(sale.CustomerId);
        Assert.Null(sale.LinkedCreditEntryId);
        Assert.Equal(100m, sale.Total);
        Assert.Equal(0m, sale.ChangeAmount);
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
