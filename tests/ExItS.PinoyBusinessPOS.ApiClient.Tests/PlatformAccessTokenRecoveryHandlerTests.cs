using System.Net;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PlatformAccessTokenRecoveryHandlerTests
{
    [Fact]
    public async Task Platform401AttemptsRefreshOnce()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.OK);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/personal/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, recovery.Attempts);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task SuccessfulRefreshRetriesOriginalRequestOnce()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.OK);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        _ = await client.GetAsync("/api/v1/organizations/11111111-1111-1111-1111-111111111111");

        Assert.Equal(1, recovery.Attempts);
        Assert.Equal(2, inner.CallCount);
        Assert.Contains(inner.Paths, p => p.Contains("organizations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FailedRefreshTransitionsToUnauthorizedWithoutLoop()
    {
        var recovery = new CountingRecovery(succeed: false);
        var inner = new SequencingHandler(HttpStatusCode.Unauthorized);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/personal/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, recovery.Attempts);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task Second401DoesNotLoop()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Unauthorized);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/personal/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, recovery.Attempts);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task RevokedSessionCannotBeResurrected()
    {
        var recovery = new CountingRecovery(succeed: false);
        var inner = new SequencingHandler(HttpStatusCode.Unauthorized);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/personal/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(recovery.LastSucceeded);
        Assert.Equal(1, recovery.Attempts);
    }

    [Fact]
    public async Task OfflineDoesNotAttemptRefresh()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(HttpStatusCode.Unauthorized);

        var handler = CreateHandler(recovery, inner, online: false);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.GetAsync("/api/v1/personal/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, recovery.Attempts);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public async Task AuthTokenEndpointDoesNotAttemptRecovery()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(HttpStatusCode.Unauthorized);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var response = await client.PostAsync("/api/v1/platform/auth/token", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, recovery.Attempts);
    }

    [Fact]
    public async Task RefreshRetryOccursOnlyForExplicitUnauthorizedWhereSafe()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new SequencingHandler(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.OK);

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        var content = new StringContent("""{"name":"x"}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/personal/notifications/mark-read", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, recovery.Attempts);
        Assert.Equal(2, inner.CallCount);
    }

    [Fact]
    public async Task NonIdempotentWriteNotBlindlyRetriedAfterAmbiguousFailure()
    {
        var recovery = new CountingRecovery(succeed: true);
        var inner = new ThrowingHandler(new HttpRequestException("connection reset"));

        var handler = CreateHandler(recovery, inner, online: true);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") };

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.PostAsync("/api/v1/personal/notifications", new StringContent("{}")));

        Assert.Equal(0, recovery.Attempts);
        Assert.Equal(1, inner.CallCount);
    }

    private static PlatformAccessTokenRecoveryHandler CreateHandler(
        CountingRecovery recovery,
        HttpMessageHandler inner,
        bool online)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPlatformAccessTokenRecovery>(recovery);
        var sp = services.BuildServiceProvider();
        return new PlatformAccessTokenRecoveryHandler(
            sp,
            new StubConnectivity(online),
            NullLogger<PlatformAccessTokenRecoveryHandler>.Instance)
        {
            InnerHandler = inner
        };
    }

    private sealed class CountingRecovery(bool succeed) : IPlatformAccessTokenRecovery
    {
        public int Attempts { get; private set; }
        public bool LastSucceeded { get; private set; }

        public Task<bool> TryReissueAccessTokenAsync(CancellationToken ct = default)
        {
            Attempts++;
            LastSucceeded = succeed;
            return Task.FromResult(succeed);
        }
    }

    private sealed class StubConnectivity(bool online) : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(online);
    }

    private sealed class SequencingHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _factory;
        private int _index;
        public int CallCount { get; private set; }
        public List<string> Paths { get; } = [];

        public SequencingHandler(params Func<HttpResponseMessage>[] factories)
        {
            _factory = i => factories[Math.Min(i, factories.Length - 1)]();
        }

        public SequencingHandler(params HttpStatusCode[] statuses)
        {
            _factory = i => new HttpResponseMessage(statuses[Math.Min(i, statuses.Length - 1)]);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            var response = _factory(_index);
            _index++;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            throw exception;
        }
    }
}

public sealed class PlatformRequestClassificationTests
{
    [Fact]
    public async Task OfflineRequestIsClassifiedOffline()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new PosApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivity(false));

        var result = await client.GetAsync<object>("/api/v1/personal/notifications");

        Assert.Equal(ApiCallStatus.Offline, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Online401IsClassifiedUnauthorized()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"detail":"Authentication is required."}""", Encoding.UTF8, "application/json")
        });
        var client = new PosApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

        var result = await client.SendAsync<object>(HttpMethod.Post, "/api/x", new { });

        Assert.Equal(ApiCallStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task Online500IsServiceUnavailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new PosApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

        var result = await client.SendAsync<object>(HttpMethod.Post, "/api/x", new { });

        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task TimeoutIsNotSessionExpired()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("timeout"));
        var client = new PosApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

        var result = await client.SendAsync<object>(HttpMethod.Post, "/api/x", new { });

        Assert.Equal(ApiCallStatus.Timeout, result.Status);
        Assert.NotEqual(ApiCallStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task ForbiddenIsNotSessionExpired()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = new PosApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

        var result = await client.SendAsync<object>(HttpMethod.Post, "/api/x", new { });

        Assert.Equal(ApiCallStatus.Forbidden, result.Status);
        Assert.NotEqual(ApiCallStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task ConnectedTransportFailureIsUnavailableNotOffline()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("no route to host"));
        var client = new PosApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") },
            new StubConnectivity(true));

        var result = await client.SendAsync<object>(HttpMethod.Post, "/api/x", new { });

        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
    }

    private sealed class StubConnectivity(bool online) : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(online);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(respond(request));
        }
    }
}
