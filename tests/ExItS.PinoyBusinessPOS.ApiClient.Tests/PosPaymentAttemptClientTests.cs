using System.Net;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PosPaymentAttemptClientTests
{
    private const string AttemptPayload =
        """
        {
          "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          "saleId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          "organizationId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
          "method": "Card",
          "provider": "Fake",
          "providerReference": "FAKE-123",
          "externalReference": null,
          "amount": 150.00,
          "currency": "PHP",
          "status": "RequiresCustomerAction",
          "checkoutUrl": "https://example.test/checkout",
          "deepLink": null,
          "qrPayload": null,
          "cardBrand": null,
          "cardLastFour": null,
          "failureCode": null,
          "failureMessage": null,
          "idempotencyKey": "deadbeefdeadbeefdeadbeefdeadbeef",
          "createdAtUtc": "2026-07-30T04:15:00+00:00",
          "updatedAtUtc": "2026-07-30T04:15:00+00:00",
          "expiresAtUtc": "2026-07-30T04:30:00+00:00",
          "completedAtUtc": null,
          "verifiedBy": null,
          "verificationReason": null
        }
        """;

    [Fact]
    public async Task Create_posts_to_the_sale_payment_attempts_route()
    {
        var saleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(AttemptPayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.CreateAsync(
            saleId,
            new CreatePaymentAttemptRequest(PosSaleOptions.CardPaymentMethod, "deadbeefdeadbeefdeadbeefdeadbeef"));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal($"/api/v1/pos/sales/{saleId:D}/payment-attempts", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("RequiresCustomerAction", result.Data!.Status);
        Assert.True(handler.LastRequest.Headers.Contains("Idempotency-Key"));
    }

    [Fact]
    public async Task Get_loads_attempt_by_id()
    {
        var attemptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(AttemptPayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.GetAsync(attemptId);

        Assert.True(result.IsSuccess);
        Assert.Equal($"/api/v1/pos/payment-attempts/{attemptId:D}", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Cancel_posts_to_the_cancel_route()
    {
        var attemptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(AttemptPayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.CancelAsync(attemptId);

        Assert.True(result.IsSuccess);
        Assert.Equal($"/api/v1/pos/payment-attempts/{attemptId:D}/cancel", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Simulate_posts_outcome_to_the_simulate_route()
    {
        var attemptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(AttemptPayload, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.SimulateAsync(attemptId, new SimulatePaymentRequest("success"));

        Assert.True(result.IsSuccess);
        Assert.Equal($"/api/v1/pos/payment-attempts/{attemptId:D}/simulate", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Offline_short_circuits_create_without_calling_the_network()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new PosPaymentAttemptClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivityService(isConnected: false));

        var result = await client.CreateAsync(
            Guid.NewGuid(),
            new CreatePaymentAttemptRequest(PosSaleOptions.GCashPaymentMethod, Guid.NewGuid().ToString("N")));

        Assert.Equal(ApiCallStatus.Offline, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    private static PosPaymentAttemptClient CreateClient(StubHandler handler) =>
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
