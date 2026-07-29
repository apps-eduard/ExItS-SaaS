using System.Net;
using System.Text;
using ExItS.Platform.Admin.Models;
using ExItS.Platform.Admin.Services;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class PlatformApiClientTests
{
    [Fact]
    public async Task Get_products_uses_expected_pagination_and_status_query()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"items":[],"totalCount":0,"page":2,"pageSize":25}""", Encoding.UTF8, "application/json")
        });
        var client = new PlatformApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") });

        var result = await client.GetProductsAsync(2, 25, "Active");

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/platform/catalog/products?page=2&pageSize=25&status=Active", handler.Request!.PathAndQuery);
    }

    [Fact]
    public async Task Problem_details_returns_not_found_with_correlation_id()
    {
        var handler = new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"title":"Missing","detail":"No product exists."}""", Encoding.UTF8, "application/problem+json")
            };
            response.Headers.Add("X-Correlation-ID", "corr-123");
            return response;
        });
        var client = new PlatformApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") });

        var result = await client.GetProductAsync(Guid.NewGuid());

        Assert.Equal(ApiCallStatus.NotFound, result.Status);
        Assert.Equal("Missing", result.Error!.Title);
        Assert.Equal("corr-123", result.Error.CorrelationId);
    }

    [Fact]
    public async Task Get_portfolio_summary_uses_admin_path()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"activeProductCount":1,"publishedPlanVersionCount":0,"organizationCount":2,"trialingSubscriptionCount":0,"activeSubscriptionCount":1,"gracePeriodSubscriptionCount":0,"pastDueSubscriptionCount":0,"suspendedSubscriptionCount":0,"pendingManualPaymentCount":0,"latestEntitlementSnapshotCount":0,"partialFailures":[]}""",
                Encoding.UTF8,
                "application/json")
        });
        var client = new PlatformApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") });
        var result = await client.GetPortfolioSummaryAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/platform/admin/portfolio-summary", handler.Request!.AbsolutePath);
        Assert.Equal(1, result.Data!.ActiveProductCount);
    }

    [Fact]
    public async Task Cancellation_is_honored()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new PlatformApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetProductsAsync(ct: cts.Token));
    }

    [Fact]
    public async Task Transport_failure_returns_unavailable()
    {
        var client = new PlatformApiClient(new HttpClient(new StubHandler(_ => throw new HttpRequestException("offline"))) { BaseAddress = new Uri("http://platform.test") });
        var result = await client.GetPortfolioSummaryAsync();
        Assert.Equal(ApiCallStatus.Unavailable, result.Status);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request.RequestUri;
            return Task.FromResult(response(request));
        }
    }
}
