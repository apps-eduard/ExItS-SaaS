using System.Net;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class LinkedCustomerPlatformAuthorizationClientTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PersonalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Authorize_maps_200_to_authorized_proof()
    {
        var json = JsonSerializer.Serialize(new
        {
            personalUserId = PersonalId,
            organizationId = OrgId,
            platformBusinessCustomerId = CustomerId,
            linkedCustomerAppUserId = LinkId
        });
        HttpRequestMessage? seen = null;
        var client = CreateClient(HttpStatusCode.OK, json, (_, req) => seen = req);
        var result = await client.VerifyAsync(OrgId, CustomerId);

        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.Authorized, result.Outcome);
        Assert.NotNull(result.Proof);
        Assert.Equal(PersonalId, result.Proof!.PersonalUserId);
        Assert.Equal(LinkId, result.Proof.LinkedCustomerAppUserId);
        Assert.NotNull(seen);
        Assert.Equal(HttpMethod.Get, seen!.Method);
        Assert.Contains($"organizationId={OrgId:D}", seen.RequestUri!.PathAndQuery, StringComparison.Ordinal);
        Assert.Contains($"businessCustomerId={CustomerId:D}", seen.RequestUri.PathAndQuery, StringComparison.Ordinal);
        Assert.Equal("PlatformSession", seen.Headers.Authorization!.Scheme);
        Assert.Equal("session-token", seen.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Authorize_maps_403_to_denied()
    {
        var client = CreateClient(HttpStatusCode.Forbidden, "{}");
        var result = await client.VerifyAsync(OrgId, CustomerId);
        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.Denied, result.Outcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Authorize_maps_failure_statuses_to_not_found(HttpStatusCode status)
    {
        var client = CreateClient(status, "{}");
        var result = await client.VerifyAsync(OrgId, CustomerId);
        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Authorize_maps_malformed_json_to_not_found()
    {
        var client = CreateClient(HttpStatusCode.OK, "{not-json");
        var result = await client.VerifyAsync(OrgId, CustomerId);
        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Authorize_maps_mismatched_ids_in_body_to_not_found()
    {
        var json = JsonSerializer.Serialize(new
        {
            personalUserId = PersonalId,
            organizationId = Guid.NewGuid(),
            platformBusinessCustomerId = CustomerId,
            linkedCustomerAppUserId = LinkId
        });
        var client = CreateClient(HttpStatusCode.OK, json);
        var result = await client.VerifyAsync(OrgId, CustomerId);
        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Authorize_maps_http_exception_to_not_found()
    {
        var handler = new StubHandler((_, _) => throw new HttpRequestException("unreachable"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test/") };
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Request.Headers["X-ExItS-Session-Token"] = "session-token";
        var client = new LinkedCustomerPlatformAuthorizationClient(
            httpClient,
            accessor,
            Options.Create(new PlatformAuthOptions { BaseUrl = "http://platform.test" }));

        var result = await client.VerifyAsync(OrgId, CustomerId);
        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Authorize_forwards_platform_auth_cookie_as_session_when_bearer_only_inbound()
    {
        // React Personal: Bearer to POS + HttpOnly Platform cookie (no X-ExItS-Session-Token).
        HttpRequestMessage? seen = null;
        var json = JsonSerializer.Serialize(new
        {
            personalUserId = PersonalId,
            organizationId = OrgId,
            platformBusinessCustomerId = CustomerId,
            linkedCustomerAppUserId = LinkId
        });
        var handler = new StubHandler((req, _) =>
        {
            seen = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test/") };
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Request.Headers.Authorization = "Bearer product-access-token";
        // HttpOnly Platform session cookie (same-origin Vite proxy / browser credentials).
        accessor.HttpContext.Request.Headers.Cookie = ".ExItS.Platform.Auth=cookie-session-token";

        var client = new LinkedCustomerPlatformAuthorizationClient(
            httpClient,
            accessor,
            Options.Create(new PlatformAuthOptions { BaseUrl = "http://platform.test" }));

        var result = await client.VerifyAsync(OrgId, CustomerId);

        Assert.Equal(LinkedCustomerPlatformAuthorizationOutcome.Authorized, result.Outcome);
        Assert.NotNull(seen);
        Assert.Equal("PlatformSession", seen!.Headers.Authorization!.Scheme);
        Assert.Equal("cookie-session-token", seen.Headers.Authorization.Parameter);
        Assert.False(
            seen.Headers.Authorization.Parameter == "product-access-token",
            "Product Bearer must not be forwarded as PlatformSession.");
        Assert.True(seen.Headers.TryGetValues("Cookie", out var cookies));
        Assert.Contains(cookies, c => c.Contains(".ExItS.Platform.Auth=cookie-session-token", StringComparison.Ordinal));
    }

    private static LinkedCustomerPlatformAuthorizationClient CreateClient(
        HttpStatusCode status,
        string body,
        Action<HttpStatusCode, HttpRequestMessage>? onRequest = null)
    {
        var handler = new StubHandler((req, _) =>
        {
            onRequest?.Invoke(status, req);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test/") };
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Request.Headers["X-ExItS-Session-Token"] = "session-token";
        return new LinkedCustomerPlatformAuthorizationClient(
            httpClient,
            accessor,
            Options.Create(new PlatformAuthOptions { BaseUrl = "http://platform.test" }));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request, cancellationToken));
    }
}
