using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCustomerApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Customer_lifecycle_search_isolation_and_duplicate_mobile()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        var withoutMobile = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Aling Rosa", null, "Corner", "Neighbor"));
        Assert.Equal(HttpStatusCode.Created, withoutMobile.StatusCode);
        var rosa = await withoutMobile.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(rosa);
        Assert.Null(rosa!.MobileNumber);

        var withMobile = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Juan", "0917-111-2222", null, null));
        Assert.Equal(HttpStatusCode.Created, withMobile.StatusCode);
        var juan = await withMobile.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(juan);

        var duplicate = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Clone", "09171112222", null, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(ApplicationErrorCodes.MobileConflict, problem.GetProperty("errorCode").GetString());

        var otherOrg = await PostCustomerAsync(client, OrgB, new CreateCustomerRequest("Other Org Juan", "0917-111-2222", null, null));
        Assert.Equal(HttpStatusCode.Created, otherOrg.StatusCode);

        using var searchByName = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers?search=Rosa&page=1&pageSize=20", OrgA);
        using var searchNameResponse = await client.SendAsync(searchByName);
        searchNameResponse.EnsureSuccessStatusCode();
        var named = await searchNameResponse.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.Contains(named!.Items, i => i.CustomerId == rosa.CustomerId);

        using var searchByMobile = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers?search=09171112222&page=1&pageSize=20", OrgA);
        using var searchMobileResponse = await client.SendAsync(searchByMobile);
        searchMobileResponse.EnsureSuccessStatusCode();
        var mobiles = await searchMobileResponse.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.Contains(mobiles!.Items, i => i.CustomerId == juan!.CustomerId);

        using var update = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/customers/{juan.CustomerId:D}", OrgA);
        update.Content = JsonContent.Create(new UpdateCustomerRequest("Juan Updated", "0917-111-2222", "New address", "ID note"));
        using var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Equal("Juan Updated", updated!.DisplayName);
        Assert.Equal(OrgA, updated.OrganizationId);

        using var deactivate = CreateScopedRequest(HttpMethod.Post, $"/api/v1/pos/customers/{juan.CustomerId:D}/deactivate", OrgA);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        using var reactivate = CreateScopedRequest(HttpMethod.Post, $"/api/v1/pos/customers/{juan.CustomerId:D}/reactivate", OrgA);
        using var reactivateResponse = await client.SendAsync(reactivate);
        reactivateResponse.EnsureSuccessStatusCode();

        using var crossGet = CreateScopedRequest(HttpMethod.Get, $"/api/v1/pos/customers/{juan.CustomerId:D}", OrgB);
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        using var crossUpdate = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/customers/{juan.CustomerId:D}", OrgB);
        crossUpdate.Content = JsonContent.Create(new UpdateCustomerRequest("Hacked", null, null, null));
        using var crossUpdateResponse = await client.SendAsync(crossUpdate);
        Assert.Equal(HttpStatusCode.NotFound, crossUpdateResponse.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        _ = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var endpointSource = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Customers", "CustomerEndpoints.cs"));
        Assert.DoesNotContain("credit_account", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LedgerEntry", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordRepayment", endpointSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Concurrent_updates_surface_concurrency_conflict()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var created = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Race", "09170001111", null, null));
        created.EnsureSuccessStatusCode();
        var customer = await created.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        await using var dbA = new PosDbContext(options);
        await using var dbB = new PosDbContext(options);
        var rowA = await dbA.Set<ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers.POSCustomerRecord>()
            .FirstAsync(c => c.Id == customer!.CustomerId);
        var rowB = await dbB.Set<ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers.POSCustomerRecord>()
            .FirstAsync(c => c.Id == customer.CustomerId);

        rowA.DisplayName = "Race A";
        rowB.DisplayName = "Race B";
        await dbA.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    [Fact]
    public async Task Pagination_is_stable_by_display_name_then_id()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        foreach (var name in new[] { "Zed", "Amy", "Mia", "Bob" })
        {
            (await PostCustomerAsync(client, org, new CreateCustomerRequest(name, null, null, null))).EnsureSuccessStatusCode();
        }

        using var page1Request = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers?page=1&pageSize=2", org);
        using var page1 = await client.SendAsync(page1Request);
        page1.EnsureSuccessStatusCode();
        var first = await page1.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.Equal(new[] { "Amy", "Bob" }, first!.Items.Select(i => i.DisplayName).ToArray());

        using var page2Request = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers?page=2&pageSize=2", org);
        using var page2 = await client.SendAsync(page2Request);
        page2.EnsureSuccessStatusCode();
        var second = await page2.Content.ReadFromJsonAsync<PagedResult<POSCustomerDto>>(JsonOptions);
        Assert.Equal(new[] { "Mia", "Zed" }, second!.Items.Select(i => i.DisplayName).ToArray());
    }

    [Fact]
    public async Task Platform_correlation_is_org_scoped_unique_and_optional_for_legacy_customers()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var platformId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var otherId = Guid.Parse("ffffffff-bbbb-cccc-dddd-eeeeeeeeeeee");

        var legacy = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Legacy", null, null, null));
        Assert.Equal(HttpStatusCode.Created, legacy.StatusCode);
        var legacyDto = await legacy.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Null(legacyDto!.PlatformBusinessCustomerId);

        var created = await PostCustomerAsync(
            client,
            OrgA,
            new CreateCustomerRequest("Rosa", null, null, null, PlatformBusinessCustomerId: platformId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rosa = await created.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Equal(platformId, rosa!.PlatformBusinessCustomerId);

        var duplicate = await PostCustomerAsync(
            client,
            OrgA,
            new CreateCustomerRequest("Clone", null, null, null, PlatformBusinessCustomerId: platformId));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var otherOrg = await PostCustomerAsync(
            client,
            OrgB,
            new CreateCustomerRequest("Other Org", null, null, null, PlatformBusinessCustomerId: platformId));
        Assert.Equal(HttpStatusCode.Created, otherOrg.StatusCode);

        using var found = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/by-platform-business-customer/{platformId:D}",
            OrgA);
        using var foundResponse = await client.SendAsync(found);
        foundResponse.EnsureSuccessStatusCode();
        var foundDto = await foundResponse.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Equal(rosa.CustomerId, foundDto!.CustomerId);

        using var crossGet = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/by-platform-business-customer/{platformId:D}",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        using var conflictPut = CreateScopedRequest(
            HttpMethod.Put,
            $"/api/v1/pos/customers/{rosa.CustomerId:D}/platform-correlation",
            OrgA);
        conflictPut.Content = JsonContent.Create(new CorrelatePlatformBusinessCustomerRequest(otherId));
        using var conflictResponse = await client.SendAsync(conflictPut);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        using var idempotentPut = CreateScopedRequest(
            HttpMethod.Put,
            $"/api/v1/pos/customers/{rosa.CustomerId:D}/platform-correlation",
            OrgA);
        idempotentPut.Content = JsonContent.Create(new CorrelatePlatformBusinessCustomerRequest(platformId));
        using var idempotentResponse = await client.SendAsync(idempotentPut);
        idempotentResponse.EnsureSuccessStatusCode();

        using var clear = CreateScopedRequest(
            HttpMethod.Delete,
            $"/api/v1/pos/customers/{rosa.CustomerId:D}/platform-correlation",
            OrgA);
        using var clearResponse = await client.SendAsync(clear);
        clearResponse.EnsureSuccessStatusCode();
        var cleared = await clearResponse.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Null(cleared!.PlatformBusinessCustomerId);

        using var missing = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/by-platform-business-customer/{platformId:D}",
            OrgA);
        using var missingResponse = await client.SendAsync(missing);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Personal_exits_id_is_org_scoped_unique_on_create()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        const string personalId = "EX-4827-1936";

        var created = await PostCustomerAsync(
            client,
            OrgA,
            new CreateCustomerRequest("Rosa", null, null, null, LinkedPersonalPublicUserId: personalId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rosa = await created.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.Equal(personalId, rosa!.LinkedPersonalPublicUserId);

        var duplicate = await PostCustomerAsync(
            client,
            OrgA,
            new CreateCustomerRequest("Clone", null, null, null, LinkedPersonalPublicUserId: "ex-4827-1936"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var otherOrg = await PostCustomerAsync(
            client,
            OrgB,
            new CreateCustomerRequest("Other Org", null, null, null, LinkedPersonalPublicUserId: personalId));
        Assert.Equal(HttpStatusCode.Created, otherOrg.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostCustomerAsync(
        HttpClient client,
        Guid organizationId,
        CreateCustomerRequest body)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", organizationId);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateScopedRequest(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
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
