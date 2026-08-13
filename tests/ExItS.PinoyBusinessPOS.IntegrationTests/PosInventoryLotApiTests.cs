using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosInventoryLotApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string Sales = "/api/v1/pos/sales";

    [Fact]
    public async Task Expiration_tracked_product_requires_expiry_keeps_lots_separate_and_uses_fefo()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var other = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Milk 1L", tracksExpiration: true);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(0m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var missingExpiry = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        missingExpiry.Content = JsonContent.Create(new AdjustInventoryRequest("In", 20m, "Receive"), options: JsonOptions);
        using var missingResponse = await client.SendAsync(missingExpiry);
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.InventoryExpirationRequired, await ReadErrorCodeAsync(missingResponse));

        await ReceiveAsync(client, org, product.ProductId, 20m, new DateOnly(2026, 8, 20), "LOT-A");
        await ReceiveAsync(client, org, product.ProductId, 30m, new DateOnly(2026, 9, 5), "LOT-B");

        using var lotsRequest = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", org);
        using var lotsResponse = await client.SendAsync(lotsRequest);
        lotsResponse.EnsureSuccessStatusCode();
        var lots = await lotsResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        Assert.Equal(2, lots!.Items.Count);
        Assert.Equal(50m, lots.Items.Sum(l => l.QuantityOnHand));

        using var cross = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", other);
        using var crossResponse = await client.SendAsync(cross);
        var otherLots = await crossResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        Assert.Empty(otherLots!.Items);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 5m)],
                "Cash",
                AmountTendered: 500m),
            options: JsonOptions);
        using var saleResponse = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);

        using var afterLots = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", org);
        using var afterLotsResponse = await client.SendAsync(afterLots);
        var remaining = await afterLotsResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        var early = remaining!.Items.Single(l => l.ExpirationDate == new DateOnly(2026, 8, 20));
        var later = remaining.Items.Single(l => l.ExpirationDate == new DateOnly(2026, 9, 5));
        Assert.Equal(15m, early.QuantityOnHand);
        Assert.Equal(30m, later.QuantityOnHand);

        using var detail = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var detailResponse = await client.SendAsync(detail);
        var account = await detailResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(45m, account!.OnHandQuantity);
        Assert.Equal(45m, account.SellableQuantity);

        using var list = Scoped(HttpMethod.Get, Inventory, org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var listed = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryAccountDto>>(JsonOptions);
        var listRow = listed!.Items.Single(i => i.ProductId == product.ProductId);
        Assert.True(listRow.TracksExpiration);
        Assert.Null(listRow.SellableQuantity);
        Assert.Null(listRow.ExpiredQuantity);
        Assert.Null(listRow.NearExpiryQuantity);
    }

    [Fact]
    public async Task Expired_lot_is_not_sellable_and_write_off_reduces_that_lot()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Yogurt", tracksExpiration: true);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(0m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        var expiredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var validDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        await ReceiveAsync(client, org, product.ProductId, 10m, expiredDate, "EXP");
        await ReceiveAsync(client, org, product.ProductId, 5m, validDate, "OK");

        using var detail = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var detailResponse = await client.SendAsync(detail);
        var account = await detailResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(15m, account!.OnHandQuantity);
        Assert.Equal(5m, account.SellableQuantity);
        Assert.Equal(10m, account.ExpiredQuantity);

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var tooMuch = Scoped(HttpMethod.Post, Sales, org);
        tooMuch.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 10m)],
                "Cash",
                AmountTendered: 500m),
            options: JsonOptions);
        using var tooMuchResponse = await client.SendAsync(tooMuch);
        Assert.Equal(HttpStatusCode.Conflict, tooMuchResponse.StatusCode);

        using var lotsRequest = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", org);
        using var lotsResponse = await client.SendAsync(lotsRequest);
        var lots = await lotsResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        var expiredLot = lots!.Items.Single(l => l.LotNumber == "EXP");

        using var writeOff = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        writeOff.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 10m, "Expired", LotId: expiredLot.LotId),
            options: JsonOptions);
        using var writeOffResponse = await client.SendAsync(writeOff);
        Assert.Equal(HttpStatusCode.OK, writeOffResponse.StatusCode);

        using var after = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var afterResponse = await client.SendAsync(after);
        var afterAccount = await afterResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(5m, afterAccount!.OnHandQuantity);
        Assert.Equal(5m, afterAccount.SellableQuantity);
        Assert.Equal(0m, afterAccount.ExpiredQuantity);
    }

    [Fact]
    public async Task Sale_retry_does_not_double_deduct_lot()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Cheese", tracksExpiration: true);
        var saleId = Guid.NewGuid();

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(0m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
        await ReceiveAsync(client, org, product.ProductId, 8m, expiry, "LOT-R");

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 2m)],
            "Cash",
            AmountTendered: 200m,
            SaleId: saleId);
        using var first = Scoped(HttpMethod.Post, Sales, org);
        first.Content = JsonContent.Create(body, options: JsonOptions);
        (await client.SendAsync(first)).EnsureSuccessStatusCode();
        using var second = Scoped(HttpMethod.Post, Sales, org);
        second.Content = JsonContent.Create(body, options: JsonOptions);
        (await client.SendAsync(second)).EnsureSuccessStatusCode();

        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var getResponse = await client.SendAsync(get);
        var account = await getResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(6m, account!.OnHandQuantity);
        Assert.Equal(6m, account.SellableQuantity);
    }

    [Fact]
    public async Task Non_expiry_product_preserves_existing_inventory_behavior()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "USB Cable", tracksExpiration: false);
        Assert.False(product.TracksExpiration);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(4m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        adjust.Content = JsonContent.Create(new AdjustInventoryRequest("In", 2m, "More stock"), options: JsonOptions);
        (await client.SendAsync(adjust)).EnsureSuccessStatusCode();

        using var get = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var getResponse = await client.SendAsync(get);
        var account = await getResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(6m, account!.OnHandQuantity);
        Assert.Null(account.SellableQuantity);
    }

    private static async Task ReceiveAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        DateOnly expiry,
        string? lotNumber)
    {
        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org);
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
        bool tracksExpiration)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(
                name,
                "Piece",
                50m,
                Sku: $"sku-{Guid.NewGuid():N}"[..20],
                TracksExpiration: tracksExpiration),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal(tracksExpiration, product!.TracksExpiration);
        return product;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
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
