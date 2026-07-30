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
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// P6-WP06 closeout: full customer→credit→due date→repay→reverse→statement→receipt lifecycle
/// plus production commercial-header fail-closed behavior.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosUtangMvpCloseoutApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Full_utang_lifecycle_reconciles_outstanding_statement_and_receipt()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        // 1. Create customer
        var customer = await CreateCustomerAsync(client, OrgA, "Closeout Rosa");

        // 2. Record credit
        var credit = await CreateCreditAsync(client, OrgA, customer.CustomerId, 100m, "Goods on credit");

        // 3. Assign due date (past → overdue when unpaid remainder)
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        using var setDue = CreateScopedRequest(
            HttpMethod.Put,
            $"/api/v1/pos/credit/{credit.CreditEntryId:D}/due-date",
            OrgA,
            Actor);
        setDue.Content = JsonContent.Create(new SetCreditDueDateRequest(dueDate, "Agreed due date"));
        using var setDueResponse = await client.SendAsync(setDue);
        setDueResponse.EnsureSuccessStatusCode();

        // 4. Partial repayment
        var partial = await CreateRepaymentAsync(client, OrgA, customer.CustomerId, 40m, "Partial");
        await AssertOutstandingAsync(client, OrgA, customer.CustomerId, 60m);

        // 5. Exact remaining repayment
        var settle = await CreateRepaymentAsync(client, OrgA, customer.CustomerId, 60m, "Settle");
        await AssertOutstandingAsync(client, OrgA, customer.CustomerId, 0m);

        // Overpayment rejected
        using var over = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        over.Content = JsonContent.Create(new CreateRepaymentRequest(1m, "Over"));
        using var overResponse = await client.SendAsync(over);
        Assert.Equal(HttpStatusCode.Conflict, overResponse.StatusCode);
        var overProblem = await overResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(DomainErrorCodes.RepaymentOutstandingZero, overProblem.GetProperty("errorCode").GetString());

        // 6. Reverse exact repayment → outstanding back to 60
        using var reverseSettle = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{settle.RepaymentId:D}/reverse",
            OrgA,
            Actor);
        reverseSettle.Content = JsonContent.Create(new ReverseRepaymentRequest("Mistake settle"));
        using var reverseSettleResponse = await client.SendAsync(reverseSettle);
        reverseSettleResponse.EnsureSuccessStatusCode();
        await AssertOutstandingAsync(client, OrgA, customer.CustomerId, 60m);

        // FIFO overdue: unpaid remainder 60 on past due date
        using var overdueReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/overdue-summary",
            OrgA);
        using var overdueResponse = await client.SendAsync(overdueReq);
        overdueResponse.EnsureSuccessStatusCode();
        var overdue = await overdueResponse.Content.ReadFromJsonAsync<CustomerOverdueSummaryDto>(JsonOptions);
        Assert.Equal(60m, overdue!.OverdueAmount);
        Assert.Equal(1, overdue.OverdueCreditCount);

        // 7. Clear due date (history append-only)
        using var clearDue = CreateScopedRequest(
            HttpMethod.Delete,
            $"/api/v1/pos/credit/{credit.CreditEntryId:D}/due-date?reason=Cleared%20after%20review",
            OrgA,
            Actor);
        using var clearDueResponse = await client.SendAsync(clearDue);
        clearDueResponse.EnsureSuccessStatusCode();

        using var overdueAfterClear = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/overdue-summary",
            OrgA);
        using var overdueAfterClearResponse = await client.SendAsync(overdueAfterClear);
        var overdueCleared = await overdueAfterClearResponse.Content.ReadFromJsonAsync<CustomerOverdueSummaryDto>(JsonOptions);
        Assert.Equal(0m, overdueCleared!.OverdueAmount);

        // 8. Reverse credit blocked while repayment leaves negative risk? Outstanding 60; reversing 100 would go -40
        using var reverseCreditBlocked = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries/{credit.CreditEntryId:D}/reverse",
            OrgA);
        reverseCreditBlocked.Content = JsonContent.Create(new ReverseCreditEntryRequest("Would go negative"));
        using var reverseCreditBlockedResponse = await client.SendAsync(reverseCreditBlocked);
        Assert.Equal(HttpStatusCode.Conflict, reverseCreditBlockedResponse.StatusCode);

        // Reverse remaining active repayment then reverse credit
        using var reversePartial = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/repayments/{partial.RepaymentId:D}/reverse",
            OrgA,
            Actor);
        reversePartial.Content = JsonContent.Create(new ReverseRepaymentRequest("Undo partial"));
        using var reversePartialResponse = await client.SendAsync(reversePartial);
        reversePartialResponse.EnsureSuccessStatusCode();
        await AssertOutstandingAsync(client, OrgA, customer.CustomerId, 100m);

        using var reverseCredit = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries/{credit.CreditEntryId:D}/reverse",
            OrgA);
        reverseCredit.Content = JsonContent.Create(new ReverseCreditEntryRequest("Corrected entry"));
        using var reverseCreditResponse = await client.SendAsync(reverseCredit);
        reverseCreditResponse.EnsureSuccessStatusCode();
        await AssertOutstandingAsync(client, OrgA, customer.CustomerId, 0m);

        // 9–10. Statement + receipt (reversed repayment still retrievable)
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        using var statementReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/statement?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}",
            OrgA);
        using var statementResponse = await client.SendAsync(statementReq);
        statementResponse.EnsureSuccessStatusCode();
        var statement = await statementResponse.Content.ReadFromJsonAsync<CustomerStatementDto>(JsonOptions);
        Assert.NotNull(statement);
        Assert.Equal(statement!.ClosingBalance, statement.OpeningBalance + statement.Lines.Sum(l => l.SignedEffect));
        Assert.Contains(statement.Lines, l => l.IsReversed && l.EntryType == "Credit");
        Assert.Contains(statement.Lines, l => l.IsReversed && l.EntryType == "Repayment");

        using var receipt1 = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{settle.RepaymentId:D}/receipt",
            OrgA);
        using var receipt1Response = await client.SendAsync(receipt1);
        receipt1Response.EnsureSuccessStatusCode();
        var first = await receipt1Response.Content.ReadFromJsonAsync<RepaymentReceiptDto>(JsonOptions);
        Assert.True(first!.IsReversed);
        Assert.Equal(RepaymentReceiptService.BuildReceiptReference(settle.RepaymentId), first.ReceiptReference);

        using var receipt2 = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/repayments/{settle.RepaymentId:D}/receipt",
            OrgA);
        using var receipt2Response = await client.SendAsync(receipt2);
        var second = await receipt2Response.Content.ReadFromJsonAsync<RepaymentReceiptDto>(JsonOptions);
        Assert.Equal(first.ReceiptReference, second!.ReceiptReference);

        // Cross-org concealment
        using var cross = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/utang-summary",
            OrgB);
        using var crossResponse = await client.SendAsync(cross);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);
    }

    [Fact]
    public async Task Production_environment_ignores_commercial_headers_and_fails_closed()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString, environmentName: "Production");
        var client = factory.CreateClient();

        using var request = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/customers", OrgA);
        request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, "Active");
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.FeatureGrantsHeaderName,
            "customer-credit-view,customer-credit-repay,customer-credit-create");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var code = problem.GetProperty("errorCode").GetString();
        Assert.True(
            code is ApplicationErrorCodes.CommercialAccessUnknown
                or ApplicationErrorCodes.DevelopmentHeadersUnavailable,
            $"Unexpected Production fail-closed errorCode: {code}");
    }

    private static async Task AssertOutstandingAsync(HttpClient client, Guid orgId, Guid customerId, decimal expected)
    {
        using var req = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customerId:D}/utang-summary",
            orgId);
        using var response = await client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<CustomerUtangSummaryDto>(JsonOptions);
        Assert.Equal(expected, summary!.OutstandingAmount);
        Assert.Equal(summary.ActiveCreditTotal - summary.ActiveRepaymentTotal, summary.OutstandingAmount);
    }

    private static async Task<POSCustomerDto> CreateCustomerAsync(HttpClient client, Guid orgId, string name)
    {
        using var request = CreateScopedRequest(HttpMethod.Post, "/api/v1/pos/customers", orgId);
        request.Content = JsonContent.Create(new CreateCustomerRequest(name, null, null, null));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions))!;
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
        return (await response.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions))!;
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
        return (await response.Content.ReadFromJsonAsync<RepaymentDto>(JsonOptions))!;
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

    private sealed class PosApiFactory(string connectionString, string environmentName = "Testing")
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.UseSetting("Security:EnforceHttps", "false");
            if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                builder.UseSetting("AllowedHosts", "localhost;test");
            }

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString,
                    ["Security:EnforceHttps"] = "false"
                };
                if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
                {
                    values["AllowedHosts"] = "localhost;test";
                }

                config.AddInMemoryCollection(values);
            });
        }
    }
}
