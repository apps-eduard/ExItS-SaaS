using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCustomerCreditOfflineIdempotencyApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Post_customer_with_same_idempotency_headers_creates_once()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var idempotencyKey = "cust-create-once";
        var body = new CreateCustomerRequest("Idempotent Customer", "09171230001", "Addr", "Notes", customerId);
        var payloadHash = ComputePayloadHash(body);

        var first = await PostCustomerWithIdempotencyAsync(
            client, org, body, idempotencyKey, payloadHash, operationId);
        var second = await PostCustomerWithIdempotencyAsync(
            client, org, body, idempotencyKey, payloadHash, operationId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var created = await first.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        var replay = await second.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotNull(replay);
        Assert.Equal(created!.CustomerId, replay!.CustomerId);
        Assert.Equal(customerId, created.CustomerId);

        using var listRequest = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers?page=1&pageSize=20", org);
        using var listResponse = await client.SendAsync(listRequest);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.Single(page!.Items.Where(i => i.CustomerId == customerId));
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_payload_hash_returns_conflict()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var idempotencyKey = "cust-hash-mismatch";
        var operationId = Guid.NewGuid();

        var firstBody = new CreateCustomerRequest("Hash A", "09171230002", null, null, Guid.NewGuid());
        var first = await PostCustomerWithIdempotencyAsync(
            client,
            org,
            firstBody,
            idempotencyKey,
            ComputePayloadHash(firstBody),
            operationId);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var secondBody = new CreateCustomerRequest("Hash B", "09171230003", null, null, Guid.NewGuid());
        var second = await PostCustomerWithIdempotencyAsync(
            client,
            org,
            secondBody,
            idempotencyKey,
            ComputePayloadHash(secondBody),
            Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var conflict = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("conflict_payload_mismatch", conflict.GetProperty("outcomeCode").GetString());
    }

    [Fact]
    public async Task Sync_customers_endpoint_returns_created_items()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var created = await PostCustomerAsync(client, org, new CreateCustomerRequest("Sync Me", "09171230004", null, null));
        created.EnsureSuccessStatusCode();
        var customer = await created.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        using var syncRequest = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/sync/customers?page=1&pageSize=20", org);
        using var syncResponse = await client.SendAsync(syncRequest);
        syncResponse.EnsureSuccessStatusCode();

        var syncPage = await syncResponse.Content.ReadFromJsonAsync<CustomerSyncPageDto>(JsonOptions);
        Assert.NotNull(syncPage);
        Assert.Contains(syncPage!.Items, i => i.CustomerId == customer!.CustomerId);
    }

    private static async Task<HttpResponseMessage> PostCustomerAsync(
        HttpClient client,
        Guid organizationId,
        CreateCustomerRequest body)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", organizationId);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostCustomerWithIdempotencyAsync(
        HttpClient client,
        Guid organizationId,
        CreateCustomerRequest body,
        string idempotencyKey,
        string payloadHash,
        Guid operationId)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", organizationId);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", payloadHash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", operationId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.CustomerCreate);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static string ComputePayloadHash(CreateCustomerRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static HttpRequestMessage CreateScopedRequest(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
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
