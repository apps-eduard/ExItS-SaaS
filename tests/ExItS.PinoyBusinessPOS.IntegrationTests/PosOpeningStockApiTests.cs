using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosOpeningStockApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid PrimaryBranch = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Enable_with_zero_opening_does_not_require_unit_cost_or_create_movement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Zero Stock Soap", tracksExpiration: false);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 0m), options: JsonOptions);
        using var enableResponse = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var account = await enableResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.True(account!.IsTracked);
        Assert.Equal(0m, account.OnHandQuantity);

        using var movements = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/movements?page=1&pageSize=50", org);
        using var movementsResponse = await client.SendAsync(movements);
        movementsResponse.EnsureSuccessStatusCode();
        var page = await movementsResponse.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task Enable_with_opening_quantity_requires_unit_cost()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Cost Required", tracksExpiration: false);

        using var missingCost = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        missingCost.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 24m),
            options: JsonOptions);
        using var missingCostResponse = await client.SendAsync(missingCost);
        Assert.Equal(HttpStatusCode.BadRequest, missingCostResponse.StatusCode);
    }

    [Fact]
    public async Task Enable_with_opening_stock_records_movement_cost_and_on_hand()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Bath Soap", tracksExpiration: false);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 24m, UnitCost: 18m),
            options: JsonOptions);
        using var enableResponse = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var account = await enableResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(24m, account!.OnHandQuantity);

        using var movements = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/movements?page=1&pageSize=50", org);
        using var movementsResponse = await client.SendAsync(movements);
        movementsResponse.EnsureSuccessStatusCode();
        var page = await movementsResponse.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        var movement = Assert.Single(page!.Items);
        Assert.Equal("OpeningStock", movement.MovementType);
        Assert.Equal(24m, movement.QuantityEffect);
        Assert.Equal(18m, movement.UnitCost);
        Assert.Equal(432m, movement.StockValue);
    }

    [Fact]
    public async Task Enable_with_expiring_opening_stock_creates_lot_and_requires_expiry()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Expiring Soap", tracksExpiration: true);

        using var missingExpiry = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        missingExpiry.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 24m, UnitCost: 18m),
            options: JsonOptions);
        using var missingExpiryResponse = await client.SendAsync(missingExpiry);
        Assert.Equal(HttpStatusCode.BadRequest, missingExpiryResponse.StatusCode);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(
                OpeningQuantity: 24m,
                UnitCost: 18m,
                ExpirationDate: new DateOnly(2027, 12, 30),
                LotNumber: "LOT-A123"),
            options: JsonOptions);
        using var enableResponse = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        using var lots = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/lots", org);
        using var lotsResponse = await client.SendAsync(lots);
        lotsResponse.EnsureSuccessStatusCode();
        var lotPage = await lotsResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryLotDto>>(JsonOptions);
        var lot = Assert.Single(lotPage!.Items);
        Assert.Equal(24m, lot.QuantityOnHand);
        Assert.Equal(new DateOnly(2027, 12, 30), lot.ExpirationDate);
        Assert.Equal("LOT-A123", lot.LotNumber);
    }

    [Fact]
    public async Task Add_opening_stock_on_tracked_zero_product_records_movement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Deferred Opening Soap", tracksExpiration: false);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 0m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var addOpening = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/opening-stock", org);
        addOpening.Content = JsonContent.Create(
            new AddOpeningStockRequest(OpeningQuantity: 24m, UnitCost: 18m),
            options: JsonOptions);
        using var addResponse = await client.SendAsync(addOpening);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var account = await addResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.True(account!.HasOpeningStock);
        Assert.Equal(24m, account.OnHandQuantity);

        using var movements = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/movements?page=1&pageSize=50", org);
        using var movementsResponse = await client.SendAsync(movements);
        movementsResponse.EnsureSuccessStatusCode();
        var page = await movementsResponse.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        var movement = Assert.Single(page!.Items);
        Assert.Equal("OpeningStock", movement.MovementType);
        Assert.Equal(18m, movement.UnitCost);
        Assert.Equal(432m, movement.StockValue);
    }

    [Fact]
    public async Task Add_opening_stock_rejects_duplicate_opening()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Duplicate Opening", tracksExpiration: false);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 10m, UnitCost: 5m),
            options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var duplicate = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/opening-stock", org);
        duplicate.Content = JsonContent.Create(
            new AddOpeningStockRequest(OpeningQuantity: 5m, UnitCost: 5m),
            options: JsonOptions);
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Secondary_branch_can_add_opening_while_primary_already_has_opening()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var secondaryBranch = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var product = await CreateProductAsync(client, org, "Multi Branch Apple", tracksExpiration: false);

        using var enablePrimary = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org, PrimaryBranch);
        enablePrimary.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 10m, UnitCost: 50m),
            options: JsonOptions);
        (await client.SendAsync(enablePrimary)).EnsureSuccessStatusCode();

        using var getSecondary = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org, secondaryBranch);
        using var getSecondaryResponse = await client.SendAsync(getSecondary);
        getSecondaryResponse.EnsureSuccessStatusCode();
        var secondaryBefore = await getSecondaryResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.True(secondaryBefore!.IsTracked);
        Assert.Equal(0m, secondaryBefore.OnHandQuantity);
        Assert.False(secondaryBefore.HasOpeningStock);

        using var addSecondary = Scoped(
            HttpMethod.Post,
            $"{Inventory}/{product.ProductId:D}/opening-stock",
            org,
            secondaryBranch);
        addSecondary.Content = JsonContent.Create(
            new AddOpeningStockRequest(OpeningQuantity: 5m, UnitCost: 55m),
            options: JsonOptions);
        using var addSecondaryResponse = await client.SendAsync(addSecondary);
        Assert.Equal(HttpStatusCode.OK, addSecondaryResponse.StatusCode);
        var secondaryAfter = await addSecondaryResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.True(secondaryAfter!.HasOpeningStock);
        Assert.Equal(5m, secondaryAfter.OnHandQuantity);

        using var getPrimary = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org, PrimaryBranch);
        using var getPrimaryResponse = await client.SendAsync(getPrimary);
        getPrimaryResponse.EnsureSuccessStatusCode();
        var primaryAfter = await getPrimaryResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(10m, primaryAfter!.OnHandQuantity);
        Assert.True(primaryAfter.HasOpeningStock);
        Assert.Equal(15m, primaryAfter.OrganizationOnHandQuantity);

        using var duplicateSecondary = Scoped(
            HttpMethod.Post,
            $"{Inventory}/{product.ProductId:D}/opening-stock",
            org,
            secondaryBranch);
        duplicateSecondary.Content = JsonContent.Create(
            new AddOpeningStockRequest(OpeningQuantity: 1m, UnitCost: 55m),
            options: JsonOptions);
        using var duplicateResponse = await client.SendAsync(duplicateSecondary);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_product_does_not_add_opening_stock_movement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Edit Only", tracksExpiration: false);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 0m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var getProduct = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var getProductResponse = await client.SendAsync(getProduct);
        getProductResponse.EnsureSuccessStatusCode();
        var latest = await getProductResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);

        using var update = Scoped(HttpMethod.Put, $"{Products}/{product.ProductId:D}", org);
        update.Content = JsonContent.Create(
            new UpdatePosCatalogProductRequest(
                "Edit Only Renamed",
                latest!.UnitOfMeasure,
                latest.SellingPrice,
                latest.Description,
                latest.Sku,
                latest.Barcode,
                latest.CategoryId,
                ExpectedUpdatedAtUtc: latest.UpdatedAtUtc),
            options: JsonOptions);
        using var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();

        using var movements = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/movements?page=1&pageSize=50", org);
        using var movementsResponse = await client.SendAsync(movements);
        movementsResponse.EnsureSuccessStatusCode();
        var page = await movementsResponse.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        Assert.Empty(page!.Items);
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
                28m,
                Sku: $"sku-{Guid.NewGuid():N}"[..20],
                TracksExpiration: tracksExpiration,
                ExpirationWarningDays: tracksExpiration ? 7 : null),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId, Guid? branchId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.BranchHeaderName,
            (branchId ?? PrimaryBranch).ToString("D"));
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
