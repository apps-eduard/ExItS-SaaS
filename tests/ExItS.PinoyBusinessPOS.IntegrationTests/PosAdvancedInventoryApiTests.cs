using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosAdvancedInventoryApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string Inventory = "/api/v1/pos/inventory";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Reorder_configuration_and_suggestions_work_end_to_end()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coffee", "Piece", 45m, "adv-coffee");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 4m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var reorder = Scoped(HttpMethod.Put, $"{Inventory}/{product.ProductId:D}/reorder", org);
        reorder.Content = JsonContent.Create(
            new SetInventoryReorderRequest(10m, 25m, "Seasonal restock threshold"),
            options: JsonOptions);
        using var reorderResponse = await client.SendAsync(reorder);
        reorderResponse.EnsureSuccessStatusCode();
        var configured = await reorderResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(10m, configured!.ReorderLevel);
        Assert.Equal(25m, configured.ReorderQuantity);
        Assert.Equal(nameof(InventoryStockStatus.ReorderSuggested), configured.StockStatus);

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        adjust.Content = JsonContent.Create(new AdjustInventoryRequest("Out", 1m, "Sample"), options: JsonOptions);
        (await client.SendAsync(adjust)).EnsureSuccessStatusCode();

        using var suggestions = Scoped(HttpMethod.Get, $"{Inventory}/reorder-suggestions", org);
        using var suggestionsResponse = await client.SendAsync(suggestions);
        suggestionsResponse.EnsureSuccessStatusCode();
        var suggested = await suggestionsResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryAccountDto>>(JsonOptions);
        Assert.Contains(suggested!.Items, i => i.ProductId == product.ProductId);
    }

    [Fact]
    public async Task Stock_count_complete_posts_variance_and_reconciliation_stays_balanced()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sugar", "Kilogram", 55m, "adv-sugar");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 10m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var create = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        create.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(product.ProductId)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        createResponse.EnsureSuccessStatusCode();
        var draft = await createResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);

        using var start = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft!.StockCountId:D}/start", org);
        using var startResponse = await client.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.StartsWith("CNT-", started!.CountNumber, StringComparison.Ordinal);

        using var update = Scoped(HttpMethod.Put, $"{Inventory}/stock-counts/{draft.StockCountId:D}", org);
        update.Content = JsonContent.Create(
            new UpdateStockCountRequest([new CreateStockCountLineRequest(product.ProductId, CountedQuantity: 8m)]),
            options: JsonOptions);
        (await client.SendAsync(update)).EnsureSuccessStatusCode();

        using var complete = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft.StockCountId:D}/complete", org);
        using var completeResponse = await client.SendAsync(complete);
        completeResponse.EnsureSuccessStatusCode();
        var completed = await completeResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.Equal(nameof(StockCountStatus.Completed), completed!.Status);

        using var getAccount = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var accountResponse = await client.SendAsync(getAccount);
        accountResponse.EnsureSuccessStatusCode();
        var account = await accountResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, account!.OnHandQuantity);

        using var completeAgain = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft.StockCountId:D}/complete", org);
        using var completeAgainResponse = await client.SendAsync(completeAgain);
        completeAgainResponse.EnsureSuccessStatusCode();

        using var reconciliation = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/reconciliation", org);
        using var reconciliationResponse = await client.SendAsync(reconciliation);
        reconciliationResponse.EnsureSuccessStatusCode();
        var recon = await reconciliationResponse.Content.ReadFromJsonAsync<PosInventoryReconciliationDto>(JsonOptions);
        Assert.True(recon!.IsBalanced);
        Assert.Equal(0m, recon.Difference);
    }

    [Fact]
    public async Task Movement_filters_include_stock_count_variance()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Oil", "Liter", 120m, "adv-oil");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 5m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var create = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        create.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(product.ProductId)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        var draft = await createResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);

        using (var start = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft!.StockCountId:D}/start", org))
        {
            (await client.SendAsync(start)).EnsureSuccessStatusCode();
        }

        using (var update = Scoped(HttpMethod.Put, $"{Inventory}/stock-counts/{draft.StockCountId:D}", org))
        {
            update.Content = JsonContent.Create(
                new UpdateStockCountRequest([new CreateStockCountLineRequest(product.ProductId, CountedQuantity: 6m)]),
                options: JsonOptions);
            (await client.SendAsync(update)).EnsureSuccessStatusCode();
        }

        using (var complete = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft.StockCountId:D}/complete", org))
        {
            (await client.SendAsync(complete)).EnsureSuccessStatusCode();
        }

        using var movements = Scoped(
            HttpMethod.Get,
            $"{Inventory}/{product.ProductId:D}/movements?movementType={nameof(StockMovementType.StockCountVarianceIncrease)}",
            org);
        using var movementsResponse = await client.SendAsync(movements);
        movementsResponse.EnsureSuccessStatusCode();
        var page = await movementsResponse.Content.ReadFromJsonAsync<PagedResult<PosStockMovementDto>>(JsonOptions);
        Assert.Contains(
            page!.Items,
            m => m.MovementType == nameof(StockMovementType.StockCountVarianceIncrease));
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

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
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
