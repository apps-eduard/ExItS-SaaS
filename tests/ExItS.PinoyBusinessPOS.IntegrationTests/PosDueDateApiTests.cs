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
public sealed class PosDueDateApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Due_dates_fifo_overdue_history_and_org_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();

        var createdCustomer = await PostCustomerAsync(client, OrgA, new CreateCustomerRequest("Due Rosa", null, null, null));
        createdCustomer.EnsureSuccessStatusCode();
        var customer = await createdCustomer.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);
        Assert.NotNull(customer);

        var first = await CreateCreditAsync(client, OrgA, customer!.CustomerId, 100m, "Oldest");
        var second = await CreateCreditAsync(client, OrgA, customer.CustomerId, 80m, "Next");
        Assert.Null(first.CurrentDueDate);

        using var setPast = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/credit/{first.CreditEntryId:D}/due-date", OrgA, Actor);
        setPast.Content = JsonContent.Create(new SetCreditDueDateRequest(new DateOnly(2020, 1, 1), "Past due agreed"));
        using var setPastResponse = await client.SendAsync(setPast);
        Assert.Equal(HttpStatusCode.OK, setPastResponse.StatusCode);
        var pastEntry = await setPastResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.Equal(new DateOnly(2020, 1, 1), pastEntry!.CurrentDueDate);

        using var setFuture = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/credit/{second.CreditEntryId:D}/due-date", OrgA, Actor);
        setFuture.Content = JsonContent.Create(new SetCreditDueDateRequest(DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)), "Future"));
        using var setFutureResponse = await client.SendAsync(setFuture);
        setFutureResponse.EnsureSuccessStatusCode();

        using var noReason = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/credit/{second.CreditEntryId:D}/due-date", OrgA, Actor);
        noReason.Content = JsonContent.Create(new SetCreditDueDateRequest(new DateOnly(2026, 9, 1), " "));
        using var noReasonResponse = await client.SendAsync(noReason);
        Assert.Equal(HttpStatusCode.BadRequest, noReasonResponse.StatusCode);
        var noReasonProblem = await noReasonResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(DomainErrorCodes.InvalidCreditDueDateReason, noReasonProblem.GetProperty("errorCode").GetString());

        using var unchanged = CreateScopedRequest(HttpMethod.Put, $"/api/v1/pos/credit/{first.CreditEntryId:D}/due-date", OrgA, Actor);
        unchanged.Content = JsonContent.Create(new SetCreditDueDateRequest(new DateOnly(2020, 1, 1), "Same date"));
        using var unchangedResponse = await client.SendAsync(unchanged);
        Assert.Equal(HttpStatusCode.Conflict, unchangedResponse.StatusCode);
        var unchangedProblem = await unchangedResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(DomainErrorCodes.CreditDueDateUnchanged, unchangedProblem.GetProperty("errorCode").GetString());

        using var historyReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/credit/{first.CreditEntryId:D}/due-date-history?page=1&pageSize=20",
            OrgA);
        using var historyResponse = await client.SendAsync(historyReq);
        historyResponse.EnsureSuccessStatusCode();
        var history = await historyResponse.Content.ReadFromJsonAsync<PagedResult<CreditDueDateChangeDto>>(JsonOptions);
        Assert.True(history!.TotalCount >= 1);
        Assert.Equal("Past due agreed", history.Items[0].Reason);

        using var overdueSummaryReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/overdue-summary",
            OrgA);
        using var overdueSummaryResponse = await client.SendAsync(overdueSummaryReq);
        overdueSummaryResponse.EnsureSuccessStatusCode();
        var overdueSummary = await overdueSummaryResponse.Content.ReadFromJsonAsync<CustomerOverdueSummaryDto>(JsonOptions);
        Assert.Equal(180m, overdueSummary!.OutstandingAmount);
        Assert.Equal(100m, overdueSummary.OverdueAmount);
        Assert.Equal(1, overdueSummary.OverdueCreditCount);

        using var repay = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/repayments",
            OrgA,
            Actor);
        repay.Content = JsonContent.Create(new CreateRepaymentRequest(100m, "FIFO clear oldest"));
        using var repayResponse = await client.SendAsync(repay);
        repayResponse.EnsureSuccessStatusCode();

        using var afterPaySummaryReq = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/overdue-summary",
            OrgA);
        using var afterPaySummaryResponse = await client.SendAsync(afterPaySummaryReq);
        var afterPay = await afterPaySummaryResponse.Content.ReadFromJsonAsync<CustomerOverdueSummaryDto>(JsonOptions);
        Assert.Equal(80m, afterPay!.OutstandingAmount);
        Assert.Equal(0m, afterPay.OverdueAmount);
        Assert.Equal(0, afterPay.OverdueCreditCount);

        using var reverseCredit = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/credit-entries/{second.CreditEntryId:D}/reverse",
            OrgA);
        reverseCredit.Content = JsonContent.Create(new ReverseCreditEntryRequest("Cancel remaining"));
        using var reverseCreditResponse = await client.SendAsync(reverseCredit);
        reverseCreditResponse.EnsureSuccessStatusCode();

        using var setOnReversed = CreateScopedRequest(
            HttpMethod.Put,
            $"/api/v1/pos/credit/{second.CreditEntryId:D}/due-date",
            OrgA,
            Actor);
        setOnReversed.Content = JsonContent.Create(new SetCreditDueDateRequest(new DateOnly(2020, 2, 2), "Should fail"));
        using var setOnReversedResponse = await client.SendAsync(setOnReversed);
        Assert.Equal(HttpStatusCode.Conflict, setOnReversedResponse.StatusCode);

        using var clear = CreateScopedRequest(
            HttpMethod.Delete,
            $"/api/v1/pos/credit/{first.CreditEntryId:D}/due-date?reason={Uri.EscapeDataString("Cleared after full FIFO offset")}",
            OrgA,
            Actor);
        using var clearResponse = await client.SendAsync(clear);
        clearResponse.EnsureSuccessStatusCode();
        var cleared = await clearResponse.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.Null(cleared!.CurrentDueDate);
        Assert.Equal(100m, cleared.Amount);

        using var overdueCustomers = CreateScopedRequest(HttpMethod.Get, "/api/v1/pos/overdue/customers", OrgA);
        using var overdueCustomersResponse = await client.SendAsync(overdueCustomers);
        overdueCustomersResponse.EnsureSuccessStatusCode();

        using var cross = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/customers/{customer.CustomerId:D}/overdue-summary",
            OrgB);
        using var crossResponse = await client.SendAsync(cross);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);

        using var crossHistory = CreateScopedRequest(
            HttpMethod.Get,
            $"/api/v1/pos/credit/{first.CreditEntryId:D}/due-date-history",
            OrgB);
        using var crossHistoryResponse = await client.SendAsync(crossHistory);
        Assert.Equal(HttpStatusCode.NotFound, crossHistoryResponse.StatusCode);

        var dueDateEndpoints = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Credit", "DueDateEndpoints.cs"));
        Assert.DoesNotContain("statement", dueDateEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("receipt", dueDateEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("installment", dueDateEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("interest", dueDateEndpoints, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("allocation", dueDateEndpoints, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CreditEntryDto> CreateCreditAsync(
        HttpClient client,
        Guid organizationId,
        Guid customerId,
        decimal amount,
        string remarks)
    {
        using var request = CreateScopedRequest(
            HttpMethod.Post,
            $"/api/v1/pos/customers/{customerId:D}/credit-entries",
            organizationId);
        request.Content = JsonContent.Create(new CreateCreditEntryRequest(amount, remarks));
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var entry = await response.Content.ReadFromJsonAsync<CreditEntryDto>(JsonOptions);
        Assert.NotNull(entry);
        return entry!;
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
