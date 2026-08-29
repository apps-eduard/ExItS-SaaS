using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Sales;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosReportBranchScopeApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid BranchA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string Products = "/api/v1/pos/catalog/products";
    private const string Sales = "/api/v1/pos/sales";
    private const string SalesSummary = "/api/v1/pos/reports/sales-summary";
    private const string SalesByPayment = "/api/v1/pos/reports/sales-by-payment";
    private const string Profitability = "/api/v1/pos/reports/profitability";
    private const string ClassicSales = "/api/v1/pos/reports/sales";

    [Fact]
    public async Task Sales_summary_filters_by_branch_and_all_branches_sums()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var product = await CreateProductAsync(client, org, "Branch Scope Rice", "Kilogram", 50m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        await CheckoutAsync(client, org, product.ProductId, 2m, 100m, BranchA);
        await CheckoutAsync(client, org, product.ProductId, 1m, 50m, BranchB);

        using var allReq = Scoped(HttpMethod.Get, $"{SalesSummary}?fromDate={today}&toDate={today}", org);
        using var allResp = await client.SendAsync(allReq);
        allResp.EnsureSuccessStatusCode();
        var allDto = await allResp.Content.ReadFromJsonAsync<PosSalesSummaryReportDto>(JsonOptions);
        Assert.Equal(2, allDto!.CompletedTransactionCount);
        Assert.Equal(150m, allDto.CompletedGrossSales);

        using var aReq = Scoped(
            HttpMethod.Get,
            $"{SalesSummary}?fromDate={today}&toDate={today}&branchId={BranchA:D}",
            org);
        using var aResp = await client.SendAsync(aReq);
        aResp.EnsureSuccessStatusCode();
        var aDto = await aResp.Content.ReadFromJsonAsync<PosSalesSummaryReportDto>(JsonOptions);
        Assert.Equal(1, aDto!.CompletedTransactionCount);
        Assert.Equal(100m, aDto.CompletedGrossSales);

        using var bReq = Scoped(
            HttpMethod.Get,
            $"{SalesSummary}?fromDate={today}&toDate={today}&branchId={BranchB:D}",
            org);
        using var bResp = await client.SendAsync(bReq);
        bResp.EnsureSuccessStatusCode();
        var bDto = await bResp.Content.ReadFromJsonAsync<PosSalesSummaryReportDto>(JsonOptions);
        Assert.Equal(1, bDto!.CompletedTransactionCount);
        Assert.Equal(50m, bDto.CompletedGrossSales);

        using var emptyReq = Scoped(
            HttpMethod.Get,
            $"{SalesSummary}?fromDate={today}&toDate={today}&branchId={Guid.NewGuid():D}",
            org);
        using var emptyResp = await client.SendAsync(emptyReq);
        emptyResp.EnsureSuccessStatusCode();
        var emptyDto = await emptyResp.Content.ReadFromJsonAsync<PosSalesSummaryReportDto>(JsonOptions);
        Assert.Equal(0, emptyDto!.CompletedTransactionCount);
        Assert.Equal(0m, emptyDto.CompletedGrossSales);
    }

    [Fact]
    public async Task Sales_by_payment_and_classic_sales_honor_branchId()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var product = await CreateProductAsync(client, org, "Pay Scope", "Piece", 10m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        await CheckoutAsync(client, org, product.ProductId, 1m, 10m, BranchA);
        await CheckoutAsync(client, org, product.ProductId, 3m, 30m, BranchB);

        using var payA = Scoped(
            HttpMethod.Get,
            $"{SalesByPayment}?fromDate={today}&toDate={today}&branchId={BranchA:D}",
            org);
        using var payAResp = await client.SendAsync(payA);
        payAResp.EnsureSuccessStatusCode();
        var payADto = await payAResp.Content.ReadFromJsonAsync<PosSalesByPaymentReportDto>(JsonOptions);
        var cashA = payADto!.Rows.Single(r => r.PaymentMethod == "Cash");
        Assert.Equal(10m, cashA.GrossCompleted);

        using var salesA = Scoped(
            HttpMethod.Get,
            $"{ClassicSales}?fromDate={today}&toDate={today}&branchId={BranchA:D}",
            org);
        using var salesAResp = await client.SendAsync(salesA);
        salesAResp.EnsureSuccessStatusCode();
        var salesADto = await salesAResp.Content.ReadFromJsonAsync<PosSalesReportDto>(JsonOptions);
        Assert.Equal(10m, salesADto!.CompletedSalesTotal);
        Assert.Equal(1, salesADto.CompletedSaleCount);
    }

    [Fact]
    public async Task Invalid_branchId_fails_closed_without_org_fallback()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var product = await CreateProductAsync(client, org, "Fail Closed", "Piece", 10m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        await CheckoutAsync(client, org, product.ProductId, 1m, 10m, BranchA);

        using var emptyGuid = Scoped(
            HttpMethod.Get,
            $"{SalesSummary}?fromDate={today}&toDate={today}&branchId={Guid.Empty:D}",
            org);
        using var emptyResp = await client.SendAsync(emptyGuid);
        Assert.Equal(HttpStatusCode.BadRequest, emptyResp.StatusCode);

        using var unknown = Scoped(
            HttpMethod.Get,
            $"{SalesSummary}?fromDate={today}&toDate={today}&branchId={Guid.NewGuid():D}",
            org);
        using var unknownResp = await client.SendAsync(unknown);
        unknownResp.EnsureSuccessStatusCode();
        var dto = await unknownResp.Content.ReadFromJsonAsync<PosSalesSummaryReportDto>(JsonOptions);
        Assert.Equal(0m, dto!.CompletedGrossSales);
        Assert.NotEqual(10m, dto.CompletedGrossSales);
    }

    [Fact]
    public async Task Profitability_branch_scope_matches_sales()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var product = await CreateProductAsync(client, org, "Profit Scope", "Piece", 20m);
        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        await CheckoutAsync(client, org, product.ProductId, 1m, 20m, BranchA);
        await CheckoutAsync(client, org, product.ProductId, 2m, 40m, BranchB);

        using var aReq = Scoped(
            HttpMethod.Get,
            $"{Profitability}?fromDate={today}&toDate={today}&branchId={BranchA:D}",
            org);
        using var aResp = await client.SendAsync(aReq);
        aResp.EnsureSuccessStatusCode();
        var aDto = await aResp.Content.ReadFromJsonAsync<PosProfitabilityReportDto>(JsonOptions);
        Assert.Equal(BranchA, aDto!.BranchId);
        Assert.Equal(20m, aDto.NetSales);

        using var allReq = Scoped(HttpMethod.Get, $"{Profitability}?fromDate={today}&toDate={today}", org);
        using var allResp = await client.SendAsync(allReq);
        allResp.EnsureSuccessStatusCode();
        var allDto = await allResp.Content.ReadFromJsonAsync<PosProfitabilityReportDto>(JsonOptions);
        Assert.Null(allDto!.BranchId);
        Assert.Equal(60m, allDto.NetSales);
    }

    private static async Task CheckoutAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        decimal tendered,
        Guid branchId)
    {
        using var req = Scoped(HttpMethod.Post, Sales, org, branchId);
        req.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(productId, qty)],
                "Cash",
                AmountTendered: tendered),
            options: JsonOptions);
        using var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string uom,
        decimal price)
    {
        using var req = Scoped(HttpMethod.Post, Products, org);
        req.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, uom, price, null, $"sku-{Guid.NewGuid():N}"[..16]),
            options: JsonOptions);
        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid? branchId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.FeatureGrantsHeaderName,
            string.Join(',',
                PosFeatureCodes.StoreCatalogView,
                PosFeatureCodes.StoreCatalogManage,
                PosFeatureCodes.StoreSalesView,
                PosFeatureCodes.StoreSalesCreate,
                PosFeatureCodes.StoreDashboardView,
                PosFeatureCodes.StoreReportsView,
                PosFeatureCodes.StoreAdvancedReports,
                PosFeatureCodes.StoreShiftsView,
                PosFeatureCodes.StoreShiftsManage,
                PosFeatureCodes.StoreInventoryView));
        request.Headers.TryAddWithoutValidation(
            PosCommercialHeaders.SubscriptionStatusHeaderName,
            PosSubscriptionStatuses.Active);
        if (branchId is { } id && id != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation(
                PosOrganizationHeaders.BranchHeaderName,
                id.ToString("D"));
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
