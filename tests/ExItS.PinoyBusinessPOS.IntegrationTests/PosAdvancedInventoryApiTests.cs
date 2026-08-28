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
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 4m, UnitCost: 1m), options: JsonOptions);
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
        Assert.Equal(nameof(InventoryStockStatus.LowStock), configured.StockStatus);
        Assert.True(configured.IsReorderSuggested);
        Assert.Equal(25m, configured.SuggestedOrderQuantity);

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
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 10m, UnitCost: 1m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var create = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        create.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(product.ProductId)], "Weekly count"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        createResponse.EnsureSuccessStatusCode();
        var draft = await createResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.Equal("Weekly count", draft!.Title);
        Assert.Null(draft.Notes);

        using var start = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{draft.StockCountId:D}/start", org);
        using var startResponse = await client.SendAsync(start);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.Matches(@"^CNT-\d{8}-01$", started!.CountNumber);
        Assert.Equal("Weekly count", started.Title);

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
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 5m, UnitCost: 1m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var create = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        create.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(product.ProductId)], "Weekly count"),
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

    [Fact]
    public async Task Stock_count_title_notes_and_friendly_numbers_are_server_authoritative()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var first = await CreateProductAsync(client, org, "Bread", "Piece", 10m, "cnt-bread");
        var second = await CreateProductAsync(client, org, "Water", "Piece", 15m, "cnt-water");

        using var enableFirst = Scoped(HttpMethod.Post, $"{Inventory}/{first.ProductId:D}/enable", org);
        enableFirst.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 11m, UnitCost: 1m), options: JsonOptions);
        (await client.SendAsync(enableFirst)).EnsureSuccessStatusCode();
        using var enableSecond = Scoped(HttpMethod.Post, $"{Inventory}/{second.ProductId:D}/enable", org);
        enableSecond.Content = JsonContent.Create(new EnableInventoryTrackingRequest(OpeningQuantity: 0m), options: JsonOptions);
        (await client.SendAsync(enableSecond)).EnsureSuccessStatusCode();

        using var blank = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        blank.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(first.ProductId)], "   "),
            options: JsonOptions);
        using var blankResponse = await client.SendAsync(blank);
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);

        using var createCustom = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        createCustom.Content = JsonContent.Create(
            new CreateStockCountRequest(
                [
                    new CreateStockCountLineRequest(first.ProductId),
                    new CreateStockCountLineRequest(second.ProductId)
                ],
                "Freezer inventory check",
                Notes: "Counted after Friday closing."),
            options: JsonOptions);
        using var customResponse = await client.SendAsync(createCustom);
        customResponse.EnsureSuccessStatusCode();
        var custom = await customResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        Assert.Equal("Freezer inventory check", custom!.Title);
        Assert.Equal("Counted after Friday closing.", custom.Notes);
        Assert.Equal(2, custom.Lines.Count);
        Assert.Equal(TimeSpan.Zero, custom.CreatedAtUtc.Offset);

        using var createSecond = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts", org);
        createSecond.Content = JsonContent.Create(
            new CreateStockCountRequest([new CreateStockCountLineRequest(first.ProductId)], "Monthly count"),
            options: JsonOptions);
        using var secondCreateResponse = await client.SendAsync(createSecond);
        secondCreateResponse.EnsureSuccessStatusCode();
        var secondDraft = await secondCreateResponse.Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);

        using var startFirst = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{custom.StockCountId:D}/start", org);
        using var startSecond = Scoped(HttpMethod.Post, $"{Inventory}/stock-counts/{secondDraft!.StockCountId:D}/start", org);
        var clientA = factory.CreateClient();
        var clientB = factory.CreateClient();
        var started = await Task.WhenAll(clientA.SendAsync(startFirst), clientB.SendAsync(startSecond));
        foreach (var response in started)
        {
            response.EnsureSuccessStatusCode();
        }

        var firstStarted = await started[0].Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        var secondStarted = await started[1].Content.ReadFromJsonAsync<PosStockCountDto>(JsonOptions);
        var numbers = new[] { firstStarted!.CountNumber, secondStarted!.CountNumber };
        Assert.All(numbers, n => Assert.Matches(@"^CNT-\d{8}-\d{2,}$", n));
        Assert.Equal(2, numbers.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(numbers, n => n!.EndsWith("-01", StringComparison.Ordinal));
        Assert.Contains(numbers, n => n!.EndsWith("-02", StringComparison.Ordinal));
        Assert.Equal("Freezer inventory check", firstStarted.Title);
        Assert.Equal("Counted after Friday closing.", firstStarted.Notes);
        Assert.Equal("Monthly count", secondStarted.Title);
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
