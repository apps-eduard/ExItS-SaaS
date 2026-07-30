using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosRepaymentApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Repayment_overpayment_ledger_reversal_and_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        var createdCustomer = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Pay Rosa", null, null, null));
        createdCustomer.EnsureSuccessStatusCode();
        var customer = await createdCustomer.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        using var creditReq = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer!.CustomerId:D}/credit-entries",
            OrgA);
        creditReq.Content = JsonContent.Create(new CreateCreditEntryRequest(100m, "Goods"));
        using var creditResponse = await client.SendAsync(creditReq);
        creditResponse.EnsureSuccessStatusCode();

        using var over = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        over.Content = JsonContent.Create(new CreateRepaymentRequest(150m, "Too much"));
        using var overResponse = await client.SendAsync(over);
        Assert.Equal(HttpStatusCode.Conflict, overResponse.StatusCode);
        var overProblem = await overResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(DomainErrorCodes.RepaymentExceedsOutstanding, overProblem.GetProperty("errorCode").GetString());

        using var partial = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        partial.Content = JsonContent.Create(new CreateRepaymentRequest(40m, "Partial"));
        using var partialResponse = await client.SendAsync(partial);
        Assert.Equal(HttpStatusCode.Created, partialResponse.StatusCode);
        var repayment = await partialResponse.Content.ReadFromJsonAsync<RepaymentDto>(JsonOptions);
        Assert.NotNull(repayment);

        using var summaryReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/utang-summary",
            OrgA);
        using var summaryResponse = await client.SendAsync(summaryReq);
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<CustomerUtangSummaryDto>(JsonOptions);
        Assert.Equal(60m, summary!.OutstandingAmount);

        using var exact = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        exact.Content = JsonContent.Create(new CreateRepaymentRequest(60m, "Settle"));
        using var exactResponse = await client.SendAsync(exact);
        exactResponse.EnsureSuccessStatusCode();

        using var zero = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        zero.Content = JsonContent.Create(new CreateRepaymentRequest(1m, "Zero"));
        using var zeroResponse = await client.SendAsync(zero);
        Assert.Equal(HttpStatusCode.Conflict, zeroResponse.StatusCode);

        using var ledgerReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/ledger?page=1&pageSize=50",
            OrgA);
        using var ledgerResponse = await client.SendAsync(ledgerReq);
        ledgerResponse.EnsureSuccessStatusCode();
        var ledger = await ledgerResponse.Content.ReadFromJsonAsync<PagedResult<LedgerEntryDto>>(JsonOptions);
        Assert.Equal(3, ledger!.TotalCount);
        Assert.Contains(ledger.Items, i => i.EntryType == "Credit");
        Assert.Contains(ledger.Items, i => i.EntryType == "Repayment");
        Assert.True(ledger.Items.Zip(ledger.Items.Skip(1)).All(pair =>
            pair.First.RecordedAtUtc <= pair.Second.RecordedAtUtc));

        using var reverse = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{repayment!.RepaymentId:D}/reverse",
            OrgA,
            Actor);
        reverse.Content = JsonContent.Create(new ReverseRepaymentRequest("Undo partial"));
        using var reverseResponse = await client.SendAsync(reverse);
        reverseResponse.EnsureSuccessStatusCode();

        using var afterReverseSummary = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/utang-summary",
            OrgA);
        using var afterReverseResponse = await client.SendAsync(afterReverseSummary);
        var after = await afterReverseResponse.Content.ReadFromJsonAsync<CustomerUtangSummaryDto>(JsonOptions);
        Assert.Equal(40m, after!.OutstandingAmount);

        using var cross = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/utang-summary",
            OrgB);
        using var crossResponse = await client.SendAsync(cross);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);

        var endpointSource = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Payments", "RepaymentEndpoints.cs"));
        Assert.DoesNotContain("due_date", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("statement", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("receipt", endpointSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gateway", endpointSource, StringComparison.OrdinalIgnoreCase);
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

    private static HttpRequestMessage CreateScopedRequest(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? actorId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        if (actorId is not null)
        {
            request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, actorId.Value.ToString("D"));
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
