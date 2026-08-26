using System.Net;
using System.Net.Http.Headers;
using ExItS.Platform.Application.Integration.Pos;
using ExItS.Platform.Application.Operations;
using ExItS.Platform.Infrastructure.Operations;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Operations;

public sealed class PosHealthProbeTests
{
    [Fact]
    public async Task Unconfigured_base_url_is_unavailable()
    {
        var probe = CreateProbe(baseUrl: "", handler: new FixedHandler(HttpStatusCode.OK, "Healthy"));
        var live = await probe.ProbeLivenessAsync();
        var ready = await probe.ProbeReadinessAsync();
        Assert.Equal(SystemHealthStatuses.Unavailable, live.Status);
        Assert.Equal(SystemHealthStatuses.Unavailable, ready.Status);
        Assert.Null(live.LatencyMs);
        Assert.Null(ready.LatencyMs);
    }

    [Fact]
    public async Task Liveness_healthy_does_not_imply_database_health()
    {
        var probe = CreateProbe(
            "http://pos.test",
            new PathAwareHandler
            {
                LiveStatus = HttpStatusCode.OK,
                LiveBody = "Healthy",
                ReadyStatus = HttpStatusCode.ServiceUnavailable,
                ReadyBody = "Unhealthy"
            });

        var live = await probe.ProbeLivenessAsync();
        var ready = await probe.ProbeReadinessAsync();
        Assert.Equal(SystemHealthStatuses.Healthy, live.Status);
        Assert.Equal(SystemHealthStatuses.Unhealthy, ready.Status);
    }

    [Fact]
    public async Task Connection_failure_is_unavailable_without_secret_text()
    {
        var probe = CreateProbe("http://pos.test", new ThrowingHandler("Password=super-secret-value"));
        var live = await probe.ProbeLivenessAsync();
        Assert.Equal(SystemHealthStatuses.Unavailable, live.Status);
        var json = System.Text.Json.JsonSerializer.Serialize(live);
        Assert.DoesNotContain("super-secret-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Secret_like_body_is_redacted_to_unknown()
    {
        var probe = CreateProbe(
            "http://pos.test",
            new FixedHandler(HttpStatusCode.OK, "Healthy Password=leaked"));
        var live = await probe.ProbeLivenessAsync();
        Assert.Equal(SystemHealthStatuses.Unknown, live.Status);
        var json = System.Text.Json.JsonSerializer.Serialize(live);
        Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("leaked", json, StringComparison.Ordinal);
    }

    private static PosHealthProbe CreateProbe(string baseUrl, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        return new PosHealthProbe(
            new StubHttpClientFactory(client),
            Options.Create(new PosProductApiOptions { BaseUrl = baseUrl }));
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FixedHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
                }
            });
        }
    }

    private sealed class PathAwareHandler : HttpMessageHandler
    {
        public HttpStatusCode LiveStatus { get; init; }
        public string LiveBody { get; init; } = "Healthy";
        public HttpStatusCode ReadyStatus { get; init; }
        public string ReadyBody { get; init; } = "Healthy";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var ready = request.RequestUri?.AbsolutePath.Contains("/ready", StringComparison.OrdinalIgnoreCase) == true;
            var status = ready ? ReadyStatus : LiveStatus;
            var body = ready ? ReadyBody : LiveBody;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body)
            });
        }
    }

    private sealed class ThrowingHandler(string message) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException(message);
    }
}
