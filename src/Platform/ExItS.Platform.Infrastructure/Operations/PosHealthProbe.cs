using System.Diagnostics;
using ExItS.Platform.Application.Integration.Pos;
using ExItS.Platform.Application.Operations;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Operations;

/// <summary>
/// Probes POS public <c>/health</c> and <c>/health/ready</c>. Does not send support keys,
/// inspect Docker, or surface exception/secret text.
/// </summary>
internal sealed class PosHealthProbe : IPosHealthProbe
{
    public const string HttpClientName = "pos-health-probe";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PosProductApiOptions _options;

    public PosHealthProbe(IHttpClientFactory httpClientFactory, IOptions<PosProductApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public Task<ProbedDependencyHealth> ProbeLivenessAsync(CancellationToken cancellationToken = default) =>
        ProbeAsync("/health", cancellationToken);

    public Task<ProbedDependencyHealth> ProbeReadinessAsync(CancellationToken cancellationToken = default) =>
        ProbeAsync("/health/ready", cancellationToken);

    private async Task<ProbedDependencyHealth> ProbeAsync(string path, CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return new ProbedDependencyHealth(SystemHealthStatuses.Unavailable, LatencyMs: null, checkedAt);
        }

        if (!Uri.TryCreate(_options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            return new ProbedDependencyHealth(SystemHealthStatuses.Unavailable, LatencyMs: null, checkedAt);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, path.TrimStart('/')));
            request.Headers.Accept.ParseAdd("text/plain");
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            var latency = Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds);

            string body;
            try
            {
                body = (await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
            }
            catch
            {
                body = string.Empty;
            }

            if (LooksLikeSecret(body))
            {
                body = string.Empty;
            }

            var status = SystemHealthStatusRules.FromHealthEndpointBody(body, (int)response.StatusCode);
            return new ProbedDependencyHealth(status, latency, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            stopwatch.Stop();
            return new ProbedDependencyHealth(
                SystemHealthStatuses.Unavailable,
                Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds),
                DateTimeOffset.UtcNow);
        }
    }

    private static bool LooksLikeSecret(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length > 64)
        {
            return true;
        }

        return body.Contains("Password=", StringComparison.OrdinalIgnoreCase)
               || body.Contains("pwd=", StringComparison.OrdinalIgnoreCase)
               || body.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
               || body.Contains("BEGIN ", StringComparison.OrdinalIgnoreCase)
               || body.Contains("secret", StringComparison.OrdinalIgnoreCase);
    }
}
