using System.Net;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.ApiClient;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PosApiClientTests
{
    private sealed record ProductDto(string Name, int Quantity);

    [Fact]
    public async Task Get_deserializes_success_response_case_insensitively()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"Name":"Softdrink","Quantity":24}""", Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.True(result.IsSuccess);
        Assert.Equal(ApiCallStatus.Success, result.Status);
        Assert.Equal("Softdrink", result.Data!.Name);
        Assert.Equal(24, result.Data.Quantity);
        Assert.Equal(1, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, ApiCallStatus.NotFound)]
    [InlineData(HttpStatusCode.BadRequest, ApiCallStatus.Validation)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ApiCallStatus.Validation)]
    [InlineData(HttpStatusCode.Conflict, ApiCallStatus.Conflict)]
    [InlineData(HttpStatusCode.Unauthorized, ApiCallStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ApiCallStatus.Forbidden)]
    public async Task Problem_details_are_classified_per_status_code(HttpStatusCode httpStatus, ApiCallStatus expected)
    {
        var handler = new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(httpStatus)
            {
                Content = new StringContent(
                    """{"title":"Something went wrong","detail":"Detailed reason.","errorCode":"POS-001"}""",
                    Encoding.UTF8,
                    "application/problem+json")
            };
            response.Headers.Add("X-Correlation-ID", "corr-abc");
            return response;
        });
        // POST is used so the single automatic retry (GET-only) never masks the classification.
        var client = CreateClient(handler);

        var result = await client.SendAsync<ProductDto>(HttpMethod.Post, "/api/products", new { Name = "x" });

        Assert.Equal(expected, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error!.Title);
        Assert.Equal("Detailed reason.", result.Error.Detail);
        Assert.Equal("POS-001", result.Error.ErrorCode);
        Assert.Equal("corr-abc", result.Error.CorrelationId);
        Assert.Equal((int)httpStatus, result.Error.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Timeout_classification_applies_when_task_canceled_without_user_cancellation()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("simulated http client timeout"));
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.Equal(ApiCallStatus.Timeout, result.Status);
        // GET is retried once on Timeout, so the stub is invoked twice.
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task Cancellation_returns_cancelled_status_without_throwing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1", cts.Token);

        Assert.Equal(ApiCallStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task Offline_connectivity_short_circuits_without_calling_network()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new PosApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") }, new StubConnectivityService(isConnected: false));

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.Equal(ApiCallStatus.Offline, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Http_request_exception_when_connected_is_unavailable()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("no route to host"));
        var client = new PosApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivityService(isConnected: true));

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Http_request_exception_when_offline_is_offline()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("no route to host"));
        var client = new PosApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivityService(isConnected: false));

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        // Short-circuit before network when connectivity reports offline.
        Assert.Equal(ApiCallStatus.Offline, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Get_retries_once_on_unavailable_then_returns_unavailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"title":"Service unavailable"}""", Encoding.UTF8, "application/problem+json")
        });
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task Get_retry_succeeds_on_second_attempt()
    {
        var handler = new StubHandler(request =>
        {
            if (request.CallNumber == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"name":"Softdrink","quantity":24}""", Encoding.UTF8, "application/json")
            };
        });
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task Post_is_never_retried_on_unavailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler);

        var result = await client.SendAsync<ProductDto>(HttpMethod.Post, "/api/products", new { Name = "x" });

        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Correlation_id_is_extracted_from_response_headers()
    {
        var handler = new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"title":"Missing"}""", Encoding.UTF8, "application/problem+json")
            };
            response.Headers.Add("X-Correlation-ID", "corr-xyz-789");
            return response;
        });
        var client = CreateClient(handler);

        var result = await client.GetAsync<ProductDto>("/api/products/1");

        Assert.Equal("corr-xyz-789", result.Error!.CorrelationId);
    }

    [Fact]
    public async Task Get_health_parses_json_object_response()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":"Healthy"}""", Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var result = await client.GetHealthAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Healthy", result.Data!.Status);
        Assert.Equal("/health", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Get_health_parses_bare_string_response()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Healthy", Encoding.UTF8, "text/plain")
        });
        var client = CreateClient(handler);

        var result = await client.GetHealthAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("Healthy", result.Data!.Status);
    }

    private static PosApiClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

    private sealed class StubConnectivityService(bool isConnected) : IConnectivityService
    {
        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(isConnected);
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged;
    }

    private sealed class StubHandler(Func<StubRequest, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private int _callCount;

        public int CallCount => _callCount;
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var callNumber = Interlocked.Increment(ref _callCount);
            LastRequest = request;
            return Task.FromResult(respond(new StubRequest(request, callNumber)));
        }
    }

    private sealed record StubRequest(HttpRequestMessage Request, int CallNumber);
}
