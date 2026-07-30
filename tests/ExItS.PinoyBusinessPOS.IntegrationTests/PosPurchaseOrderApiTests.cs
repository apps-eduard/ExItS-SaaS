using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosPurchaseOrderApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private const string PurchaseOrders = "/api/v1/pos/purchase-orders";
    private const string Suppliers = "/api/v1/pos/suppliers";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Purchase_order_lifecycle_submit_partial_receive_and_deny_over_receipt()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Acme Wholesale"));
        var product = await CreateProductAsync(client, org, "Bigas", "Kilogram", 62m, sku: "po-rice-1");

        using var create = Scoped(HttpMethod.Post, PurchaseOrders, org);
        create.Content = JsonContent.Create(
            new CreatePurchaseOrderRequest(
                supplier.SupplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreatePurchaseOrderLineRequest(product.ProductId, 10m, 50m)],
                ExpectedDeliveryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                SupplierReference: "REF-001"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var draft = await createResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.NotNull(draft);
        Assert.Equal("Draft", draft!.Status);
        Assert.Null(draft.PoNumber);

        using var submit = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}/submit", org);
        AddSubmitIdempotencyHeaders(submit, draft.PurchaseOrderId);
        using var submitResponse = await client.SendAsync(submit);
        submitResponse.EnsureSuccessStatusCode();
        var ordered = await submitResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.NotNull(ordered);
        Assert.Equal("Ordered", ordered!.Status);
        Assert.StartsWith("PO-", ordered.PoNumber, StringComparison.Ordinal);
        Assert.Equal("Bigas", ordered.Lines.Single().NameSnapshot);
        Assert.Equal("Kilogram", ordered.Lines.Single().UomSnapshot);

        var grnId = Guid.NewGuid();
        var partialBody = new ReceivePurchaseOrderRequest(
            [new ReceivePurchaseOrderLineRequest(product.ProductId, 4m)],
            grnId);
        using var partial = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}/receive", org);
        partial.Content = JsonContent.Create(partialBody, options: JsonOptions);
        AddReceiveIdempotencyHeaders(partial, grnId, partialBody);
        using var partialResponse = await client.SendAsync(partial);
        Assert.Equal(HttpStatusCode.Created, partialResponse.StatusCode);
        var grn1 = await partialResponse.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions);
        Assert.NotNull(grn1);
        Assert.StartsWith("GRN-", grn1!.GrnNumber, StringComparison.Ordinal);

        using var getPo = Scoped(HttpMethod.Get, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}", org);
        using var getPoResponse = await client.SendAsync(getPo);
        var partialPo = await getPoResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.Equal("PartiallyReceived", partialPo!.Status);
        Assert.Equal(6m, partialPo.Lines.Single().OutstandingQty);

        using var over = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}/receive", org);
        over.Content = JsonContent.Create(
            new ReceivePurchaseOrderRequest([new ReceivePurchaseOrderLineRequest(product.ProductId, 7m)]),
            options: JsonOptions);
        using var overResponse = await client.SendAsync(over);
        Assert.Equal(HttpStatusCode.BadRequest, overResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.PurchaseOverReceipt, await ReadErrorCodeAsync(overResponse));

        var grn2Id = Guid.NewGuid();
        using var complete = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}/receive", org);
        complete.Content = JsonContent.Create(
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(product.ProductId, 6m)],
                grn2Id),
            options: JsonOptions);
        using var completeResponse = await client.SendAsync(complete);
        completeResponse.EnsureSuccessStatusCode();

        using var finalGet = Scoped(HttpMethod.Get, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}", org);
        using var finalResponse = await client.SendAsync(finalGet);
        var received = await finalResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.Equal("Received", received!.Status);
        Assert.Equal(0m, received.Lines.Single().OutstandingQty);
    }

    [Fact]
    public async Task Draft_can_be_cancelled_and_duplicate_line_products_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var supplier = await CreateSupplierAsync(client, org, new CreateSupplierRequest("Beta Supply"));
        var product = await CreateProductAsync(client, org, "Tinapa", "Piece", 20m, sku: "po-fish-1");

        using var dup = Scoped(HttpMethod.Post, PurchaseOrders, org);
        dup.Content = JsonContent.Create(
            new CreatePurchaseOrderRequest(
                supplier.SupplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                [
                    new CreatePurchaseOrderLineRequest(product.ProductId, 1m, 10m),
                    new CreatePurchaseOrderLineRequest(product.ProductId, 2m, 10m)
                ]),
            options: JsonOptions);
        using var dupResponse = await client.SendAsync(dup);
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.PurchaseOrderDuplicateProduct, await ReadErrorCodeAsync(dupResponse));

        using var create = Scoped(HttpMethod.Post, PurchaseOrders, org);
        create.Content = JsonContent.Create(
            new CreatePurchaseOrderRequest(
                supplier.SupplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreatePurchaseOrderLineRequest(product.ProductId, 5m, 12m)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        var draft = await createResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);

        using var cancel = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft!.PurchaseOrderId:D}/cancel", org);
        using var cancelResponse = await client.SendAsync(cancel);
        cancelResponse.EnsureSuccessStatusCode();
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    private static async Task<PosSupplierDto> CreateSupplierAsync(
        HttpClient client,
        Guid org,
        CreateSupplierRequest body)
    {
        using var request = Scoped(HttpMethod.Post, Suppliers, org);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions))!;
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
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static void AddSubmitIdempotencyHeaders(HttpRequestMessage request, Guid purchaseOrderId)
    {
        const string body = "{}";
        request.Headers.TryAddWithoutValidation("Idempotency-Key", purchaseOrderId.ToString("N"));
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", ComputePayloadHash(body));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", purchaseOrderId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.PurchaseOrderSubmit);
    }

    private static void AddReceiveIdempotencyHeaders(
        HttpRequestMessage request,
        Guid goodsReceiptId,
        ReceivePurchaseOrderRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", goodsReceiptId.ToString("N"));
        request.Headers.TryAddWithoutValidation("X-Pos-Payload-Hash", ComputePayloadHash(json));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", goodsReceiptId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", OfflineOperationTypes.PurchaseOrderReceive);
    }

    private static string ComputePayloadHash(string json)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
