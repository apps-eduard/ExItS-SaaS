using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosInventoryApiTests(PosPostgreSqlFixture fixture)
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
    public async Task Enable_adjust_and_low_stock_work_with_org_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var product = await CreateProductAsync(client, orgA, "Sardines", "Piece", 25m, "inv-sardines-1");
        await CreateProductAsync(client, orgB, "Other Org", "Piece", 10m, "inv-other-1");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", orgA);
        enable.Content = JsonContent.Create(
            new EnableInventoryTrackingRequest(OpeningQuantity: 5m, ReorderLevel: 3m),
            options: JsonOptions);
        using var enableResponse = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var enabled = await enableResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.True(enabled!.IsTracked);
        Assert.Equal(5m, enabled.OnHandQuantity);
        Assert.Equal(3m, enabled.ReorderLevel);

        using var adjust = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", orgA);
        adjust.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 3m, "Display stock"),
            options: JsonOptions);
        using var adjustResponse = await client.SendAsync(adjust);
        Assert.Equal(HttpStatusCode.OK, adjustResponse.StatusCode);
        var adjusted = await adjustResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(2m, adjusted!.OnHandQuantity);
        Assert.True(adjusted.IsLowStock);

        using var lowStock = Scoped(HttpMethod.Get, $"{Inventory}/low-stock", orgA);
        using var lowResponse = await client.SendAsync(lowStock);
        lowResponse.EnsureSuccessStatusCode();
        var low = await lowResponse.Content.ReadFromJsonAsync<PagedResult<PosInventoryAccountDto>>(JsonOptions);
        Assert.Contains(low!.Items, i => i.ProductId == product.ProductId);

        using var crossOrg = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", orgB);
        using var crossResponse = await client.SendAsync(crossOrg);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);
    }

    [Fact]
    public async Task Manage_capability_is_required_for_mutations()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Noodles", "Piece", 12m, "inv-noodles-1");

        using var denied = Scoped(
            HttpMethod.Post,
            $"{Inventory}/{product.ProductId:D}/enable",
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StoreInventoryView);
        denied.Content = JsonContent.Create(new EnableInventoryTrackingRequest(2m), options: JsonOptions);
        using var deniedResponse = await client.SendAsync(denied);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        using var allowedView = Scoped(
            HttpMethod.Get,
            Inventory,
            org,
            status: PosSubscriptionStatuses.PastDue,
            grants: PosFeatureCodes.StoreInventoryView);
        using var viewResponse = await client.SendAsync(allowedView);
        viewResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Cash_checkout_deducts_and_void_restores_stock()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Soap", "Piece", 40m, "inv-soap-1");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(10m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 4m)],
                "Cash",
                AmountTendered: 200m),
            options: JsonOptions);
        using var checkoutResponse = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var sale = await checkoutResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);

        using var afterSale = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var afterSaleResponse = await client.SendAsync(afterSale);
        var account = await afterSaleResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(6m, account!.OnHandQuantity);

        using var voidSale = Scoped(HttpMethod.Post, $"{Sales}/{sale!.SaleId:D}/void", org);
        voidSale.Content = JsonContent.Create(new VoidSaleRequest("Wrong item"), options: JsonOptions);
        (await client.SendAsync(voidSale)).EnsureSuccessStatusCode();

        using var afterVoid = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var afterVoidResponse = await client.SendAsync(afterVoid);
        var restored = await afterVoidResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(10m, restored!.OnHandQuantity);
    }

    [Fact]
    public async Task Insufficient_stock_rejects_checkout_without_sale()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Eggs", "Piece", 8m, "inv-eggs-1");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(1m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 2m)],
                "Cash",
                AmountTendered: 20m),
            options: JsonOptions);
        using var response = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(ApplicationErrorCodes.InsufficientStock, await ReadErrorCodeAsync(response));

        using var list = Scoped(HttpMethod.Get, $"{Sales}?page=1", org);
        using var listResponse = await client.SendAsync(list);
        var sales = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosSaleDto>>(JsonOptions);
        Assert.Empty(sales!.Items);
    }

    [Fact]
    public async Task Idempotent_client_sale_id_does_not_double_deduct()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Oil", "Piece", 50m, "inv-oil-1");
        var saleId = Guid.NewGuid();

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(5m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        var body = new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(product.ProductId, 2m)],
            "Cash",
            AmountTendered: 100m,
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
        Assert.Equal(3m, account!.OnHandQuantity);
    }

    [Fact]
    public async Task Utang_checkout_deducts_and_void_restores_stock()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Coffee", "Piece", 30m, "inv-coffee-utang");

        using var customerRequest = Scoped(HttpMethod.Post, "/api/v1/pos/customers", org);
        customerRequest.Content = JsonContent.Create(
            new CreateCustomerRequest("Utang Stock Customer", null, null, null),
            options: JsonOptions);
        using var customerResponse = await client.SendAsync(customerRequest);
        customerResponse.EnsureSuccessStatusCode();
        var customer = await customerResponse.Content.ReadFromJsonAsync<POSCustomerDto>(JsonOptions);

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(8m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        await PosShiftIntegrationSupport.EnsureOpenShiftAsync(client, org, Actor);
        using var checkout = Scoped(HttpMethod.Post, Sales, org);
        checkout.Content = JsonContent.Create(
            new CheckoutSaleRequest(
                [new CheckoutSaleLineRequest(product.ProductId, 3m)],
                PosSaleOptions.UtangPaymentMethod,
                CustomerId: customer!.CustomerId,
                CreditEntryId: Guid.NewGuid()),
            options: JsonOptions);
        using var checkoutResponse = await client.SendAsync(checkout);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var sale = await checkoutResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);

        using var afterSale = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var afterSaleResponse = await client.SendAsync(afterSale);
        var account = await afterSaleResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(5m, account!.OnHandQuantity);

        using var voidSale = Scoped(HttpMethod.Post, $"{Sales}/{sale!.SaleId:D}/void", org);
        voidSale.Content = JsonContent.Create(new VoidSaleRequest("Cancel utang sale"), options: JsonOptions);
        (await client.SendAsync(voidSale)).EnsureSuccessStatusCode();

        using var afterVoid = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var afterVoidResponse = await client.SendAsync(afterVoid);
        var restored = await afterVoidResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, restored!.OnHandQuantity);
    }

    [Fact]
    public async Task Uom_change_is_blocked_after_inventory_activity()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Rice", "Kilogram", 60m, "inv-rice-uom");

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(2m), options: JsonOptions);
        (await client.SendAsync(enable)).EnsureSuccessStatusCode();

        using var getProduct = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var getProductResponse = await client.SendAsync(getProduct);
        getProductResponse.EnsureSuccessStatusCode();
        var latest = await getProductResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);

        using var update = Scoped(HttpMethod.Put, $"{Products}/{product.ProductId:D}", org);
        update.Content = JsonContent.Create(
            new UpdatePosCatalogProductRequest(
                latest!.Name,
                "Piece",
                latest.SellingPrice,
                latest.Description,
                latest.Sku,
                latest.Barcode,
                latest.CategoryId,
                latest.UpdatedAtUtc),
            options: JsonOptions);
        using var response = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(DomainErrorCodes.InventoryUomChangeBlocked, await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Idempotent_adjust_replays_same_stock_movement()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var product = await CreateProductAsync(client, org, "Sugar", "Piece", 15m, "inv-sugar-idem");
        var movementId = Guid.NewGuid();

        using var enable = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/enable", org);
        enable.Content = JsonContent.Create(new EnableInventoryTrackingRequest(10m), options: JsonOptions);
        using var enableResponse = await client.SendAsync(enable);
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var body = new AdjustInventoryRequest("Out", 2m, "Spill", MovementId: movementId);
        using var firstResponse = await PostAdjustWithIdempotencyAsync(client, org, product.ProductId, body);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, first!.OnHandQuantity);

        using var secondResponse = await PostAdjustWithIdempotencyAsync(client, org, product.ProductId, body);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, second!.OnHandQuantity);

        using var bodyOnly = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        bodyOnly.Content = JsonContent.Create(body, options: JsonOptions);
        using var bodyOnlyResponse = await client.SendAsync(bodyOnly);
        Assert.Equal(HttpStatusCode.OK, bodyOnlyResponse.StatusCode);
        var bodyOnlyAccount = await bodyOnlyResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, bodyOnlyAccount!.OnHandQuantity);

        using var getMovement = Scoped(HttpMethod.Get, $"{Inventory}/movements/{movementId:D}", org);
        using var getMovementResponse = await client.SendAsync(getMovement);
        Assert.Equal(HttpStatusCode.OK, getMovementResponse.StatusCode);
        var movement = await getMovementResponse.Content.ReadFromJsonAsync<PosStockMovementDto>(JsonOptions);
        Assert.Equal(movementId, movement!.MovementId);
        Assert.Equal(product.ProductId, movement.ProductId);

        using var list = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}/movements?page=1&pageSize=50", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PosStockMovementPagedResult>(JsonOptions);
        Assert.Equal(1, page!.Items.Count(m => m.MovementId == movementId));

        using var getAccount = Scoped(HttpMethod.Get, $"{Inventory}/{product.ProductId:D}", org);
        using var getAccountResponse = await client.SendAsync(getAccount);
        var account = await getAccountResponse.Content.ReadFromJsonAsync<PosInventoryAccountDto>(JsonOptions);
        Assert.Equal(8m, account!.OnHandQuantity);
    }

    private static async Task<HttpResponseMessage> PostAdjustWithIdempotencyAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        AdjustInventoryRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        using var request = Scoped(HttpMethod.Post, $"{Inventory}/{productId:D}/adjustments", org);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", body.MovementId!.Value.ToString("N"));
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", ComputePayloadHash(json));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", body.MovementId.Value.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.InventoryAdjustment);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request);
    }

    private static string ComputePayloadHash(string json)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
