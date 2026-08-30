using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosReportingApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Sales = "/api/v1/pos/sales";
    private const string Categories = "/api/v1/pos/expense-categories";
    private const string Expenses = "/api/v1/pos/expenses";
    private const string Dashboard = "/api/v1/pos/dashboard";
    private const string SalesReport = "/api/v1/pos/reports/sales";
    private const string ExpensesReport = "/api/v1/pos/reports/expenses";
    private const string UtangReport = "/api/v1/pos/reports/utang";
    private const string InventoryReport = "/api/v1/pos/reports/inventory";

    [Fact]
    public async Task Dashboard_and_reports_reconcile_sales_expenses_and_empty_periods()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var day = today.ToString("yyyy-MM-dd");

        var product = await CreateProductAsync(client, orgA, "Bigas", "Kilogram", 50m, "rice-rpt");
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, orgA, Actor);
        using var saleReq = Scoped(HttpMethod.Post, Sales, orgA);
        saleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                new List<CheckoutSaleLineRequest> { new(product.ProductId, 2m) },
                "Cash",
                AmountTendered: 100m),
            options: JsonOptions);
        using var saleResp = await client.SendAsync(saleReq);
        Assert.Equal(HttpStatusCode.Created, saleResp.StatusCode);
        var sale = await saleResp.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.NotNull(sale);

        using var voidReq = Scoped(HttpMethod.Post, $"{Sales}/{sale!.SaleId:D}/void", orgA);
        voidReq.Content = JsonContent.Create(new VoidSaleRequest("Wrong tender"), options: JsonOptions);
        using var voidResp = await client.SendAsync(voidReq);
        Assert.Equal(HttpStatusCode.OK, voidResp.StatusCode);

        using var sale2Req = Scoped(HttpMethod.Post, Sales, orgA);
        sale2Req.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                new List<CheckoutSaleLineRequest> { new(product.ProductId, 1m) },
                "Cash",
                AmountTendered: 50m),
            options: JsonOptions);
        using var sale2Resp = await client.SendAsync(sale2Req);
        Assert.Equal(HttpStatusCode.Created, sale2Resp.StatusCode);

        var expenseCategory = await CreateExpenseCategoryAsync(client, orgA, "Utilities");
        using var expenseReq = Scoped(HttpMethod.Post, Expenses, orgA);
        expenseReq.Content = JsonContent.Create(
            new RecordExpenseRequest(expenseCategory.CategoryId, "Cash", 25m, "Water bill", today),
            options: JsonOptions);
        using var expenseResp = await client.SendAsync(expenseReq);
        Assert.Equal(HttpStatusCode.Created, expenseResp.StatusCode);

        using var dash = Scoped(HttpMethod.Get, $"{Dashboard}?fromDate={day}&toDate={day}", orgA);
        using var dashResp = await client.SendAsync(dash);
        dashResp.EnsureSuccessStatusCode();
        var dashboard = await dashResp.Content.ReadFromJsonAsync<PosDashboardDto>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(50m, dashboard!.CompletedSalesTotal);
        Assert.Equal(1, dashboard.CompletedSaleCount);
        Assert.Equal(1, dashboard.VoidedSaleCount);
        Assert.Equal(25m, dashboard.RecordedExpenseTotal);
        Assert.Equal(0, dashboard.VoidedExpenseCount);
        Assert.Equal(50m, dashboard.CashSalesTotal);
        Assert.Equal(0m, dashboard.ManualGCashSalesTotal);
        Assert.Equal(0m, dashboard.UtangSalesTotal);
        Assert.NotNull(dashboard.SalesTotalComparison);

        using var salesReport = Scoped(HttpMethod.Get, $"{SalesReport}?fromDate={day}&toDate={day}", orgA);
        using var salesResp = await client.SendAsync(salesReport);
        salesResp.EnsureSuccessStatusCode();
        var salesDto = await salesResp.Content.ReadFromJsonAsync<PosSalesReportDto>(JsonOptions);
        Assert.Equal(50m, salesDto!.CompletedSalesTotal);
        Assert.Equal(100m, salesDto.VoidedSalesTotal);
        Assert.Contains(salesDto.ByProduct, p => p.ProductId == product.ProductId && p.Quantity == 1m);

        using var expensesReport = Scoped(HttpMethod.Get, $"{ExpensesReport}?fromDate={day}&toDate={day}", orgA);
        using var expensesResp = await client.SendAsync(expensesReport);
        expensesResp.EnsureSuccessStatusCode();
        var expensesDto = await expensesResp.Content.ReadFromJsonAsync<PosExpensesReportDto>(JsonOptions);
        Assert.Equal(25m, expensesDto!.ActiveExpenseTotal);
        Assert.Equal(1, expensesDto.Details.Count);

        using var empty = Scoped(
            HttpMethod.Get,
            $"{Dashboard}?fromDate=2020-01-01&toDate=2020-01-01",
            orgA);
        using var emptyResp = await client.SendAsync(empty);
        emptyResp.EnsureSuccessStatusCode();
        var emptyDto = await emptyResp.Content.ReadFromJsonAsync<PosDashboardDto>(JsonOptions);
        Assert.Equal(0m, emptyDto!.CompletedSalesTotal);
        Assert.Equal(0, emptyDto.CompletedSaleCount);
        Assert.Empty(emptyDto.PaymentMethodBreakdown);

        using var cross = Scoped(HttpMethod.Get, $"{Dashboard}?fromDate={day}&toDate={day}", orgB);
        using var crossResp = await client.SendAsync(cross);
        crossResp.EnsureSuccessStatusCode();
        var crossDto = await crossResp.Content.ReadFromJsonAsync<PosDashboardDto>(JsonOptions);
        Assert.Equal(0m, crossDto!.CompletedSalesTotal);

        using var utang = Scoped(HttpMethod.Get, $"{UtangReport}?fromDate={day}&toDate={day}", orgA);
        using var utangResp = await client.SendAsync(utang);
        utangResp.EnsureSuccessStatusCode();
        var utangDto = await utangResp.Content.ReadFromJsonAsync<PosUtangReportDto>(JsonOptions);
        Assert.Equal(0m, utangDto!.ActiveCustomerOutstanding);

        using var inventory = Scoped(HttpMethod.Get, $"{InventoryReport}?fromDate={day}&toDate={day}", orgA);
        using var inventoryResp = await client.SendAsync(inventory);
        inventoryResp.EnsureSuccessStatusCode();
        Assert.NotNull(await inventoryResp.Content.ReadFromJsonAsync<PosInventoryReportDto>(JsonOptions));
    }

    [Fact]
    public async Task Dashboard_requires_feature_grant_and_rejects_oversized_range()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var denied = Scoped(
            HttpMethod.Get,
            $"{Dashboard}?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            PosSubscriptionStatuses.Active,
            PosFeatureCodes.StoreSalesView);
        using var deniedResp = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResp.StatusCode);

        using var continuity = Scoped(
            HttpMethod.Get,
            $"{SalesReport}?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            PosSubscriptionStatuses.PastDue,
            PosFeatureCodes.StoreReportsView);
        using var continuityResp = await client.SendAsync(continuity);
        continuityResp.EnsureSuccessStatusCode();

        using var suspended = Scoped(
            HttpMethod.Get,
            $"{SalesReport}?fromDate=2026-07-01&toDate=2026-07-01",
            org,
            PosSubscriptionStatuses.Suspended,
            PosFeatureCodes.StoreReportsView);
        using var suspendedResp = await client.SendAsync(suspended);
        Assert.Equal(HttpStatusCode.Forbidden, suspendedResp.StatusCode);

        using var tooLarge = Scoped(
            HttpMethod.Get,
            $"{Dashboard}?fromDate=2025-01-01&toDate=2026-01-03",
            org);
        using var tooLargeResp = await client.SendAsync(tooLarge);
        Assert.Equal(HttpStatusCode.BadRequest, tooLargeResp.StatusCode);
        var problem = await tooLargeResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(
            ApplicationErrorCodes.ReportRangeTooLarge,
            problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Operational_sales_summary_allows_ReportingUser_and_inventory_status_allows_InventoryStaff()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var owner = Actor;
        var reporter = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var inventoryStaff = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");

        // Bootstrap Owner on first authenticated request.
        using var bootstrap = ScopedActor(HttpMethod.Get, Dashboard, org, owner);
        using var bootstrapResp = await client.SendAsync(bootstrap);
        Assert.True(bootstrapResp.IsSuccessStatusCode, await bootstrapResp.Content.ReadAsStringAsync());

        await AssignRoleAsync(client, org, owner, reporter, "ReportingUser");
        await AssignRoleAsync(client, org, owner, inventoryStaff, "InventoryStaff");

        using var sales = ScopedActor(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales-summary",
            org,
            reporter);
        using var salesResp = await client.SendAsync(sales);
        Assert.Equal(HttpStatusCode.OK, salesResp.StatusCode);

        using var inv = ScopedActor(
            HttpMethod.Get,
            "/api/v1/pos/reports/inventory-status",
            org,
            inventoryStaff);
        using var invResp = await client.SendAsync(inv);
        Assert.Equal(HttpStatusCode.OK, invResp.StatusCode);

        using var salesDeniedForInv = ScopedActor(
            HttpMethod.Get,
            "/api/v1/pos/reports/sales-summary",
            org,
            inventoryStaff);
        using var salesDeniedResp = await client.SendAsync(salesDeniedForInv);
        Assert.Equal(HttpStatusCode.Forbidden, salesDeniedResp.StatusCode);
    }

    private static async Task AssignRoleAsync(
        HttpClient client,
        Guid org,
        Guid ownerActor,
        Guid targetActor,
        string role)
    {
        using var assign = ScopedActor(HttpMethod.Post, "/api/v1/pos/permissions/assignments", org, ownerActor);
        assign.Content = JsonContent.Create(new AssignPosRoleRequest(targetActor, role), options: JsonOptions);
        using var response = await client.SendAsync(assign);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static HttpRequestMessage ScopedActor(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid actorId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            actorId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.SubscriptionStatusHeaderName,
            PosSubscriptionStatuses.Active);
        // Operational reports require plan entitlement store-advanced-reports (Growth).
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.FeatureGrantsHeaderName,
            string.Join(
                ',',
                UtangCapabilityPolicy.DefaultDevelopmentGrants.Concat(
                    [PosFeatureCodes.StoreAdvancedReports, PosFeatureCodes.StoreExport])));
        return request;
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

    private static async Task<PosExpenseCategoryDto> CreateExpenseCategoryAsync(
        HttpClient client,
        Guid org,
        string name)
    {
        using var request = Scoped(HttpMethod.Post, Categories, org);
        request.Content = JsonContent.Create(new CreatePosExpenseCategoryRequest(name), options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<PosExpenseCategoryDto>(JsonOptions);
        Assert.NotNull(category);
        return category!;
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
