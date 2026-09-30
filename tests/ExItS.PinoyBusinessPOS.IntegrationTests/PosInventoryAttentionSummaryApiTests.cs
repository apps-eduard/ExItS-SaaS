using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// Branch-authoritative GET /api/v1/pos/inventory/attention-summary.
/// Must not leak organization-wide management/overview expiry or stock counts.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosInventoryAttentionSummaryApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Main = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Iloilo = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Warehouse = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Attention = "/api/v1/pos/inventory/attention-summary";

    [Fact]
    public async Task Empty_warehouse_does_not_count_other_branch_expiry()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var apple = await CreateProductAsync(client, org, "Apple", tracksExpiration: true);
        var milk = await CreateProductAsync(client, org, "Milk", tracksExpiration: true);
        var banana = await CreateProductAsync(client, org, "Banana", tracksExpiration: true);

        await EnableTrackedAsync(client, org, apple.ProductId, Main);
        await EnableTrackedAsync(client, org, milk.ProductId, Main);
        await EnableTrackedAsync(client, org, banana.ProductId, Iloilo);
        await EnableExpirationAsync(client, org, apple.ProductId, Main, warningDays: 7);
        await EnableExpirationAsync(client, org, milk.ProductId, Main, warningDays: 7);
        await EnableExpirationAsync(client, org, banana.ProductId, Iloilo, warningDays: 7);

        var expired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
        var near = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        await ReceiveAsync(client, org, apple.ProductId, 3m, expired, "MAIN-APPLE-EXP", Main);
        await ReceiveAsync(client, org, milk.ProductId, 4m, near, "MAIN-MILK-NEAR", Main);
        await ReceiveAsync(client, org, banana.ProductId, 2m, near, "ILO-BANANA-NEAR", Iloilo);

        // Warehouse has no physical stock.
        var warehouse = await GetAttentionAsync(client, org, Warehouse);
        Assert.Equal(0, warehouse.ExpiredLotCount);
        Assert.Equal(0, warehouse.NearExpiryLotCount);

        var main = await GetAttentionAsync(client, org, Main);
        Assert.Equal(1, main.ExpiredLotCount);
        Assert.Equal(1, main.NearExpiryLotCount);

        var iloilo = await GetAttentionAsync(client, org, Iloilo);
        Assert.Equal(0, iloilo.ExpiredLotCount);
        Assert.Equal(1, iloilo.NearExpiryLotCount);
    }

    [Fact]
    public async Task Warning_days_are_per_branch_setting()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var banana = await CreateProductAsync(client, org, "Warn Banana", tracksExpiration: true, warningDays: 7);

        await EnableTrackedAsync(client, org, banana.ProductId, Main);
        await EnableTrackedAsync(client, org, banana.ProductId, Iloilo);
        await EnableExpirationAsync(client, org, banana.ProductId, Main, warningDays: 7);
        await EnableExpirationAsync(client, org, banana.ProductId, Iloilo, warningDays: 30);

        // Day +10: near for Iloilo (30) but not Main (7).
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        await ReceiveAsync(client, org, banana.ProductId, 5m, expiry, "MAIN-W", Main);
        await ReceiveAsync(client, org, banana.ProductId, 5m, expiry, "ILO-W", Iloilo);

        var main = await GetAttentionAsync(client, org, Main);
        var iloilo = await GetAttentionAsync(client, org, Iloilo);
        Assert.Equal(0, main.NearExpiryLotCount);
        Assert.Equal(1, iloilo.NearExpiryLotCount);
    }

    [Fact]
    public async Task Expiry_off_branch_reports_zero_even_with_stock()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "No Expiry Stock", tracksExpiration: false);

        await EnableTrackedAsync(client, org, product.ProductId, Warehouse);
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, Warehouse);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", 100m, "Warehouse stock"),
            options: JsonOptions);
        (await client.SendAsync(adjust)).EnsureSuccessStatusCode();

        // Other branch has expired lot attention.
        var other = await CreateProductAsync(client, org, "Other Expiry", tracksExpiration: true);
        await EnableTrackedAsync(client, org, other.ProductId, Main);
        await EnableExpirationAsync(client, org, other.ProductId, Main, warningDays: 7);
        await ReceiveAsync(
            client,
            org,
            other.ProductId,
            3m,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            "MAIN-EXP",
            Main);

        var warehouse = await GetAttentionAsync(client, org, Warehouse);
        Assert.Equal(0, warehouse.ExpiredLotCount);
        Assert.Equal(0, warehouse.NearExpiryLotCount);
    }

    [Fact]
    public async Task Zero_qty_lots_are_not_counted()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Zero Lot", tracksExpiration: true);
        await EnableTrackedAsync(client, org, product.ProductId, Main);
        await EnableExpirationAsync(client, org, product.ProductId, Main, warningDays: 7);

        var expired = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        await ReceiveAsync(client, org, product.ProductId, 5m, expired, "ZERO-EXP", Main);

        using var lotsReq = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", org, Main);
        using var lotsRes = await client.SendAsync(lotsReq);
        var lots = await lotsRes.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        var lotId = lots!.Items.Single().LotId;

        using var outAdj = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org, Main);
        outAdj.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 5m, "Clear", LotId: lotId),
            options: JsonOptions);
        (await client.SendAsync(outAdj)).EnsureSuccessStatusCode();

        var summary = await GetAttentionAsync(client, org, Main);
        Assert.Equal(0, summary.ExpiredLotCount);
        Assert.Equal(0, summary.NearExpiryLotCount);
    }

    [Fact]
    public async Task Low_and_out_of_stock_match_branch_inventory_list_totals()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var low = await CreateProductAsync(client, org, "Low Item", tracksExpiration: false);
        var outProduct = await CreateProductAsync(client, org, "Out Item", tracksExpiration: false);

        using var enableLow = Scoped(HttpMethod.Post, $"{Inventory}/{low.ProductId:D}/enable", org, Main);
        enableLow.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 2m, UnitCost: 1m, ReorderLevel: 5m),
            options: JsonOptions);
        (await client.SendAsync(enableLow)).EnsureSuccessStatusCode();

        using var enableOut = Scoped(HttpMethod.Post, $"{Inventory}/{outProduct.ProductId:D}/enable", org, Main);
        enableOut.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 0m, ReorderLevel: 1m),
            options: JsonOptions);
        (await client.SendAsync(enableOut)).EnsureSuccessStatusCode();

        // Healthy Warehouse branch balances (avoid Primary-fallback leakage into Warehouse attention).
        using var adjLow = Scoped(HttpMethod.Post, $"{Inventory}/{low.ProductId:D}/adjustments", org, Warehouse);
        adjLow.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", 50m, "Warehouse seed"),
            options: JsonOptions);
        (await client.SendAsync(adjLow)).EnsureSuccessStatusCode();

        using var adjOut = Scoped(HttpMethod.Post, $"{Inventory}/{outProduct.ProductId:D}/adjustments", org, Warehouse);
        adjOut.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", 10m, "Warehouse seed"),
            options: JsonOptions);
        (await client.SendAsync(adjOut)).EnsureSuccessStatusCode();

        using var listLow = Scoped(HttpMethod.Get, $"{Inventory}?tracked=true&lowStock=true&page=1&pageSize=1", org, Main);
        using var listLowRes = await client.SendAsync(listLow);
        var lowPage = await listLowRes.Content.ReadFromJsonAsync<PagedResult<PosInventoryAccountDto>>(JsonOptions);

        using var listOut = Scoped(
            HttpMethod.Get,
            $"{Inventory}?tracked=true&stockStatus=OutOfStock&page=1&pageSize=1",
            org,
            Main);
        using var listOutRes = await client.SendAsync(listOut);
        var outPage = await listOutRes.Content.ReadFromJsonAsync<PagedResult<PosInventoryAccountDto>>(JsonOptions);

        var attention = await GetAttentionAsync(client, org, Main);
        Assert.Equal(lowPage!.TotalCount, attention.LowStockProductCount);
        Assert.Equal(outPage!.TotalCount, attention.OutOfStockProductCount);

        var warehouse = await GetAttentionAsync(client, org, Warehouse);
        Assert.Equal(0, warehouse.LowStockProductCount);
        Assert.Equal(0, warehouse.OutOfStockProductCount);
    }

    [Fact]
    public async Task Missing_branch_header_is_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Get, Attention);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            org.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<PosInventoryAttentionSummaryDto> GetAttentionAsync(
        HttpClient client,
        Guid org,
        Guid branchId)
    {
        using var request = Scoped(HttpMethod.Get, Attention, org, branchId);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PosInventoryAttentionSummaryDto>(JsonOptions);
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task EnableTrackedAsync(HttpClient client, Guid org, Guid productId, Guid branchId)
    {
        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/enable", org, branchId);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 0m),
            options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();
    }

    private static async Task EnableExpirationAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        Guid branchId,
        int warningDays)
    {
        using var enable = Scoped(
            HttpMethod.Post,
            $"{Inventory}/products/{productId:D}/expiration-tracking/enable",
            org,
            branchId);
        enable.Content = JsonContent.Create(
            new EnableExpirationTrackingRequest(ExpirationWarningDays: warningDays),
            options: JsonOptions);
        using var response = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task ReceiveAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        DateOnly expiry,
        string? lotNumber,
        Guid branchId)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org, branchId);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", qty, "Receive", ExpirationDate: expiry, LotNumber: lotNumber),
            options: JsonOptions);
        using var response = await client.SendAsync(adjust);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        bool tracksExpiration,
        int? warningDays = null)
    {
        using var request = Scoped(HttpMethod.Post, Products, org, Main);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                50m,
                Sku: $"sku-{Guid.NewGuid():N}"[..20],
                TracksExpiration: tracksExpiration,
                ExpirationWarningDays: tracksExpiration ? warningDays ?? 7 : null),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
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
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.BranchHeaderName,
            (branchId ?? Main).ToString("D"));
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
