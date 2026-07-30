using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Statements;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosStatementReceiptAndCommercialApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Statement_and_receipt_reconcile_idempotent_and_org_isolated()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        var customer = await CreateCustomerAsync(client, OrgA, "Statement Rosa");
        var credit = await CreateCreditAsync(client, OrgA, customer.CustomerId, 100m, "Goods");
        var repayment = await CreateRepaymentAsync(client, OrgA, customer.CustomerId, 40m, "Partial");

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        using var statementReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/statement?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}&organizationDisplayName=Store%20A",
            OrgA);
        using var statementResponse = await client.SendAsync(statementReq);
        statementResponse.EnsureSuccessStatusCode();
        var statement = await statementResponse.Content.ReadFromJsonAsync<CustomerStatementDto>(JsonOptions);
        Assert.NotNull(statement);
        Assert.Equal(0m, statement!.OpeningBalance);
        Assert.Equal(60m, statement.ClosingBalance);
        Assert.Equal(statement.ClosingBalance, statement.OpeningBalance + statement.Lines.Sum(l => l.SignedEffect));
        Assert.Contains(statement.Lines, l => l.EntryId == credit.CreditEntryId);
        Assert.Contains(statement.Lines, l => l.EntryId == repayment.RepaymentId);

        using var receiptReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/receipt",
            OrgA);
        using var receipt1 = await client.SendAsync(receiptReq);
        receipt1.EnsureSuccessStatusCode();
        var first = await receipt1.Content.ReadFromJsonAsync<RepaymentReceiptDto>(JsonOptions);
        Assert.Equal(RepaymentReceiptService.BuildReceiptReference(repayment.RepaymentId), first!.ReceiptReference);
        Assert.False(first.IsReversed);

        using var receiptReq2 = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/receipt",
            OrgA);
        using var receipt2 = await client.SendAsync(receiptReq2);
        var second = await receipt2.Content.ReadFromJsonAsync<RepaymentReceiptDto>(JsonOptions);
        Assert.Equal(first.ReceiptReference, second!.ReceiptReference);

        using var reverse = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/reverse",
            OrgA,
            Actor);
        reverse.Content = JsonContent.Create(new ReverseRepaymentRequest("void"));
        using var reverseResponse = await client.SendAsync(reverse);
        reverseResponse.EnsureSuccessStatusCode();

        using var receiptReq3 = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/receipt",
            OrgA);
        using var receipt3 = await client.SendAsync(receiptReq3);
        var reversed = await receipt3.Content.ReadFromJsonAsync<RepaymentReceiptDto>(JsonOptions);
        Assert.True(reversed!.IsReversed);
        Assert.Equal("Reversed", reversed.Status);

        using var crossStatement = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/statement?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}",
            OrgB);
        using var crossStatementResponse = await client.SendAsync(crossStatement);
        Assert.Equal(HttpStatusCode.NotFound, crossStatementResponse.StatusCode);

        using var crossReceipt = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/receipt",
            OrgB);
        using var crossReceiptResponse = await client.SendAsync(crossReceipt);
        Assert.Equal(HttpStatusCode.NotFound, crossReceiptResponse.StatusCode);

        var statementEndpoints = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Statements", "StatementEndpoints.cs"));
        Assert.DoesNotContain("tax", statementEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gateway", statementEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("interest", statementEndpoints, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expired_continuity_allows_view_repay_statement_denies_mutations()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        // Seed under Active defaults, then exercise Expired continuity headers.
        var customer = await CreateCustomerAsync(client, OrgA, "Continuity Rosa");
        var credit = await CreateCreditAsync(client, OrgA, customer.CustomerId, 80m, "Seed");
        var creditToReverse = await CreateCreditAsync(client, OrgA, customer.CustomerId, 15m, "Correctable");
        var repayment = await CreateRepaymentAsync(client, OrgA, customer.CustomerId, 20m, "Seed pay");

        var continuity = $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditRepay}";

        using var listReq = CreateScopedRequest(
            HttpMethod.Get,
            "/api/v1/pos/customers",
            OrgA,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        using var listResponse = await client.SendAsync(listReq);
        listResponse.EnsureSuccessStatusCode();

        using var createCustomer = CreateScopedRequest(
            HttpMethod.Post,
            "/api/v1/pos/customers",
            OrgA,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        createCustomer.Content = JsonContent.Create(new CreateCustomerRequest("Blocked", null, null, null));
        using var createCustomerResponse = await client.SendAsync(createCustomer);
        Assert.Equal(HttpStatusCode.Forbidden, createCustomerResponse.StatusCode);

        using var createCredit = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries",
            OrgA,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        createCredit.Content = JsonContent.Create(new CreateCreditEntryRequest(10m, "Blocked"));
        using var createCreditResponse = await client.SendAsync(createCredit);
        Assert.Equal(HttpStatusCode.Forbidden, createCreditResponse.StatusCode);

        using var reverseRepayment = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{repayment.RepaymentId:D}/reverse",
            OrgA,
            Actor,
            PosSubscriptionStatuses.Expired,
            continuity);
        reverseRepayment.Content = JsonContent.Create(new ReverseRepaymentRequest("blocked"));
        using var reverseRepaymentResponse = await client.SendAsync(reverseRepayment);
        Assert.Equal(HttpStatusCode.Forbidden, reverseRepaymentResponse.StatusCode);

        using var dueDate = CreateScopedRequest(
            HttpMethod.Put,
            $"/api/v1/pos/credit/{credit.CreditEntryId:D}/due-date",
            OrgA,
            Actor,
            PosSubscriptionStatuses.Expired,
            continuity);
        dueDate.Content = JsonContent.Create(new SetCreditDueDateRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "blocked"));
        using var dueDateResponse = await client.SendAsync(dueDate);
        Assert.Equal(HttpStatusCode.Forbidden, dueDateResponse.StatusCode);

        using var repay = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor,
            PosSubscriptionStatuses.Expired,
            continuity);
        repay.Content = JsonContent.Create(new CreateRepaymentRequest(10m, "Allowed continuity repay"));
        using var repayResponse = await client.SendAsync(repay);
        Assert.Equal(HttpStatusCode.Created, repayResponse.StatusCode);

        using var reverseCredit = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries/{creditToReverse.CreditEntryId:D}/reverse",
            OrgA,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        reverseCredit.Content = JsonContent.Create(new ReverseCreditEntryRequest("continuity correction"));
        using var reverseCreditResponse = await client.SendAsync(reverseCredit);
        Assert.Equal(HttpStatusCode.OK, reverseCreditResponse.StatusCode);

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        using var statement = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/statement?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}",
            OrgA,
            status: PosSubscriptionStatuses.Expired,
            grants: continuity);
        using var statementResponse = await client.SendAsync(statement);
        statementResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Suspended_and_unknown_commercial_context_deny()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var customer = await CreateCustomerAsync(client, OrgA, "Suspended Rosa");

        using var suspended = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}",
            OrgA,
            status: PosSubscriptionStatuses.Suspended,
            grants: $"{PosFeatureCodes.CustomerCreditView},{PosFeatureCodes.CustomerCreditRepay}");
        using var suspendedResponse = await client.SendAsync(suspended);
        Assert.Equal(HttpStatusCode.Forbidden, suspendedResponse.StatusCode);

        // Explicit empty grants = known context with no features.
        using var noGrants = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}",
            OrgA,
            status: PosSubscriptionStatuses.Active,
            grants: "");
        noGrants.Headers.Remove(PosCommercialHeaders.FeatureGrantsHeaderName);
        noGrants.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, " ");
        using var noGrantsResponse = await client.SendAsync(noGrants);
        Assert.Equal(HttpStatusCode.Forbidden, noGrantsResponse.StatusCode);
    }

    private static async Task<POSCustomerDto> CreateCustomerAsync(HttpClient client, Guid orgId, string name)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", orgId);
        request.Content = JsonContent.Create(new CreateCustomerRequest(name, null, null, null));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);
        return customer!;
    }

    private static async Task<CreditEntryDto> CreateCreditAsync(
        HttpClient client, Guid orgId, Guid customerId, decimal amount, string remarks)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries",
            orgId);
        request.Content = JsonContent.Create(new CreateCreditEntryRequest(amount, remarks));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.NotNull(entry);
        return entry!;
    }

    private static async Task<RepaymentDto> CreateRepaymentAsync(
        HttpClient client, Guid orgId, Guid customerId, decimal amount, string remarks)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/repayments",
            orgId,
            Actor);
        request.Content = JsonContent.Create(new CreateRepaymentRequest(amount, remarks));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var repayment = await response.Content.ReadFromJsonAsync<RepaymentDto>(JsonOptions);
        Assert.NotNull(repayment);
        return repayment!;
    }

    private static HttpRequestMessage CreateScopedRequest(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? actorId = null,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        if (actorId is not null)
        {
            request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, actorId.Value.ToString("D"));
        }

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
