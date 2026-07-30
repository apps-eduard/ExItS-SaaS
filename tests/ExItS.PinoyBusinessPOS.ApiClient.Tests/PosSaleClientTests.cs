using System.Net;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PosSaleClientTests
{
    private const string SalePayload =
        """
        {
          "saleId": "11111111-1111-1111-1111-111111111111",
          "organizationId": "22222222-2222-2222-2222-222222222222",
          "saleNumber": "SALE-20260730-000001",
          "status": "Completed",
          "paymentMethod": "Cash",
          "subtotal": 118.50,
          "total": 118.50,
          "amountTendered": 200.00,
          "changeAmount": 81.50,
          "gcashReference": null,
          "recordedAtUtc": "2026-07-30T04:15:00+00:00",
          "recordedBy": "33333333-3333-3333-3333-333333333333",
          "voidedAtUtc": null,
          "voidedBy": null,
          "voidReason": null,
          "updatedAtUtc": "2026-07-30T04:15:00+00:00",
          "lines": [
            {
              "saleLineId": "44444444-4444-4444-4444-444444444444",
              "productId": "55555555-5555-5555-5555-555555555555",
              "lineNumber": 1,
              "name": "Bigas",
              "sku": "rice-1",
              "barcode": null,
              "unitOfMeasure": "Kilogram",
              "unitPrice": 62.00,
              "quantity": 1.5,
              "lineTotal": 93.00
            }
          ]
        }
        """;

    [Fact]
    public async Task Checkout_posts_to_the_sales_route_and_deserializes_the_sale()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SalePayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.CheckoutAsync(new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1.5m)],
            PosSaleOptions.CashPaymentMethod,
            200m));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/v1/pos/sales", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("SALE-20260730-000001", result.Data!.SaleNumber);
        Assert.Equal(118.50m, result.Data.Total);
        Assert.Equal(81.50m, result.Data.ChangeAmount);
        Assert.Equal("Bigas", Assert.Single(result.Data.Lines).Name);
    }

    [Fact]
    public async Task Checkout_with_sale_id_sends_idempotency_headers()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SalePayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);
        var saleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await client.CheckoutAsync(new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m)],
            PosSaleOptions.CashPaymentMethod,
            50m,
            SaleId: saleId));

        Assert.True(result.IsSuccess);
        Assert.True(handler.LastRequest!.Headers.Contains("Idempotency-Key"));
        Assert.True(handler.LastRequest.Headers.Contains("X-Pos-Payload-Hash"));
        Assert.Equal(saleId.ToString("N"), handler.LastRequest.Headers.GetValues("Idempotency-Key").Single());
        Assert.Equal("sale.checkout", handler.LastRequest.Headers.GetValues("X-Pos-Operation-Type").Single());
    }

    [Fact]
    public async Task Checkout_without_sale_id_omits_idempotency_headers()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SalePayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.CheckoutAsync(new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m)],
            PosSaleOptions.CashPaymentMethod,
            50m));

        Assert.True(result.IsSuccess);
        Assert.False(handler.LastRequest!.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task List_sales_sends_every_filter_as_a_query_parameter()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"items":[],"totalCount":0,"page":2,"pageSize":10}""",
                Encoding.UTF8,
                "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.ListSalesAsync(
            status: PosSaleOptions.VoidedStatus,
            paymentMethod: PosSaleOptions.ManualGCashPaymentMethod,
            fromDateUtc: new DateOnly(2026, 7, 1),
            toDateUtc: new DateOnly(2026, 7, 30),
            saleNumber: "SALE-20260730-000001",
            page: 2,
            pageSize: 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Page);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("page=2", query, StringComparison.Ordinal);
        Assert.Contains("pageSize=10", query, StringComparison.Ordinal);
        Assert.Contains("status=Voided", query, StringComparison.Ordinal);
        Assert.Contains("paymentMethod=ManualGCash", query, StringComparison.Ordinal);
        Assert.Contains("fromDate=2026-07-01", query, StringComparison.Ordinal);
        Assert.Contains("toDate=2026-07-30", query, StringComparison.Ordinal);
        Assert.Contains("saleNumber=SALE-20260730-000001", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Void_posts_the_reason_to_the_void_route()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SalePayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);
        var saleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await client.VoidSaleAsync(saleId, new VoidSaleRequest("Wrong item"));

        Assert.True(result.IsSuccess);
        Assert.Equal($"/api/v1/pos/sales/{saleId:D}/void", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Offline_short_circuits_checkout_without_calling_the_network()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new PosSaleClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivityService(isConnected: false));

        var result = await client.CheckoutAsync(new CheckoutSaleRequest(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m)],
            PosSaleOptions.CashPaymentMethod,
            10m));

        Assert.Equal(ApiCallStatus.Offline, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Conflict_response_surfaces_the_error_code()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(
                """{"title":"Conflict","detail":"Sale is already voided.","errorCode":"pos.sale.status.invalid_transition"}""",
                Encoding.UTF8,
                "application/problem+json")
        });
        var client = CreateClient(handler);

        var result = await client.VoidSaleAsync(Guid.NewGuid(), new VoidSaleRequest("Again"));

        Assert.Equal(ApiCallStatus.Conflict, result.Status);
        Assert.Equal("pos.sale.status.invalid_transition", result.Error!.ErrorCode);
    }

    private static PosSaleClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

    private sealed class StubConnectivityService(bool isConnected) : IConnectivityService
    {
        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(isConnected);

        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private int _callCount;

        public int CallCount => _callCount;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _callCount);
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }
}
