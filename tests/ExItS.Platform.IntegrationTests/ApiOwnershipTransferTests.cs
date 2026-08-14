using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class ApiOwnershipTransferTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    private SessionApiFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new SessionApiFactory(fixture.ConnectionString);
        _admin = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _admin.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task<(string Token, Guid UserId, string Email, string Password, string PublicUserId)> RegisterPersonalAsync(
        string prefix)
    {
        var (userId, email, password) = await PlatformIntegrationTestUsers.RegisterPersonalWithPasswordAsync(_client, prefix);
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;

        using var me = Authed(HttpMethod.Get, "/api/v1/me/public-identity", token);
        var meResponse = await _client.SendAsync(me);
        meResponse.EnsureSuccessStatusCode();
        var publicUserId = (await meResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("publicUserId").GetString()!;
        return (token, userId, email, password, publicUserId);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionToken").GetString()!;
    }

    [Fact]
    public async Task Request_accept_transfers_sole_owner_and_preserves_organization_identity()
    {
        var (_, ownerId, ownerEmail, ownerPassword, _) = await RegisterPersonalAsync("own");
        var (_, recipientId, recipientEmail, recipientPassword, recipientPublicId) =
            await RegisterPersonalAsync("rcp");

        var createOrg = await _admin.PostAsJsonAsync(
            "/api/v1/platform/organizations",
            new { displayName = "Ownership Transfer Org", slug = Unique("xfer") });
        createOrg.EnsureSuccessStatusCode();
        var orgBody = await createOrg.Content.ReadFromJsonAsync<JsonElement>();
        var orgId = orgBody.GetProperty("id").GetGuid();
        var displayName = orgBody.GetProperty("displayName").GetString();
        var slug = orgBody.GetProperty("slug").GetString();

        (await _admin.PostAsJsonAsync(
            $"/api/v1/platform/organizations/{orgId}/members",
            new { userId = ownerId, role = "OrganizationOwner", reason = "integration-test-owner" }))
            .EnsureSuccessStatusCode();

        var ownerToken = await LoginAsync(ownerEmail, ownerPassword);
        using var setCtx = Authed(
            HttpMethod.Put,
            "/api/v1/platform/auth/organization-context",
            ownerToken,
            new { organizationId = orgId });
        var ctxResponse = await _client.SendAsync(setCtx);
        Assert.True(ctxResponse.IsSuccessStatusCode, await ctxResponse.Content.ReadAsStringAsync());

        using var resolve = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{orgId:D}/ownership-transfer/resolve-target",
            ownerToken,
            new { input = recipientPublicId });
        var resolveResponse = await _client.SendAsync(resolve);
        Assert.True(resolveResponse.IsSuccessStatusCode, await resolveResponse.Content.ReadAsStringAsync());

        using var businessQr = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{orgId:D}/ownership-transfer/resolve-target",
            ownerToken,
            new { input = "exits://qr/v1/organization/ORG001842" });
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(businessQr)).StatusCode);

        using var request = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/organizations/{orgId:D}/ownership-transfer/request",
            ownerToken,
            new { targetInput = recipientPublicId });
        var requestResponse = await _client.SendAsync(request);
        Assert.True(requestResponse.IsSuccessStatusCode, await requestResponse.Content.ReadAsStringAsync());
        var transferId = (await requestResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var wrongAccept = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/ownership-transfers/{transferId:D}/accept",
            ownerToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(wrongAccept)).StatusCode);

        var recipientToken = await LoginAsync(recipientEmail, recipientPassword);
        using var accept = Authed(
            HttpMethod.Post,
            $"/api/v1/platform/ownership-transfers/{transferId:D}/accept",
            recipientToken);
        var acceptResponse = await _client.SendAsync(accept);
        Assert.True(acceptResponse.IsSuccessStatusCode, await acceptResponse.Content.ReadAsStringAsync());

        // Membership listing is Platform-admin scoped; verify post-conditions via DevelopmentOperator.
        var recipientList = await _admin.GetAsync(
            $"/api/v1/platform/users/{recipientId:D}/memberships?status=Active&pageSize=50");
        recipientList.EnsureSuccessStatusCode();
        var recipientItems = (await recipientList.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        Assert.Contains(
            recipientItems.EnumerateArray(),
            m => m.GetProperty("organizationId").GetGuid() == orgId
                 && m.GetProperty("role").GetString() == "OrganizationOwner");

        var formerList = await _admin.GetAsync(
            $"/api/v1/platform/users/{ownerId:D}/memberships?status=Active&pageSize=50");
        formerList.EnsureSuccessStatusCode();
        var formerItems = (await formerList.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        Assert.DoesNotContain(
            formerItems.EnumerateArray(),
            m => m.GetProperty("organizationId").GetGuid() == orgId);

        var orgAfter = await _admin.GetAsync($"/api/v1/platform/organizations/{orgId:D}");
        orgAfter.EnsureSuccessStatusCode();
        var orgAfterBody = await orgAfter.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orgId, orgAfterBody.GetProperty("id").GetGuid());
        Assert.Equal(displayName, orgAfterBody.GetProperty("displayName").GetString());
        Assert.Equal(slug, orgAfterBody.GetProperty("slug").GetString());
    }
}
