using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCreditApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task Credit_create_summary_reverse_and_org_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        var createdCustomer = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Credit Rosa", null, null, null));
        createdCustomer.EnsureSuccessStatusCode();
        var customer = await createdCustomer.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        using var createEntry = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer!.CustomerId:D}/credit-entries",
            OrgA);
        createEntry.Content = JsonContent.Create(new CreateCreditEntryRequest(125.75m, "Rice and soap"));
        using var createResponse = await client.SendAsync(createEntry);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var entry = await createResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal(125.75m, entry!.Amount);
        Assert.Equal("Active", entry.Status);

        using var second = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries",
            OrgA);
        second.Content = JsonContent.Create(new CreateCreditEntryRequest(50m, "Eggs"));
        using var secondResponse = await client.SendAsync(second);
        secondResponse.EnsureSuccessStatusCode();

        using var summaryRequest = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-summary",
            OrgA);
        using var summaryResponse = await client.SendAsync(summaryRequest);
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<CustomerCreditSummaryDto>(JsonOptions);
        Assert.Equal(175.75m, summary!.OutstandingAmount);
        Assert.Equal(2, summary.ActiveEntryCount);

        using var reverse = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries/{entry.CreditEntryId:D}/reverse",
            OrgA);
        reverse.Content = JsonContent.Create(new ReverseCreditEntryRequest("Wrong amount recorded"));
        using var reverseResponse = await client.SendAsync(reverse);
        reverseResponse.EnsureSuccessStatusCode();

        using var summaryAfter = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-summary",
            OrgA);
        using var summaryAfterResponse = await client.SendAsync(summaryAfter);
        var after = await summaryAfterResponse.Content.ReadFromJsonAsync<CustomerCreditSummaryDto>(JsonOptions);
        Assert.Equal(50m, after!.OutstandingAmount);
        Assert.Equal(1, after.ActiveEntryCount);
        Assert.Equal(2, after.TotalEntryCount);

        using var history = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries?page=1&pageSize=20",
            OrgA);
        using var historyResponse = await client.SendAsync(history);
        historyResponse.EnsureSuccessStatusCode();
        var page = await historyResponse.Content.ReadFromJsonAsync<PagedResult<CreditEntryDto>>(JsonOptions);
        Assert.Equal(2, page!.TotalCount);
        Assert.Contains(page.Items, i => i.Status == "Reversed");

        using var crossSummary = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-summary",
            OrgB);
        using var crossSummaryResponse = await client.SendAsync(crossSummary);
        Assert.Equal(HttpStatusCode.NotFound, crossSummaryResponse.StatusCode);

        using var crossCreate = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries",
            OrgB);
        crossCreate.Content = JsonContent.Create(new CreateCreditEntryRequest(9m, "Hack"));
        using var crossCreateResponse = await client.SendAsync(crossCreate);
        Assert.Equal(HttpStatusCode.NotFound, crossCreateResponse.StatusCode);

        using var deactivate = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/deactivate",
            OrgA);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        using var createWhileInactive = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries",
            OrgA);
        createWhileInactive.Content = JsonContent.Create(new CreateCreditEntryRequest(5m, "Should fail"));
        using var inactiveResponse = await client.SendAsync(createWhileInactive);
        Assert.Equal(HttpStatusCode.Conflict, inactiveResponse.StatusCode);

        var endpointSource = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Credit", "CreditEndpoints.cs"));
        Assert.DoesNotContain("RecordRepayment", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("payment_allocation", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("interest", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credit_limit", endpointSource, StringComparison.OrdinalIgnoreCase);
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
