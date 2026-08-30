using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosGoodsReceiptReversalApiTests(PosPostgreSqlFixture fixture)
{
    private const string PurchaseOrders = "/api/v1/pos/purchase-orders";
    private const string GoodsReceipts = "/api/v1/pos/goods-receipts";
    private const string DirectPurchases = "/api/v1/pos/direct-purchase-receipts";
    private const string Suppliers = "/api/v1/pos/suppliers";

    [Fact]
    public async Task Void_goods_receipt_restores_stock_preserves_original_cost_and_reopens_po()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, org, "Reversal Supplier");
        var product = await CreateProductAsync(client, org, "Reversal Rice");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 5m, unitCost: 10m);

        var (poId, grn) = await CreateOrderedAndReceiveAsync(
            client,
            org,
            supplier.SupplierId,
            product.ProductId,
            orderedQty: 8m,
            unitCost: 42m,
            receiveQty: 8m);

        Assert.Equal(13m, await OnHandAsync(client, org, product.ProductId));
        Assert.Equal("Posted", grn.Status);

        // Later acquisition cost change must not affect reversal unit cost.
        using var costSeed = Scoped(HttpMethod.Post, DirectPurchases, org);
        costSeed.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 1m, 99m)]),
            options: JsonOptions);
        using var costSeedResponse = await client.SendAsync(costSeed);
        Assert.Equal(HttpStatusCode.Created, costSeedResponse.StatusCode);
        Assert.Equal(14m, await OnHandAsync(client, org, product.ProductId));

        var voidBody = new VoidGoodsReceiptRequest("Wrong delivery — full void");
        using var voidRequest = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", org);
        voidRequest.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidRequest);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var voided = await voidResponse.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions);
        Assert.NotNull(voided);
        Assert.Equal("Voided", voided!.Status);
        Assert.NotNull(voided.VoidedAtUtc);
        Assert.Equal(OwnerActor, voided.VoidedByUserId);
        Assert.Equal("Wrong delivery — full void", voided.VoidReason);

        Assert.Equal(6m, await OnHandAsync(client, org, product.ProductId)); // 14 - 8

        using var getGrn = Scoped(HttpMethod.Get, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}", org);
        using var getGrnResponse = await client.SendAsync(getGrn);
        getGrnResponse.EnsureSuccessStatusCode();
        var stillThere = await getGrnResponse.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions);
        Assert.Equal("Voided", stillThere!.Status);
        Assert.Equal(grn.GoodsReceiptId, stillThere.GoodsReceiptId);

        var movements = await MovementsAsync(client, org, product.ProductId);
        var reversal = Assert.Single(movements, m => m.MovementType == "PurchaseReceiptReversal");
        Assert.Equal(-8m, reversal.QuantityEffect);
        Assert.Equal(42m, reversal.UnitCost);
        Assert.Contains(movements, m => m.MovementType == "PurchaseReceipt");

        using var getPo = Scoped(HttpMethod.Get, $"{PurchaseOrders}/{poId:D}", org);
        using var getPoResponse = await client.SendAsync(getPo);
        getPoResponse.EnsureSuccessStatusCode();
        var po = await getPoResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);
        Assert.Equal("Ordered", po!.Status);
        Assert.Equal(0m, po.Lines.Single().ReceivedQty);
        Assert.Equal(8m, po.Lines.Single().OutstandingQty);
    }

    [Fact]
    public async Task Void_goods_receipt_idempotent_cross_org_unauthorized_and_insufficient()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, orgA, "Guard Supplier");
        var product = await CreateProductAsync(client, orgA, "Guard Item");
        await EnableTrackedAsync(client, orgA, product.ProductId, openingQuantity: 2m, unitCost: 5m);

        var (_, grn) = await CreateOrderedAndReceiveAsync(
            client,
            orgA,
            supplier.SupplierId,
            product.ProductId,
            orderedQty: 5m,
            unitCost: 7m,
            receiveQty: 5m);
        Assert.Equal(7m, await OnHandAsync(client, orgA, product.ProductId));

        // Consume most stock so void cannot fully reverse.
        using var use = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", orgA);
        use.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 4m, "Consume before void"),
            options: JsonOptions);
        using var useResponse = await client.SendAsync(use);
        Assert.Equal(HttpStatusCode.OK, useResponse.StatusCode);
        Assert.Equal(3m, await OnHandAsync(client, orgA, product.ProductId));

        var voidBody = new VoidGoodsReceiptRequest("Attempt insufficient void");
        using var insufficient = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", orgA);
        insufficient.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var insufficientResponse = await client.SendAsync(insufficient);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal(3m, await OnHandAsync(client, orgA, product.ProductId));
        Assert.Equal("Posted", (await GetGoodsReceiptAsync(client, orgA, grn.GoodsReceiptId)).Status);

        // Restore stock for successful void path.
        using var restore = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", orgA);
        restore.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", 4m, "Restore for void"),
            options: JsonOptions);
        using var restoreResponse = await client.SendAsync(restore);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        using var cross = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", orgB);
        cross.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var crossResponse = await client.SendAsync(cross);
        Assert.Equal(HttpStatusCode.NotFound, crossResponse.StatusCode);

        using var viewOnly = Scoped(
            HttpMethod.Post,
            $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void",
            orgA,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.StorePurchasingView);
        viewOnly.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var viewOnlyResponse = await client.SendAsync(viewOnly);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);

        var hash = ComputePayloadHash(voidBody);
        using var first = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", orgA);
        first.Content = JsonContent.Create(voidBody, options: JsonOptions);
        AttachIdempotency(first, "grn-void-1", hash, OfflineOperationTypes.GoodsReceiptVoid);
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var firstDto = await firstResponse.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions);
        Assert.Equal("Voided", firstDto!.Status);
        var onHandAfter = await OnHandAsync(client, orgA, product.ProductId);

        using var second = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", orgA);
        second.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(onHandAfter, await OnHandAsync(client, orgA, product.ProductId));
        Assert.Equal(
            1,
            (await MovementsAsync(client, orgA, product.ProductId))
                .Count(m => m.MovementType == "PurchaseReceiptReversal"));

        using var replay = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grn.GoodsReceiptId:D}/void", orgA);
        replay.Content = JsonContent.Create(voidBody, options: JsonOptions);
        AttachIdempotency(replay, "grn-void-1", hash, OfflineOperationTypes.GoodsReceiptVoid);
        using var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal(onHandAfter, await OnHandAsync(client, orgA, product.ProductId));
    }

    [Fact]
    public async Task Void_direct_purchase_happy_path_and_insufficient()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, "Direct Void Item");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 1m, unitCost: 3m);

        using var create = Scoped(HttpMethod.Post, DirectPurchases, org);
        create.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 4m, 11.5m)],
                SourceName: "Walk-in vendor"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var receipt = await createResponse.Content.ReadFromJsonAsync<DirectPurchaseReceiptDto>(JsonOptions);
        Assert.Equal("Posted", receipt!.Status);
        Assert.Equal(5m, await OnHandAsync(client, org, product.ProductId));

        using var consume = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        consume.Content = JsonContent.Create(
            new AdjustInventoryRequest("Out", 3m, "Consume before insufficient void"),
            options: JsonOptions);
        using var consumeResponse = await client.SendAsync(consume);
        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);

        var voidBody = new VoidDirectPurchaseReceiptRequest("Mistake");
        using var insufficient = Scoped(HttpMethod.Post, $"{DirectPurchases}/{receipt.DirectPurchaseReceiptId:D}/void", org);
        insufficient.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var insufficientResponse = await client.SendAsync(insufficient);
        Assert.Equal(HttpStatusCode.Conflict, insufficientResponse.StatusCode);
        Assert.Equal(2m, await OnHandAsync(client, org, product.ProductId));

        using var restore = Scoped(HttpMethod.Post, $"{Inventory}/{product.ProductId:D}/adjustments", org);
        restore.Content = JsonContent.Create(
            new AdjustInventoryRequest("In", 3m, "Restore for void"),
            options: JsonOptions);
        using var restoreResponse = await client.SendAsync(restore);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        using var voidOk = Scoped(HttpMethod.Post, $"{DirectPurchases}/{receipt.DirectPurchaseReceiptId:D}/void", org);
        voidOk.Content = JsonContent.Create(voidBody, options: JsonOptions);
        using var voidResponse = await client.SendAsync(voidOk);
        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        var voided = await voidResponse.Content.ReadFromJsonAsync<DirectPurchaseReceiptDto>(JsonOptions);
        Assert.Equal("Voided", voided!.Status);
        Assert.Equal(1m, await OnHandAsync(client, org, product.ProductId));

        var reversal = Assert.Single(
            await MovementsAsync(client, org, product.ProductId),
            m => m.MovementType == "DirectPurchaseReceiptReversal");
        Assert.Equal(-4m, reversal.QuantityEffect);
        Assert.Equal(11.5m, reversal.UnitCost);
    }

    private static async Task<(Guid PurchaseOrderId, PosGoodsReceiptDto Receipt)> CreateOrderedAndReceiveAsync(
        HttpClient client,
        Guid org,
        Guid supplierId,
        Guid productId,
        decimal orderedQty,
        decimal unitCost,
        decimal receiveQty)
    {
        using var create = Scoped(HttpMethod.Post, PurchaseOrders, org);
        create.Content = JsonContent.Create(
            new CreatePurchaseOrderRequest(
                supplierId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreatePurchaseOrderLineRequest(productId, orderedQty, unitCost)]),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var draft = await createResponse.Content.ReadFromJsonAsync<PosPurchaseOrderDto>(JsonOptions);

        using var submit = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft!.PurchaseOrderId:D}/submit", org);
        AddOpIdempotency(submit, draft.PurchaseOrderId, "{}", OfflineOperationTypes.PurchaseOrderSubmit);
        using var submitResponse = await client.SendAsync(submit);
        submitResponse.EnsureSuccessStatusCode();

        var grnId = Guid.NewGuid();
        var receiveBody = new ReceivePurchaseOrderRequest(
            [new ReceivePurchaseOrderLineRequest(productId, receiveQty)],
            grnId);
        using var receive = Scoped(HttpMethod.Post, $"{PurchaseOrders}/{draft.PurchaseOrderId:D}/receive", org);
        receive.Content = JsonContent.Create(receiveBody, options: JsonOptions);
        var receiveJson = JsonSerializer.Serialize(receiveBody, JsonOptions);
        AddOpIdempotency(receive, grnId, receiveJson, OfflineOperationTypes.PurchaseOrderReceive);
        using var receiveResponse = await client.SendAsync(receive);
        Assert.Equal(HttpStatusCode.Created, receiveResponse.StatusCode);
        var grn = await receiveResponse.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions);
        return (draft.PurchaseOrderId, grn!);
    }

    private static async Task<PosGoodsReceiptDto> GetGoodsReceiptAsync(HttpClient client, Guid org, Guid goodsReceiptId)
    {
        using var get = Scoped(HttpMethod.Get, $"{GoodsReceipts}/{goodsReceiptId:D}", org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosGoodsReceiptDto>(JsonOptions))!;
    }

    private static async Task<PosSupplierDto> CreateSupplierAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Suppliers, org);
        request.Content = JsonContent.Create(new CreateSupplierRequest(name), options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosSupplierDto>(JsonOptions))!;
    }

    private static void AddOpIdempotency(
        HttpRequestMessage request,
        Guid operationId,
        string payloadJson,
        string operationType)
    {
        request.Headers.TryAddWithoutValidation("Idempotency-Key", operationId.ToString("N"));
        request.Headers.TryAddWithoutValidation(
            "X-Pos-Payload-Hash",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant());
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Id", operationId.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Operation-Type", operationType);
    }

    public sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
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
