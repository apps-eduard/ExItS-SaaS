using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPaymentOfflineIdempotencyApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Post_repayment_with_same_idempotency_headers_creates_once()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var customer = await SeedCustomerWithCreditAsync(client, org, 100m);
        var repaymentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var idempotencyKey = "repay-create-once";
        var body = new CreateRepaymentRequest(40m, "Partial", repaymentId);
        var payloadHash = ComputePayloadHash(body);

        var first = await PostRepaymentWithIdempotencyAsync(
            client, org, customer.CustomerId, body, idempotencyKey, payloadHash, operationId);
        var second = await PostRepaymentWithIdempotencyAsync(
            client, org, customer.CustomerId, body, idempotencyKey, payloadHash, operationId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var created = await first.Content.ReadFromJsonAsync<PosRepaymentDto>(JsonOptions);
        var replay = await second.Content.ReadFromJsonAsync<PosRepaymentDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotNull(replay);
        Assert.Equal(created!.RepaymentId, replay!.RepaymentId);
        Assert.Equal(repaymentId, created.RepaymentId);

        using var listRequest = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments?page=1&pageSize=20",
            org,
            Actor);
        using var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PosRepaymentPagedResult>(JsonOptions);
        Assert.Single(page!.Items.Where(i => i.RepaymentId == repaymentId));
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_payload_hash_returns_conflict()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var customer = await SeedCustomerWithCreditAsync(client, org, 80m);
        var idempotencyKey = "repay-hash-mismatch";
        var operationId = Guid.NewGuid();

        var firstBody = new CreateRepaymentRequest(20m, "Hash A", Guid.NewGuid());
        var first = await PostRepaymentWithIdempotencyAsync(
            client,
            org,
            customer.CustomerId,
            firstBody,
            idempotencyKey,
            ComputePayloadHash(firstBody),
            operationId);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var secondBody = new CreateRepaymentRequest(30m, "Hash B", Guid.NewGuid());
        var second = await PostRepaymentWithIdempotencyAsync(
            client,
            org,
            customer.CustomerId,
            secondBody,
            idempotencyKey,
            ComputePayloadHash(secondBody),
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var conflict = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("conflict_payload_mismatch", conflict.GetProperty("outcomeCode").GetString());
    }

    [Fact]
    public async Task Sync_repayments_endpoint_returns_created_items()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var customer = await SeedCustomerWithCreditAsync(client, org, 50m);

        var created = await PostRepaymentAsync(client, org, customer.CustomerId, new CreateRepaymentRequest(15m, "Sync me"));
        created.EnsureSuccessStatusCode();
        var repayment = await created.Content.ReadFromJsonAsync<PosRepaymentDto>(JsonOptions);
        Assert.NotNull(repayment);

        using var syncRequest = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/sync/repayments?page=1&pageSize=20", org, Actor);
        using var syncResponse = await client.SendAsync(syncRequest);
        syncResponse.EnsureSuccessStatusCode();

        var syncPage = await syncResponse.Content.ReadFromJsonAsync<PosRepaymentSyncPageResult>(JsonOptions);
        Assert.NotNull(syncPage);
        Assert.Contains(syncPage!.Items, i => i.RepaymentId == repayment!.RepaymentId);
    }

    private static async Task<POSCustomerDto> SeedCustomerWithCreditAsync(HttpClient client, Guid org, decimal creditAmount)
    {
        var createdCustomer = await PostCustomerAsync(client, org, new CreateCustomerRequest("Repay Customer", "09171230010", null, null));
        createdCustomer.EnsureSuccessStatusCode();
        var customer = await createdCustomer.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        using var creditReq = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer!.CustomerId:D}/credit-entries",
            org,
            Actor);
        creditReq.Content = JsonContent.Create(new CreateCreditEntryRequest(creditAmount, "Goods"), options: JsonOptions);
        using var creditResponse = await client.SendAsync(creditReq);
        creditResponse.EnsureSuccessStatusCode();

        return customer;
    }

    private static async Task<HttpResponseMessage> PostCustomerAsync(
        HttpClient client,
        Guid organizationId,
        CreateCustomerRequest body)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", organizationId, Actor);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostRepaymentAsync(
        HttpClient client,
        Guid organizationId,
        Guid customerId,
        CreateRepaymentRequest body)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/repayments",
            organizationId,
            Actor);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostRepaymentWithIdempotencyAsync(
        HttpClient client,
        Guid organizationId,
        Guid customerId,
        CreateRepaymentRequest body,
        string idempotencyKey,
        string payloadHash,
        Guid operationId)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/repayments",
            organizationId,
            Actor);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", payloadHash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", operationId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.RepaymentCreate);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static string ComputePayloadHash(CreateRepaymentRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static HttpRequestMessage CreateScopedRequest(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid actorId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, actorId.ToString("D"));
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
