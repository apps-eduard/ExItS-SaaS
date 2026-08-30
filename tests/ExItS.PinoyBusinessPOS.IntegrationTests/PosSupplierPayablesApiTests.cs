using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using static ExItS.PinoyBusinessPOS.IntegrationTests.PosInventoryOpsIntegrationSupport;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosSupplierPayablesApiTests(PosPostgreSqlFixture fixture)
{
    private const string PurchaseOrders = "/api/v1/pos/purchase-orders";
    private const string GoodsReceipts = "/api/v1/pos/goods-receipts";
    private const string DirectPurchases = "/api/v1/pos/direct-purchase-receipts";
    private const string Suppliers = "/api/v1/pos/suppliers";
    private const string Payables = "/api/v1/pos/supplier-payables";
    private const string Report = "/api/v1/pos/reports/supplier-payables";

    [Fact]
    public async Task Receive_creates_payable_partial_pay_record_payment_overpay_idempotency_and_summary()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, org, "Payable Supplier");
        var product = await CreateProductAsync(client, org, "Payable Rice");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 10m, unitCost: 5m);

        var due = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var (_, grn) = await CreateOrderedAndReceiveAsync(
            client,
            org,
            supplier.SupplierId,
            product.ProductId,
            orderedQty: 4m,
            unitCost: 25m,
            receiveQty: 4m,
            paidNow: 40m,
            dueDate: due,
            paymentMethodAtReceipt: "Cash");

        Assert.Equal(14m, await OnHandAsync(client, org, product.ProductId));

        using var list = Scoped(HttpMethod.Get, $"{Payables}?supplierId={supplier.SupplierId:D}", org);
        using var listResponse = await client.SendAsync(list);
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierPayableDto>>(JsonOptions);
        var payable = Assert.Single(page!.Items);
        Assert.Equal(100m, payable.OriginalAmount);
        Assert.Equal(40m, payable.PaidAtReceiptAmount);
        Assert.Equal(40m, payable.PaidAmount);
        Assert.Equal(60m, payable.Balance);
        Assert.Equal("PartiallyPaid", payable.Status);
        Assert.False(payable.HasPostedPayments);
        Assert.True(payable.IsOverdue);
        Assert.Equal(grn.GoodsReceiptId, payable.SourceId);
        Assert.Equal("GoodsReceipt", payable.SourceType);

        using var get = Scoped(HttpMethod.Get, $"{Payables}/{payable.PayableId:D}", org);
        using var getResponse = await client.SendAsync(get);
        getResponse.EnsureSuccessStatusCode();

        using var crossGet = Scoped(HttpMethod.Get, $"{Payables}/{payable.PayableId:D}", orgB);
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        var payBody = new RecordSupplierPayablePaymentRequest(30m, "GCash", Reference: "GC-1");
        using var pay = Scoped(HttpMethod.Post, $"{Payables}/{payable.PayableId:D}/payments", org);
        pay.Content = JsonContent.Create(payBody, options: JsonOptions);
        var hash = ComputePayloadHash(payBody);
        AttachIdempotency(pay, "spp-pay-1", hash, OfflineOperationTypes.SupplierPayablePayment);
        using var payResponse = await client.SendAsync(pay);
        Assert.Equal(HttpStatusCode.Created, payResponse.StatusCode);
        var payment = await payResponse.Content.ReadFromJsonAsync<PosSupplierPayablePaymentDto>(JsonOptions);
        Assert.Equal(30m, payment!.Amount);

        using var replay = Scoped(HttpMethod.Post, $"{Payables}/{payable.PayableId:D}/payments", org);
        replay.Content = JsonContent.Create(payBody, options: JsonOptions);
        AttachIdempotency(replay, "spp-pay-1", hash, OfflineOperationTypes.SupplierPayablePayment);
        using var replayResponse = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);

        using var overpay = Scoped(HttpMethod.Post, $"{Payables}/{payable.PayableId:D}/payments", org);
        overpay.Content = JsonContent.Create(
            new RecordSupplierPayablePaymentRequest(50m, "Cash"),
            options: JsonOptions);
        using var overpayResponse = await client.SendAsync(overpay);
        Assert.Equal(HttpStatusCode.BadRequest, overpayResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.SupplierPayableOverpayNotAllowed, await ReadErrorCodeAsync(overpayResponse));

        using var payments = Scoped(HttpMethod.Get, $"{Payables}/{payable.PayableId:D}/payments", org);
        using var paymentsResponse = await client.SendAsync(payments);
        paymentsResponse.EnsureSuccessStatusCode();
        var paymentList = await paymentsResponse.Content.ReadFromJsonAsync<List<PosSupplierPayablePaymentDto>>(JsonOptions);
        Assert.Single(paymentList!);

        using var afterPay = Scoped(HttpMethod.Get, $"{Payables}/{payable.PayableId:D}", org);
        using var afterPayResponse = await client.SendAsync(afterPay);
        var updated = await afterPayResponse.Content.ReadFromJsonAsync<PosSupplierPayableDto>(JsonOptions);
        Assert.Equal(70m, updated!.PaidAmount);
        Assert.Equal(30m, updated.Balance);
        Assert.Equal("PartiallyPaid", updated.Status);
        Assert.True(updated.HasPostedPayments);

        // Inventory / cost unchanged by supplier payment.
        Assert.Equal(14m, await OnHandAsync(client, org, product.ProductId));
        var movements = await MovementsAsync(client, org, product.ProductId);
        Assert.DoesNotContain(movements, m => m.MovementType.Contains("Payable", StringComparison.OrdinalIgnoreCase));

        using var summary = Scoped(HttpMethod.Get, $"{Suppliers}/{supplier.SupplierId:D}/payable-summary", org);
        using var summaryResponse = await client.SendAsync(summary);
        summaryResponse.EnsureSuccessStatusCode();
        var summaryDto = await summaryResponse.Content.ReadFromJsonAsync<PosSupplierPayableSummaryDto>(JsonOptions);
        Assert.Equal(30m, summaryDto!.OutstandingTotal);
        Assert.Equal(30m, summaryDto.OverdueTotal);
        Assert.Equal(1, summaryDto.OpenCount);

        using var report = Scoped(HttpMethod.Get, $"{Report}?outstandingOnly=true", org);
        using var reportResponse = await client.SendAsync(report);
        reportResponse.EnsureSuccessStatusCode();
        var rows = await reportResponse.Content.ReadFromJsonAsync<List<PosSupplierPayableReportRowDto>>(JsonOptions);
        Assert.Contains(rows!, r => r.PayableId == payable.PayableId && r.Balance == 30m);
    }

    [Fact]
    public async Task Fully_paid_at_receipt_reversal_voids_payable_but_posted_payment_blocks_reversal()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, org, "Reversal Payable Supplier");
        var product = await CreateProductAsync(client, org, "Reversal Payable Item");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 2m, unitCost: 4m);

        // Default PaidNow = full → Paid status, zero payment rows → reverse OK.
        var (_, grnPaid) = await CreateOrderedAndReceiveAsync(
            client, org, supplier.SupplierId, product.ProductId, 3m, 10m, 3m);
        using var listPaid = Scoped(HttpMethod.Get, $"{Payables}?supplierId={supplier.SupplierId:D}&status=Paid", org);
        using var listPaidResponse = await client.SendAsync(listPaid);
        var paidPage = await listPaidResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierPayableDto>>(JsonOptions);
        var paidPayable = Assert.Single(paidPage!.Items, p => p.SourceId == grnPaid.GoodsReceiptId);
        Assert.Equal(30m, paidPayable.PaidAtReceiptAmount);
        Assert.False(paidPayable.HasPostedPayments);

        using var voidPaid = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grnPaid.GoodsReceiptId:D}/void", org);
        voidPaid.Content = JsonContent.Create(new VoidGoodsReceiptRequest("Undo paid receipt"), options: JsonOptions);
        using var voidPaidResponse = await client.SendAsync(voidPaid);
        Assert.Equal(HttpStatusCode.OK, voidPaidResponse.StatusCode);

        using var getVoidedPayable = Scoped(HttpMethod.Get, $"{Payables}/{paidPayable.PayableId:D}", org);
        using var getVoidedPayableResponse = await client.SendAsync(getVoidedPayable);
        var voidedPayable = await getVoidedPayableResponse.Content.ReadFromJsonAsync<PosSupplierPayableDto>(JsonOptions);
        Assert.Equal("Voided", voidedPayable!.Status);

        // Credit receive + later payment → reverse blocked.
        var (_, grnCredit) = await CreateOrderedAndReceiveAsync(
            client, org, supplier.SupplierId, product.ProductId, 2m, 20m, 2m, paidNow: 0m);
        using var listOpen = Scoped(HttpMethod.Get, $"{Payables}?status=Open", org);
        using var listOpenResponse = await client.SendAsync(listOpen);
        var openPage = await listOpenResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierPayableDto>>(JsonOptions);
        var openPayable = Assert.Single(openPage!.Items, p => p.SourceId == grnCredit.GoodsReceiptId);
        Assert.Equal(40m, openPayable.Balance);

        using var pay = Scoped(HttpMethod.Post, $"{Payables}/{openPayable.PayableId:D}/payments", org);
        pay.Content = JsonContent.Create(
            new RecordSupplierPayablePaymentRequest(10m, "BankTransfer"),
            options: JsonOptions);
        using var payResponse = await client.SendAsync(pay);
        Assert.Equal(HttpStatusCode.Created, payResponse.StatusCode);

        var onHandBefore = await OnHandAsync(client, org, product.ProductId);
        using var voidBlocked = Scoped(HttpMethod.Post, $"{GoodsReceipts}/{grnCredit.GoodsReceiptId:D}/void", org);
        voidBlocked.Content = JsonContent.Create(new VoidGoodsReceiptRequest("Should block"), options: JsonOptions);
        using var voidBlockedResponse = await client.SendAsync(voidBlocked);
        Assert.Equal(HttpStatusCode.Conflict, voidBlockedResponse.StatusCode);
        Assert.Equal(
            DomainErrorCodes.SupplierPayableReceiptReversalBlocked,
            await ReadErrorCodeAsync(voidBlockedResponse));
        Assert.Equal(onHandBefore, await OnHandAsync(client, org, product.ProductId));
        Assert.Equal("Posted", (await GetGoodsReceiptAsync(client, org, grnCredit.GoodsReceiptId)).Status);
    }

    [Fact]
    public async Task Direct_purchase_skip_without_supplier_require_supplier_for_credit_and_void_payable()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var supplier = await CreateSupplierAsync(client, org, "Direct Payable Supplier");
        var product = await CreateProductAsync(client, org, "Direct Payable Item");
        await EnableTrackedAsync(client, org, product.ProductId, openingQuantity: 1m, unitCost: 2m);

        // Fully paid walk-in without supplier → skip payable.
        using var walkIn = Scoped(HttpMethod.Post, DirectPurchases, org);
        walkIn.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 1m, 8m)],
                SourceName: "Walk-in"),
            options: JsonOptions);
        using var walkInResponse = await client.SendAsync(walkIn);
        Assert.Equal(HttpStatusCode.Created, walkInResponse.StatusCode);

        using var emptyList = Scoped(HttpMethod.Get, Payables, org);
        using var emptyListResponse = await client.SendAsync(emptyList);
        var emptyPage = await emptyListResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierPayableDto>>(JsonOptions);
        Assert.Empty(emptyPage!.Items);

        // Credit without supplier → validation failure.
        using var creditNoSupplier = Scoped(HttpMethod.Post, DirectPurchases, org);
        creditNoSupplier.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 1m, 9m)],
                SourceName: "Unknown vendor",
                PaidNow: 0m),
            options: JsonOptions);
        using var creditNoSupplierResponse = await client.SendAsync(creditNoSupplier);
        Assert.Equal(HttpStatusCode.BadRequest, creditNoSupplierResponse.StatusCode);
        Assert.Equal(
            DomainErrorCodes.DirectPurchaseRequiresSupplierForCredit,
            await ReadErrorCodeAsync(creditNoSupplierResponse));

        // Credit with supplier → payable Open; void receipt voids payable.
        using var credit = Scoped(HttpMethod.Post, DirectPurchases, org);
        credit.Content = JsonContent.Create(
            new CreateDirectPurchaseReceiptRequest(
                DateOnly.FromDateTime(DateTime.UtcNow),
                [new CreateDirectPurchaseReceiptLineRequest(product.ProductId, 2m, 12m)],
                SupplierId: supplier.SupplierId,
                PaidNow: 0m,
                DueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))),
            options: JsonOptions);
        using var creditResponse = await client.SendAsync(credit);
        Assert.Equal(HttpStatusCode.Created, creditResponse.StatusCode);
        var receipt = await creditResponse.Content.ReadFromJsonAsync<DirectPurchaseReceiptDto>(JsonOptions);

        using var list = Scoped(HttpMethod.Get, $"{Payables}?supplierId={supplier.SupplierId:D}", org);
        using var listResponse = await client.SendAsync(list);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PosSupplierPayableDto>>(JsonOptions);
        var payable = Assert.Single(page!.Items);
        Assert.Equal("DirectPurchaseReceipt", payable.SourceType);
        Assert.Equal(24m, payable.OriginalAmount);
        Assert.Equal(0m, payable.PaidAtReceiptAmount);
        Assert.Equal("Open", payable.Status);

        using var voidReceipt = Scoped(HttpMethod.Post, $"{DirectPurchases}/{receipt!.DirectPurchaseReceiptId:D}/void", org);
        voidReceipt.Content = JsonContent.Create(new VoidDirectPurchaseReceiptRequest("Undo credit buy"), options: JsonOptions);
        using var voidReceiptResponse = await client.SendAsync(voidReceipt);
        Assert.Equal(HttpStatusCode.OK, voidReceiptResponse.StatusCode);

        using var getPayable = Scoped(HttpMethod.Get, $"{Payables}/{payable.PayableId:D}", org);
        using var getPayableResponse = await client.SendAsync(getPayable);
        var voided = await getPayableResponse.Content.ReadFromJsonAsync<PosSupplierPayableDto>(JsonOptions);
        Assert.Equal("Voided", voided!.Status);
    }

    private static async Task<(Guid PurchaseOrderId, PosGoodsReceiptDto Receipt)> CreateOrderedAndReceiveAsync(
        HttpClient client,
        Guid org,
        Guid supplierId,
        Guid productId,
        decimal orderedQty,
        decimal unitCost,
        decimal receiveQty,
        decimal? paidNow = null,
        DateOnly? dueDate = null,
        string? paymentMethodAtReceipt = null)
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
            grnId,
            PaidNow: paidNow,
            DueDate: dueDate,
            PaymentMethodAtReceipt: paymentMethodAtReceipt);
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

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
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
