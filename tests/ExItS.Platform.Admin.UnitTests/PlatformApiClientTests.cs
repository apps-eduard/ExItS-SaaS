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

    [Fact]
    public async Task Start_trial_posts_to_organization_trials_route()
    {
        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var handler = OkHandler();
        var client = CreateClient(handler);

        var result = await client.StartTrialAsync(orgId, new StartTrialRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal($"/api/v1/platform/organizations/{orgId}/subscriptions/trials", handler.Request!.AbsolutePath);
    }

    [Fact]
    public async Task Subscription_lifecycle_actions_use_expected_routes()
    {
        var subscriptionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var start = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var end = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var handler = OkHandler();
        var client = CreateClient(handler);

        Assert.True((await client.ActivateSubscriptionAsync(subscriptionId, new ActivateSubscriptionRequest(start, end))).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/activate", handler.Request!.AbsolutePath);

        Assert.True((await client.EnterGracePeriodAsync(subscriptionId, new GracePeriodRequest(end))).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/grace-period", handler.Request!.AbsolutePath);

        Assert.True((await client.MarkPastDueAsync(subscriptionId)).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/past-due", handler.Request!.AbsolutePath);

        Assert.True((await client.SuspendSubscriptionAsync(subscriptionId)).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/suspend", handler.Request!.AbsolutePath);

        Assert.True((await client.ReactivateSubscriptionAsync(subscriptionId, new ReactivateSubscriptionRequest(start, end))).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/reactivate", handler.Request!.AbsolutePath);

        Assert.True((await client.CancelSubscriptionAsync(subscriptionId)).IsSuccess);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/cancel", handler.Request!.AbsolutePath);

        Assert.True((await client.ExpireSubscriptionAsync(subscriptionId)).IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal($"/api/v1/platform/subscriptions/{subscriptionId}/expire", handler.Request!.AbsolutePath);
    }

    [Fact]
    public async Task Manual_payment_mutations_use_expected_routes()
    {
        var paymentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var subscriptionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var start = DateTimeOffset.Parse("2026-07-01T00:00:00Z");
        var end = DateTimeOffset.Parse("2026-08-01T00:00:00Z");
        var handler = OkHandler();
        var client = CreateClient(handler);

        Assert.True((await client.CreateManualPaymentAsync(new CreateManualPaymentRequest(
            Guid.NewGuid(), "POS", 500m, "PHP", "GCash", "ref-1", start))).IsSuccess);
        Assert.Equal("/api/v1/platform/payments/manual", handler.Request!.AbsolutePath);

        Assert.True((await client.ConfirmPaymentAsync(paymentId, new ConfirmPaymentRequest("staff"))).IsSuccess);
        Assert.Equal($"/api/v1/platform/payments/{paymentId}/confirm", handler.Request!.AbsolutePath);

        Assert.True((await client.RejectPaymentAsync(paymentId, new RejectPaymentRequest("staff", "mismatch"))).IsSuccess);
        Assert.Equal($"/api/v1/platform/payments/{paymentId}/reject", handler.Request!.AbsolutePath);

        Assert.True((await client.VoidPaymentAsync(paymentId, new VoidPaymentRequest("staff", "refund"))).IsSuccess);
        Assert.Equal($"/api/v1/platform/payments/{paymentId}/void", handler.Request!.AbsolutePath);

        Assert.True((await client.ConfirmPaymentAndActivateAsync(paymentId, new ActivateSubscriptionForPaymentRequest(
            "staff", subscriptionId, start, end))).IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal($"/api/v1/platform/payments/{paymentId}/activate-subscription", handler.Request!.AbsolutePath);
    }

    [Fact]
    public async Task Confirm_payment_conflict_returns_conflict_status()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("""{"title":"Conflict","detail":"Payment already linked."}""", Encoding.UTF8, "application/problem+json")
        });
        var client = CreateClient(handler);

        var result = await client.ConfirmPaymentAndActivateAsync(
            Guid.NewGuid(),
            new ActivateSubscriptionForPaymentRequest("staff", Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)));

        Assert.Equal(ApiCallStatus.Failed, result.Status);
        Assert.Equal("Conflict", result.Error!.Title);
        Assert.Equal(HttpStatusCode.Conflict, result.Error.StatusCode);
    }

    private static PlatformApiClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://platform.test") });

    private static StubHandler OkHandler() =>
        new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public Uri? Request { get; private set; }
        public HttpMethod? Method { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Request = request.RequestUri;
            Method = request.Method;
            return Task.FromResult(response(request));
        }
    }
}
