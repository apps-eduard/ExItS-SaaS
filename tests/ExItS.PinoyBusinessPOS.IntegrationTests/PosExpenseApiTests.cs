using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosExpenseApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Categories = "/api/v1/pos/expense-categories";
    private const string Expenses = "/api/v1/pos/expenses";

    [Fact]
    public async Task Create_cash_and_gcash_expenses_void_and_isolate_orgs()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var category = await CreateCategoryAsync(client, orgA, "Utilities");
        await CreateCategoryAsync(client, orgB, "Other Org Category");

        using var cash = Scoped(HttpMethod.Post, Expenses, orgA);
        cash.Content = JsonContent.Create(
            new RecordExpenseRequest(
                category.CategoryId,
                "Cash",
                250.50m,
                "Electric bill",
                new DateOnly(2026, 7, 30),
                Payee: "Meralco"),
            options: JsonOptions);
        using var cashResponse = await client.SendAsync(cash);
        Assert.Equal(HttpStatusCode.Created, cashResponse.StatusCode);
        var cashExpense = await cashResponse.Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);
        Assert.NotNull(cashExpense);
        Assert.StartsWith("EXP-", cashExpense!.ExpenseNumber, StringComparison.Ordinal);
        Assert.Equal("Cash", cashExpense.PaymentMethod);
        Assert.Equal(250.50m, cashExpense.Amount);
        Assert.Equal("Recorded", cashExpense.Status);
        Assert.Null(cashExpense.GCashReference);

        using var gcash = Scoped(HttpMethod.Post, Expenses, orgA);
        gcash.Content = JsonContent.Create(
            new RecordExpenseRequest(
                category.CategoryId,
                "ManualGCash",
                100m,
                "Delivery fee",
                new DateOnly(2026, 7, 30),
                GCashReference: "GC-123"),
            options: JsonOptions);
        using var gcashResponse = await client.SendAsync(gcash);
        Assert.Equal(HttpStatusCode.Created, gcashResponse.StatusCode);
        var gcashExpense = await gcashResponse.Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);
        Assert.Equal("ManualGCash", gcashExpense!.PaymentMethod);
        Assert.Equal("GC-123", gcashExpense.GCashReference);

        using var voidRequest = Scoped(HttpMethod.Post, $"{Expenses}/{cashExpense.ExpenseId:D}/void", orgA);
        voidRequest.Content = JsonContent.Create(new VoidExpenseRequest("Wrong bill"), options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var voided = await voidResponse.Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);
        Assert.Equal("Voided", voided!.Status);
        Assert.Equal("Wrong bill", voided.VoidReason);

        using var crossOrg = Scoped(HttpMethod.Get, $"{Expenses}/{cashExpense.ExpenseId:D}", orgB);
        using var crossResponse = await client.SendAsync(crossOrg);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);
    }

    [Fact]
    public async Task Idempotent_create_replays_same_expense()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var category = await CreateCategoryAsync(client, org, "Supplies");
        var expenseId = Guid.NewGuid();
        var key = Guid.NewGuid().ToString("D");
        var operationId = Guid.NewGuid();

        var body = new RecordExpenseRequest(
            category.CategoryId,
            "Cash",
            40m,
            "Bags",
            new DateOnly(2026, 7, 30),
            ExpenseId: expenseId);
        var payloadHash = ComputePayloadHash(body);

        using var firstResponse = await PostExpenseWithIdempotencyAsync(client, org, body, key, payloadHash, operationId);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var created = await firstResponse.Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);

        using var secondResponse = await PostExpenseWithIdempotencyAsync(client, org, body, key, payloadHash, operationId);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var replayed = await secondResponse.Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);

        Assert.Equal(created!.ExpenseId, replayed!.ExpenseId);
        Assert.Equal(created.ExpenseNumber, replayed.ExpenseNumber);

        using var list = Scoped(HttpMethod.Get, $"{Expenses}?page=1", org);
        using var listResponse = await client.SendAsync(list);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosExpenseDto>>(JsonOptions);
        Assert.Single(page!.Items);
    }

    [Fact]
    public async Task Inactive_category_rejects_new_expense()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var category = await CreateCategoryAsync(client, org, "Travel");

        using var deactivate = Scoped(HttpMethod.Post, $"{Categories}/{category.CategoryId:D}/deactivate", org);
        (await client.SendAsync(deactivate)).EnsureSuccessStatusCode();

        using var record = Scoped(HttpMethod.Post, Expenses, org);
        record.Content = JsonContent.Create(
            new RecordExpenseRequest(category.CategoryId, "Cash", 10m, "Trip", new DateOnly(2026, 7, 30)),
            options: JsonOptions);
        using var response = await client.SendAsync(record);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ExpenseCategoryNotAssignable, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Summary_excludes_voided_from_net_and_requires_view_capability()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var category = await CreateCategoryAsync(client, org, "Ops");

        using var keep = Scoped(HttpMethod.Post, Expenses, org);
        keep.Content = JsonContent.Create(
            new RecordExpenseRequest(category.CategoryId, "Cash", 100m, "Keep", new DateOnly(2026, 7, 30)),
            options: JsonOptions);
        var keepExpense = await (await client.SendAsync(keep)).Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);

        using var voidable = Scoped(HttpMethod.Post, Expenses, org);
        voidable.Content = JsonContent.Create(
            new RecordExpenseRequest(category.CategoryId, "Cash", 50m, "Void me", new DateOnly(2026, 7, 30)),
            options: JsonOptions);
        var toVoid = await (await client.SendAsync(voidable)).Content.ReadFromJsonAsync<PosExpenseDto>(JsonOptions);

        using var voidRequest = Scoped(HttpMethod.Post, $"{Expenses}/{toVoid!.ExpenseId:D}/void", org);
        voidRequest.Content = JsonContent.Create(new VoidExpenseRequest("Mistake"), options: JsonOptions);
        (await client.SendAsync(voidRequest)).EnsureSuccessStatusCode();

        using var summary = Scoped(HttpMethod.Get, $"{Expenses}/summary?fromDate=2026-07-01&toDate=2026-07-31", org);
        using var summaryResponse = await client.SendAsync(summary);
        summaryResponse.EnsureSuccessStatusCode();
        var dto = await summaryResponse.Content.ReadFromJsonAsync<PosExpenseSummaryDto>(JsonOptions);
        Assert.Equal(100m, dto!.NetTotal);
        Assert.Equal(100m, dto.GrossTotal);
        Assert.Equal(50m, dto.VoidedTotal);
        Assert.Equal(1, dto.RecordedCount);
        Assert.Equal(1, dto.VoidedCount);
        Assert.Contains(dto.ByCategory, c => c.CategoryId == category.CategoryId && c.TotalAmount == 100m);
        Assert.NotNull(keepExpense);

        using var denied = Scoped(
            HttpMethod.Get,
            $"{Expenses}/summary",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreInventoryView);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    }

    [Fact]
    public async Task Manage_capability_required_for_mutations_continuity_allows_view()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var denied = Scoped(
            HttpMethod.Post,
            Categories,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreExpensesView);
        denied.Content = JsonContent.Create(new CreatePosExpenseCategoryRequest("Denied"), options: JsonOptions);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        var category = await CreateCategoryAsync(client, org, "Allowed");

        using var view = Scoped(
            HttpMethod.Get,
            Categories,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: PosFeatureCodes.StoreExpensesView);
        using var viewResponse = await client.SendAsync(view);
        viewResponse.EnsureSuccessStatusCode();

        using var continuityCreate = Scoped(
            HttpMethod.Post,
            Expenses,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: PosFeatureCodes.StoreExpensesView);
        continuityCreate.Content = JsonContent.Create(
            new RecordExpenseRequest(category.CategoryId, "Cash", 5m, "Nope", new DateOnly(2026, 7, 30)),
            options: JsonOptions);
        using var continuityResponse = await client.SendAsync(continuityCreate);
        Assert.Equal(HttpStatusCode.Forbidden, continuityResponse.StatusCode);
    }

    [Fact]
    public async Task Cash_expense_with_gcash_reference_is_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var category = await CreateCategoryAsync(client, org, "Misc");

        using var record = Scoped(HttpMethod.Post, Expenses, org);
        record.Content = JsonContent.Create(
            new RecordExpenseRequest(
                category.CategoryId,
                "Cash",
                10m,
                "Bad",
                new DateOnly(2026, 7, 30),
                GCashReference: "should-fail"),
            options: JsonOptions);
        using var response = await client.SendAsync(record);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(DomainErrorCodes.InvalidExpenseGCashReference, await ReadErrorCodeAsync(response));
    }

    private static async Task<PosExpenseCategoryDto> CreateCategoryAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Categories, org);
        request.Content = JsonContent.Create(new CreatePosExpenseCategoryRequest(name), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<PosExpenseCategoryDto>(JsonOptions);
        Assert.NotNull(category);
        return category!;
    }

    private static async Task<HttpResponseMessage> PostExpenseWithIdempotencyAsync(
        HttpClient client,
        Guid org,
        RecordExpenseRequest body,
        string idempotencyKey,
        string payloadHash,
        Guid operationId)
    {
        using var request = Scoped(HttpMethod.Post, Expenses, org);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", payloadHash);
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", operationId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.ExpenseCreate);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static string ComputePayloadHash(RecordExpenseRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
